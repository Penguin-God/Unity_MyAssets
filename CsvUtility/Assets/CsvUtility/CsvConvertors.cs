using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;

namespace CsvConvertors
{
    // 커스텀 확장용으로 필요
    interface ICsvConvertor
    {
        Type ConverteType { get; }
        object TextToObject(string text, Type type);
    }

    static class CsvConvertUtility
    {
        // public은 TypeIdentifier에서 class 가져가는 거 때문에 임시로 설정함.
        public static UserCustomConvertorManager _customConvertorManager = new UserCustomConvertorManager();
        public static object TextToObject(string text, Type type)
        {
            if (type.IsPrimitive) return new PrimitiveConvertor().TextToObject(text, type);
            else if (type == typeof(string)) return text;
            else if (type.IsEnum) return new EnumConvertor().TextToObject(text, type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) return new PairConvertor().TextToObject(text, type);
            else if (TypeIdentifier.IsIEnumerable(type)) return new IEnumerableConvertor().TextToObject(text, type);
            else if (_customConvertorManager.IsUserCustomConvertor(type)) return _customConvertorManager.TextToObject(text, type);
            return null;
        }
    }

    class UserCustomConvertorManager
    {
        Dictionary<Type, object> _objByType = new Dictionary<Type, object>();
        public UserCustomConvertorManager() => Init();

        void Init()
        {
            var interfaceType = typeof(ICsvConvertor);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && p.IsInterface == false);
            foreach (var type in types)
            {
                object obj = Activator.CreateInstance(type) as ICsvConvertor;
                _objByType.Add(type.GetProperty("ConverteType").GetValue(obj) as Type, obj);
            }
        }

        public object TextToObject(string text, Type type)
        {
            if (_objByType.TryGetValue(type, out object obj) == false)
                return null;
            text = text.Replace('+', ','); // 일단 돌아만 가게 하자
            return obj.GetType().GetMethod("TextToObject").Invoke(obj, new object[] { text, type });
        }

        public bool IsUserCustomConvertor(Type type) => _objByType.ContainsKey(type);
    }

    class PrimitiveConvertor
    {
        public object TextToObject(string text, Type type)
        {
            object result = null;
            try
            {
                result = Convert.ChangeType(text, type);
            }
            catch (Exception)
            {
                Debug.LogError($"CsvUtility Message FormatException  : Input string was not in a correct format. \n input strint value : {text}    try convert type : {type}");
            }

            return result;
        }
    }

    public interface ICsvIEnumeralbeParser
    {
        string[] GetCsvValues(object obj, FieldInfo info);
    }

    class EnumConvertor
    {
        public object TextToObject(string text, Type type)
        {
            object result = null;
            try
            {
                result = Enum.Parse(type, text);
            }
            catch
            {
                Debug.LogError($"CsvUtility Message : The requested value {text} was not found within {type} enum.");
            }
            return result;
        }
    }

    public class PairConvertor
    {
        public object TextToObject(string text, Type type)
        {
            string[] values = text.Split('+');
            if (values.Length != 2) Debug.LogError($"{type} : The input is incorrect.Please make sure you entered the Key Value pair correctly.");
            Type[] elementTypes = type.GetGenericArguments();
            ConstructorInfo constructor = type.GetConstructors()[0];
            return constructor.Invoke(new object[]
            {
                CsvConvertUtility.TextToObject(values[0], elementTypes[0]),
                CsvConvertUtility.TextToObject(values[1], elementTypes[1]),
            });
        }
    }


    #region 열거형

    class IEnumerableConvertor
    {
        ICsvIEnumeralbeParser GetIEnumerableParser(Type type)
        {
            if (type.IsArray) return new ArrayConvertor();
            else if (TypeIdentifier.IsList(type)) return new ListConvertor();
            else if (TypeIdentifier.IsDictionary(type)) return new DictionaryConvertor();
            return null;
        }

        public string[] GetIEnumerableValues(object obj, FieldInfo info)
        {
            ICsvIEnumeralbeParser parser = GetIEnumerableParser(info.FieldType);
            if (parser != null)
                return parser.GetCsvValues(obj, info);
            else
                return null;
        }

        public object TextToObject(string text, Type type)
        {
            var texts = text.Split('+').Select(x => x.Trim()).ToArray();
            if (type.IsArray) return new ArrayConvertor().TextToObject(texts, type);
            else if (TypeIdentifier.IsList(type)) return new ListConvertor().TextToObject(texts, type);
            else if (TypeIdentifier.IsDictionary(type)) return new DictionaryConvertor().TextToObject(texts, type);
            return null;
        }
    }



    public class CsvArrayConvertUtility
    {
        public static Array TextToArray(string[] texts, Type elementType)
        {
            Array array = Array.CreateInstance(elementType, texts.Length);
            for (int i = 0; i < array.Length; i++)
                array.SetValue(CsvConvertUtility.TextToObject(texts[i], elementType), i);
            return array;
        }
    }

    public class ArrayConvertor : ICsvIEnumeralbeParser
    {
        public string[] GetCsvValues(object obj, FieldInfo info)
        {
            Array array = info.GetValue(obj) as Array;
            List<string> result = new List<string>();
            foreach (var item in array) result.Add(item.ToString());
            return result.ToArray();
        }

        public object TextToObject(string[] texts, Type type) => CsvArrayConvertUtility.TextToArray(texts, type.GetElementType());
    }

    public class ListConvertor : ICsvIEnumeralbeParser
    {
        public string[] GetCsvValues(object obj, FieldInfo info)
        {
            IList list = info.GetValue(obj) as IList;
            List<string> result = new List<string>();
            foreach (var item in list) result.Add(item.ToString());
            return result.ToArray();
        }
        
        public object TextToObject(string[] texts, Type type)
            => type.GetConstructors()[2].Invoke(new object[] { CsvArrayConvertUtility.TextToArray(texts, type.GetGenericArguments()[0]) });
    }


    public class DictionaryConvertor : ICsvIEnumeralbeParser
    {
        public string[] GetCsvValues(object obj, FieldInfo info)
        {
            IDictionary dictionary = info.GetValue(obj) as IDictionary;
            List<string> keys = new List<string>();
            List<string> values = new List<string>();

            foreach (var item in dictionary.Keys) keys.Add(item.ToString());
            foreach (var item in dictionary.Values) values.Add(item.ToString());

            List<string> result = new List<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                result.Add(keys[i]);
                result.Add(values[i]);
            }

            return result.ToArray();
        }

        public object TextToObject(string[] texts, Type type)
        {
            if (texts.Length % 2 != 0) Debug.LogError("CsvUtility Message : The input is incorrect. Please make sure you entered the Dictionary correctly.");
            var result = Activator.CreateInstance(type);
            Type[] elementTypes = type.GetGenericArguments();
            MethodInfo methodInfo = type.GetMethod("Add");
            for (int i = 0; i < texts.Length; i += 2)
            {
                methodInfo.Invoke(result, new object[]
                {
                    CsvConvertUtility.TextToObject(texts[i],elementTypes[0]),
                    CsvConvertUtility.TextToObject(texts[i+1], elementTypes[1])
                });
            }
            return result;
        }
    }
    #endregion 열거형 파싱 End

    public static class TypeIdentifier
    {
        public static bool IsList(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        public static bool IsDictionary(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        public static bool IsIEnumerable(Type type) => type.IsArray || IsList(type) || IsDictionary(type);
        public static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string) || type.IsEnum
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
        public static bool IsCustom(Type type)
        {
            if (type.IsEnum) return false;
            else if (type.ToString().StartsWith("System.")) return false;
            else if (type.IsArray) return IsCustom(type.GetElementType());
            else if (IsList(type) && type.GetGenericArguments()[0] != null) return IsCustom(type.GetGenericArguments()[0]);
            else if (CsvConvertUtility._customConvertorManager.IsUserCustomConvertor(type)) return false; // 이건 임시임
            else return true;
        }
    }
}
