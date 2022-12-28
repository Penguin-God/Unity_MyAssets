using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using System.Text;

namespace ParserCore
{
    public class CsvParsers
    {
        public static CsvParser GetParser(FieldInfo info)
        {
            if (TypeIdentifier.IsIEnumerable(info.FieldType))
                return new EnumerableTypeParser();
            else
                return new PrimitiveTypeParser();
        }
    }

    enum EnumerableType
    {
        Unknown,
        Array,
        List,
        Dictionary,
    }

    public interface ICsvIEnumeralbeParser
    {
        string[] GetCsvValues(object obj, FieldInfo info);
    }

    public interface CsvPrimitiveTypeParser
    {
        object GetParserValue(string value);
        Type GetParserType();
    }

    public interface CsvParser
    {
        void SetValue(object obj, FieldInfo info, string[] values);
    }

    class PrimitiveTypeParser : CsvParser
    {
        public static CsvPrimitiveTypeParser GetPrimitiveParser(Type type)
        {
            if (type == typeof(int)) return new CsvIntParser();
            else if (type == typeof(byte)) return new CsvByteParser();
            else if (type == typeof(long)) return new CsvLongParser();
            else if (type == typeof(string)) return new CsvStringParser();
            else if (type == typeof(float)) return new CsvFloatParser();
            else if (type == typeof(double)) return new CsvDoubleParser();
            else if (type == typeof(bool)) return new CsvBooleanParser();
            else if (type.IsEnum) return new CsvEnumParser(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) return new CsvPairParser(type);
            else Debug.LogError($"Unloadable type : {type}");
            return null;
        }

        object GetParserValue(Type type, string value) => GetPrimitiveParser(type).GetParserValue(value);
        public void SetValue(object obj, FieldInfo info, string[] value) => info.SetValue(obj, GetParserValue(info.FieldType, value[0]));
    }

    class EnumerableTypeParser : CsvParser
    {
        EnumerableType GetEnumableType(Type type)
        {
            if (type.IsArray) return EnumerableType.Array;
            else if (TypeIdentifier.IsList(type)) return EnumerableType.List;
            else if (TypeIdentifier.IsDictionary(type)) return EnumerableType.Dictionary;
            return EnumerableType.Unknown;
        }

        public void SetValue(object obj, FieldInfo info, string[] values)
        {
            values = SetValue(values);
            switch (GetEnumableType(info.FieldType))
            {
                case EnumerableType.Array: new CsvArrayParser().SetValue(obj, info, values); break;
                case EnumerableType.List: new CsvListParser().SetValue(obj, info, values); break;
                case EnumerableType.Dictionary: new CsvDictionaryParser().SetValue(obj, info, values); break;
            }
        }

        string[] SetValue(string[] values)
        {
            const char replaceMark = '+';
            StringBuilder builder = new StringBuilder();

            foreach (string value in values.Where(x => !string.IsNullOrEmpty(x)))
            {
                builder.Append(replaceMark);
                builder.Append(value);
            }
            return builder.ToString().Split(replaceMark).Skip(1).Select(x => x.Trim()).ToArray();
        }

        public ICsvIEnumeralbeParser GetIEnumerableParser(Type type)
        {
            if (type.IsArray) return new CsvArrayParser();
            else if (TypeIdentifier.IsList(type)) return new CsvListParser();
            else if (TypeIdentifier.IsDictionary(type)) return new CsvDictionaryParser();
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
    }

    #region 기본형 파싱

    class CsvByteParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value)
        {
            Byte.TryParse(value, out byte result);
            return result;
        }

        public Type GetParserType() => typeof(byte);
    }

    class CsvIntParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value)
        {
            Int32.TryParse(value, out int valueInt);
            return valueInt;
        }

        public Type GetParserType() => typeof(int);
    }

    class CsvLongParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value)
        {
            long.TryParse(value, out long valueInt);
            return valueInt;
        }

        public Type GetParserType() => typeof(long);
    }

    class CsvFloatParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value)
        {
            float.TryParse(value, out float valueFloat);
            return valueFloat;
        }

        public Type GetParserType() => typeof(float);
    }

    class CsvDoubleParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value)
        {
            double.TryParse(value, out double valueFloat);
            return valueFloat;
        }

        public Type GetParserType() => typeof(double);
    }

    class CsvStringParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value) => value;
        public Type GetParserType() => typeof(string);
    }

    class CsvBooleanParser : CsvPrimitiveTypeParser
    {
        public object GetParserValue(string value) => value == "True" || value == "TRUE" || value == "true";
        public Type GetParserType() => typeof(bool);
    }

    class CsvEnumParser : CsvPrimitiveTypeParser
    {
        Type _type;
        public CsvEnumParser(Type type)
        {
            _type = type;
        }

        public object GetParserValue(string value)
        {
            object result = null;
            try
            {
                result = Enum.Parse(_type, value);
            }
            catch
            {
                Debug.LogError($"CsvUtility Message : The requested value {value} was not found within {_type} enum.");
            }
            return result;
        }
        public Type GetParserType() => _type;
    }

    #endregion 기본형 파싱 End




    #region 열거형 파싱
    public class CsvListParser : ICsvIEnumeralbeParser
    {
        public void SetValue(object obj, FieldInfo info, string[] values)
        {
            ConstructorInfo constructor = info.FieldType.GetConstructors()[2];
            info.SetValue(obj, constructor.Invoke(new object[] { GetValue(info, values) }));
        }

        Array GetValue(FieldInfo info, string[] values)
            => new CsvArrayParser().GetValue(info.FieldType.GetGenericArguments()[0], values);

        public string[] GetCsvValues(object obj, FieldInfo info)
        {
            IList list = info.GetValue(obj) as IList;
            List<string> result = new List<string>();
            foreach (var item in list) result.Add(item.ToString());
            return result.ToArray();
        }
    }

    public class CsvArrayParser : ICsvIEnumeralbeParser
    {
        public void SetValue(object obj, FieldInfo info, string[] values)
            => info.SetValue(obj, GetValue(info.FieldType.GetElementType(), values));

        public Array GetValue(Type elementType, string[] values)
        {
            Array array = Array.CreateInstance(PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserType(), values.Length);
            for (int i = 0; i < array.Length; i++)
                array.SetValue(PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserValue(values[i]), i);
            return array;
        }

        public string[] GetCsvValues(object obj, FieldInfo info)
        {
            Array array = info.GetValue(obj) as Array;
            List<string> result = new List<string>();
            foreach (var item in array) result.Add(item.ToString());
            return result.ToArray();
        }
    }

    public class CsvDictionaryParser : ICsvIEnumeralbeParser
    {
        public void SetValue(object obj, FieldInfo info, string[] values)
        {
            if (values.Length % 2 != 0) Debug.LogError($"{info.Name} : The input is incorrect.Please make sure you entered the Dictionary correctly.");

            Type[] elementTypes = info.FieldType.GetGenericArguments();
            MethodInfo methodInfo = info.FieldType.GetMethod("Add");
            for (int i = 0; i < values.Length; i += 2)
            {
                methodInfo.Invoke(info.GetValue(obj), new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementTypes[0]).GetParserValue(values[i]),
                                                                 PrimitiveTypeParser.GetPrimitiveParser(elementTypes[1]).GetParserValue(values[i+1]) });
            }
        }

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
    }
    #endregion 열거형 파싱 End

    public class CsvPairParser : CsvPrimitiveTypeParser
    {
        Type _type;
        public CsvPairParser(Type type)
        {
            _type = type;
        }

        public IEnumerable GetParserEnumerable(string[] values)
        {
            throw new NotImplementedException();
        }

        public Type GetParserType()
        {
            throw new NotImplementedException();
        }

        public object GetParserValue(string value)
        {
            string[] values = value.Split('+');
            if (values.Length != 2) Debug.LogError($"{_type} : The input is incorrect.Please make sure you entered the Key Value pair correctly.");
            Type[] elementTypes = _type.GetGenericArguments();
            ConstructorInfo constructor = _type.GetConstructors()[0];
            return constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementTypes[0]).GetParserValue(values[0]),
                                                             PrimitiveTypeParser.GetPrimitiveParser(elementTypes[1]).GetParserValue(values[1]) });
        }
    }



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
            else return true;
        }
    }
}
