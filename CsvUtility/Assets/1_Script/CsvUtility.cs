using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;



public static class CsvUtility
{
    public class CsvSaveOption
    {
        [SerializeField] int _arrayCount;
        [SerializeField] int _listCount;
        [SerializeField] int _dictionaryCount;

        public int ArrayCount => (_arrayCount > 0) ? _arrayCount : 1;
        public int ListCount => (_listCount > 0) ? _listCount : 1;
        public int DitionaryCount => (_dictionaryCount > 0) ? _dictionaryCount : 1;

        public CsvSaveOption()
        {
            _arrayCount = 1;
            _listCount = 1;
            _dictionaryCount = 1;
        }

        public CsvSaveOption(int arrayCount, int listCount = 1, int dictionaryCount = 1)
        {
            _arrayCount = arrayCount;
            _listCount = listCount;
            _dictionaryCount = dictionaryCount;
        }
    }

    public static IEnumerable<T> GetEnumerableFromCsv<T>(string csv) => new CsvLoder<T>(csv).GetInstanceIEnumerable();
    public static List<T> CsvToList<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToList();
    public static T[] CsvToArray<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToArray();

    public static string EnumerableToCsv<T>(IEnumerable<T> datas, int arrayLength = 1, int listLength = 1, int dictionaryLength = 1) 
        => GetSaver<T>(arrayLength, listLength, dictionaryLength).EnumerableToCsv(datas);
    public static void SaveCsv<T>(IEnumerable<T> datas, string path, int arrayLength = 1, int listLength = 1, int dictionaryLength = 1)
        => GetSaver<T>(arrayLength, listLength, dictionaryLength).Save(datas, path);
    static CsvSaver<T> GetSaver<T>(int arrayLength, int listLength, int dictionaryLength) => new CsvSaver<T>(arrayLength, listLength, dictionaryLength);


    static string SubLastLine(string text) => text.Substring(0, text.Length - 1);
    static IEnumerable<FieldInfo> GetSerializedFields(object obj)
    {
        Type type = obj as Type;
        return type == null ? GetSerializedFields(obj.GetType()) : GetSerializedFields(type);
    }
    public static IEnumerable<FieldInfo> GetSerializedFields(Type type)
    => type
        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .Where(x => CsvSerializedCondition(x));

    static bool CsvSerializedCondition(FieldInfo info) => info.IsPublic || info.GetCustomAttribute(typeof(SerializeField)) != null;

    static Type GetElementType(Type type) => type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
    static Type GetCoustomType(Type type) => TypeIdentifier.IsIEnumerable(type) ? GetElementType(type) : type;
    static List<string> GetConcatList(List<string> origin, IEnumerable<string> addValue) => origin.Concat(addValue).ToList();
    static List<string> GetSerializedFieldNames(Type type)
    {
        List<string> restul = new List<string>();
        foreach (FieldInfo info in GetSerializedFields(type))
        {
            restul.Add(info.Name);

            if (TypeIdentifier.IsCustom(info.FieldType))
                restul = GetConcatList(restul, GetSerializedFieldNames(GetCoustomType(info.FieldType)));
        }
        return restul;
    }

    static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string) || type.IsEnum;

    class CsvLoder<T>
    {
        const char comma = ',';
        const char mark = '\"';
        const char replaceMark = '+';
        const char lineBreak = '\n';

        string _csv;
        string[] fieldNames;

        string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();

        List<string> GetValueList(string line)
        {
            string[] tokens = line.Split(mark);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (i % 2 == 1)
                    tokens[i] = tokens[i].Replace(',', replaceMark);
            }
            return GetCells(string.Join("", tokens).Replace("\"", "")).ToList();
        }

        [Conditional("UNITY_EDITOR")]
        void CheckFieldNames(Type type, string[] filedNames)
        {
            List<string> realFieldNames = GetSerializedFieldNames(typeof(T));

            for (int i = 0; i < filedNames.Length; i++)
                Debug.Assert(realFieldNames.Contains(filedNames[i]), $"변수명과 일치하지 않는 {i + 1}번째 컬럼명 : {filedNames[i]}");
        }

        public CsvLoder(string csv)
        {
            _csv = csv.Substring(0, csv.Length - 1);
            fieldNames = GetCells(_csv.Split(lineBreak)[0]);
            CheckFieldNames(typeof(T), fieldNames);
        }

        Dictionary<string, int> GetCountByFieldName(Type type, string[] fieldNames)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                result.Add(info.Name, GetCount(info));
            return result;

            int GetCount(FieldInfo _info)
                => TypeIdentifier.IsCustom(_info.FieldType) ? fieldNames.Count(x => x == _info.Name) - 1 : fieldNames.Count(x => x == _info.Name);
        }

        public IEnumerable<T> GetInstanceIEnumerable() => _csv.Split(lineBreak).Skip(1).Select(x => (T)GetInstance(typeof(T), GetValueList(x)));

        object GetInstance(Type type, List<string> cells)
        {
            cells.RemoveAll(x => string.IsNullOrEmpty(x));
            return SetInfoValue(type, fieldNames, cells);

            // TODO : 클래스 배열 문제 해결하기
            string[] GetCustomCells(FieldInfo info, int index)
            {
                int indexof = Array.IndexOf(fieldNames.Skip(index).ToArray(), info.Name);
                fieldNames.Skip(index).Take(indexof).ToList().ForEach(x => Debug.Log(x));
                return fieldNames.Skip(index).Take(indexof).ToArray();
            }
        }

        object SetInfoValue(Type type, string[] fieldNames, List<string> cells)
        {
            object obj = Activator.CreateInstance(type);
            Dictionary<string, int> countByKey = GetCountByFieldName(type, fieldNames);

            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
            {
                if (TypeIdentifier.IsCustom(info.FieldType))
                    info.SetValue(obj, GetCustomValue(info, cells));
                else
                    CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(countByKey[info.Name], cells));
            }
            return obj;
        }

        object GetCustomValue(FieldInfo _info, List<string> cells)
        {
            if (TypeIdentifier.IsIEnumerable(_info.FieldType))
                return GetArray(_info, cells);
            else
                return GetSingleCustomValue(_info.FieldType, cells);
        }

        object GetSingleCustomValue(Type type, List<string> cells) => SetInfoValue(GetCoustomType(type), type.GetFields().Select(x => x.Name).ToArray(), cells);

        object GetArray(FieldInfo info, List<string> cells)
        {
            int length = fieldNames.Where(x => x == info.Name).Count() - 1;
            Type elementType = GetElementType(info.FieldType);

            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; i++)
                array.SetValue(GetSingleCustomValue(elementType, cells), i);

            return GetIEnumerableValue(info.FieldType, array);
        }

        object GetIEnumerableValue(Type type, Array array) => (type.IsArray) ? array : type.GetConstructors()[2].Invoke(new object[] { array });

        string[] GetFieldValues(int count, List<string> cells)
        {
            string[] result = new string[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = cells[0];
                cells.RemoveAt(0);
            }

            return result;
        }
    }

    class CsvSaver<T>
    {
        CsvSaveOption _option;
        
        Dictionary<Type, int> GetCountByType(IEnumerable<T> datas)
        {
            Dictionary<Type, int> countByType = new Dictionary<Type, int>();
            foreach (T data in datas)
            {
                foreach (FieldInfo info in GetSerializedFields(data))
                {
                    if (TypeIdentifier.IsCustom(info.FieldType))
                    {
                        if (countByType.ContainsKey(info.FieldType) == false)
                            countByType.Add(info.FieldType, 1);

                        if (TypeIdentifier.IsIEnumerable(info.FieldType))
                        {
                            int count = 0;
                            foreach (var item in info.GetValue(data) as IEnumerable)
                                count++;
                            if (count > countByType[info.FieldType])
                                countByType[info.FieldType] = count;
                        }
                    }
                }
            }
            return countByType;
        }

        public CsvSaver(int arrayLength, int listLength, int dictionaryLength) => _option = new CsvSaveOption(arrayLength, listLength, dictionaryLength);

        int GetOptionCount(Type type)
        {
            if (TypeIdentifier.IsIEnumerable(type) == false) return 1;
            else if (type.IsArray) return _option.ArrayCount;
            else if (TypeIdentifier.IsList(type)) return _option.ListCount;
            else if (TypeIdentifier.IsDictionary(type)) return _option.DitionaryCount;
            else
            {
                Debug.LogWarning("정의할 수 없는 타입");
                return 1;
            }
        }

        public string EnumerableToCsv(IEnumerable<T> datas)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Join(",", GetFirstRow(typeof(T), GetCountByType(datas))));

            foreach (var data in datas)
            {
                IEnumerable<string> values = GetValues(data);
                stringBuilder.AppendLine(string.Join(",", values));
            }

            return SubLastLine(stringBuilder.ToString());
        }

        List<string> GetFirstRow(object type, Dictionary<Type, int> countByType)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(type))
            {
                if (TypeIdentifier.IsCustom(info.FieldType))
                    result = GetCustomConcat(result, info, info.Name, countByType);
                else
                {
                    for (int i = 0; i < GetOptionCount(info.FieldType); i++)
                        result.Add(info.Name);
                }
            }
            return result;
        }

        List<string> GetCustomConcat(List<string> result, FieldInfo info, string blank, Dictionary<Type, int> countByType)
        {
            result.Add(blank);
            int length = countByType[info.FieldType];

            for (int i = 0; i < length; i++)
                result = GetCustomList(result, () => GetFirstRow(GetElementType(info.FieldType), countByType), info.Name);
            return result;
        }

        IEnumerable<string> GetValues(object data)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(data.GetType()))
            {
                if (IsPrimitive(info.FieldType))
                    result.Add(info.GetValue(data).ToString());
                else if (TypeIdentifier.IsCustom(info.FieldType))
                    result = GetCustomConcat(data, result, info);
                else if (TypeIdentifier.IsIEnumerable(info.FieldType))
                {
                    result = 
                        GetConcatList(result, GetIEnumerableValue(GetOptionCount(info.FieldType), new EnumerableTypeParser().GetIEnumerableValues(data, info)));
                }
            }
            return result;
        }
        string[] GetIEnumerableValue(int count, string[] values)
        {
            string[] result = new string[count];
            int[] counts = GetCounts(count, values.Length);
            int current = 0;
            for (int i = 0; i < count; i++)
            {
                result[i] = GetValue(values.Skip(current).Take(counts[i]));
                current += counts[i];
            }

            return result;
        }

        int[] GetCounts(int count, int valueLength)
        {
            int length = valueLength;
            int[] counts = new int[count];
            while (length > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    counts[i]++;
                    length--;
                    if (length <= 0) break;
                }
            }

            return counts;
        }

        string GetValue(IEnumerable<string> values)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\"");
            stringBuilder.Append(string.Join(",", values));
            stringBuilder.Append("\"");
            return stringBuilder.ToString();
        }

        List<string> GetCustomConcat(object data, List<string> result, FieldInfo info)
        {
            result.Add("");
            if (TypeIdentifier.IsIEnumerable(info.FieldType))
            {
                foreach (var item in info.GetValue(data) as IEnumerable)
                    result = GetCustomList(result, () => GetValues(item));
            }
            else
                result = GetCustomList(result, () => GetValues(info.GetValue(data)));

            return result;
        }



        List<string> GetCustomList(List<string> result, Func<IEnumerable<string>> OriginFunc, string blank = "")
        {
            result = GetConcatList(result, OriginFunc());
            result.Add(blank);
            return result;
        }

        public void Save(IEnumerable<T> enumerable, string filePath)
        {
            Stream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            StreamWriter outStream = new StreamWriter(fileStream, Encoding.UTF8);
            outStream.Write(EnumerableToCsv(enumerable));
            outStream.Close();
        }
    }
}
