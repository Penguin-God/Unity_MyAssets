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
    public static IEnumerable<T> GetEnumerableFromCsv<T>(string csv) => new CsvLoder<T>(csv).GetInstanceIEnumerable();
    public static List<T> CsvToList<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToList();
    public static T[] CsvToArray<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToArray();

    public static string EnumerableToCsv<T>(IEnumerable<T> datas) => new CsvSaver<T>().EnumerableToCsv(datas);
    public static void EnumerableSaveByCsvFile<T>(IEnumerable<T> datas, string path) => new CsvSaver<T>().Save(datas, path);

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
        const char lineBreak = '\n';

        string _csv;
        string[] fieldNames;

        string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();

        [Conditional("UNITY_EDITOR")]
        void CheckFieldNames(Type type, string[] filedNames)
        {
            List<string> realFieldNames = GetSerializedFieldNames(typeof(T));

            for (int i = 0; i < filedNames.Length; i++)
                Debug.Assert(realFieldNames.Contains(filedNames[i]), $"변수명과 일치하지 않는 {i + 1}번째 컬럼명 : {filedNames[i]}");
        }

        public CsvLoder(string csv)
        {
            _csv = csv.Substring(0, csv.Length - 1); ;
            fieldNames = GetCells(_csv.Split(lineBreak)[0]);
            CheckFieldNames(typeof(T), fieldNames);
        }

        Dictionary<string, int> GetCountByFieldName(Type type, string[] fieldNames)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                result.Add(info.Name, GetCount(info));
            return result;

            int GetCount(FieldInfo info)
                => TypeIdentifier.IsCustom(info.FieldType) ? fieldNames.Count(x => x == info.Name) - 1 : fieldNames.Count(x => x == info.Name);
        }

        public IEnumerable<T> GetInstanceIEnumerable() => _csv.Split(lineBreak).Skip(1).Select(x => (T)GetInstance(typeof(T), GetCells(x)));

        object GetInstance(Type type, string[] cells)
        {
            object obj = Activator.CreateInstance(type);
            
            SetInfoValue(obj, type, fieldNames);
            return obj;

            int SetInfoValue(object obj, Type type, string[] fieldNames, int currentIndex = 0)
            {
                int cellIndex = currentIndex;
                Dictionary<string, int> countByKey = GetCountByFieldName(type, fieldNames);

                foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                {
                    if (TypeIdentifier.IsCustom(info.FieldType))
                        info.SetValue(obj, GetCustomValue(info, ref cellIndex));
                    else
                    {
                        CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(countByKey[info.Name], cells, cellIndex));
                        cellIndex += countByKey[info.Name];
                    }
                }
                return cellIndex;

                object GetCustomValue(FieldInfo info, ref int cellIndex)
                {
                    cellIndex++;
                    if (TypeIdentifier.IsIEnumerable(info.FieldType))
                        return GetArray(obj, info, ref cellIndex);
                    else
                        return GetSingleCustomValue(info, ref cellIndex);
                }
            }

            object GetSingleCustomValue(FieldInfo info, ref int cellIndex)
            {
                object customValue = Activator.CreateInstance(GetCoustomType(info.FieldType));
                cellIndex = SetInfoValue(customValue, GetCoustomType(info.FieldType), GetCustomCells(info, cellIndex), cellIndex);
                cellIndex++;
                return customValue;
            }

            string[] GetCustomCells(FieldInfo info, int index)
            {
                int indexof = Array.IndexOf(fieldNames.Skip(index).ToArray(), info.Name);
                return fieldNames.Skip(index).Take(indexof).ToArray();
            }

            Array GetArray(object obj, FieldInfo info, ref int currentIndex)
            {
                int length = fieldNames.Where(x => x == info.Name).Count() - 1;
                Type elementType = GetElementType(info.FieldType);

                Array array = Array.CreateInstance(elementType, length);
                for (int i = 0; i < length; i++)
                    array.SetValue(GetSingleCustomValue(info, ref currentIndex), i);

                info.SetValue(obj, GetIEnumerableValue(info.FieldType, array));
                return array;
            }

            object GetIEnumerableValue(Type type, Array array) => (type.IsArray) ? array : type.GetConstructors()[2].Invoke(new object[] { array });
        }

        string[] GetFieldValues(int count, string[] cells, int currentIndex)
        {
            string[] result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = cells[currentIndex + i];
            return result;
        }
    }

    class CsvSaver<T>
    {
        public string EnumerableToCsv(IEnumerable<T> datas)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<string, int> countByName = GetCountByName(datas);
            stringBuilder.AppendLine(string.Join(",", GetFirstRow(typeof(T), countByName)));

            foreach (var data in datas)
            {
                IEnumerable<string> values = GetValues(data, countByName);
                stringBuilder.AppendLine(string.Join(",", values));
            }

            return SubLastLine(stringBuilder.ToString());
        }

        List<string> GetFirstRow(object type, Dictionary<string, int> countByName)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(type))
            {
                if (TypeIdentifier.IsCustom(info.FieldType))
                    result = GetCustomConcat(countByName, result, info, info.Name);
                else
                {
                    for (int i = 0; i < countByName[info.Name]; i++)
                        result.Add(info.Name);
                }
            }
            return result;

            List<string> GetCustomConcat(Dictionary<string, int> countByName, List<string> result, FieldInfo info, string blank)
            {
                result.Add(blank);
                int length = TypeIdentifier.IsIEnumerable(info.FieldType) ? countByName[blank] : 1;
                for (int i = 0; i < length; i++)
                    result = GetCustomList(result, () => GetFirstRow(GetElementType(info.FieldType), countByName), info.Name);
                return result;
            }
        }

        Dictionary<string, int> GetCountByName(IEnumerable<T> datas)
        {
            Dictionary<string, int> countByName = GetSerializedFieldNames(typeof(T)).ToDictionary(x => x, x => 0);

            foreach (T data in datas)
                SetDict(countByName, data);
            return countByName;

            void SetDict(Dictionary<string, int> countByName, object data)
            {
                foreach (FieldInfo info in GetSerializedFields(data))
                {
                    if (TypeIdentifier.IsCustom(info.FieldType))
                    {
                        if (TypeIdentifier.IsIEnumerable(info.FieldType))
                        {
                            int count = 0;
                            foreach (var item in info.GetValue(data) as IEnumerable)
                            {
                                SetDict(countByName, item);
                                count++;
                            }
                            
                            if (count > countByName[info.Name])
                                countByName[info.Name] = count;
                        }
                        else
                            SetDict(countByName, info.GetValue(data));
                    }
                    if (GetValueLength(data, info) > countByName[info.Name])
                        countByName[info.Name] = GetValueLength(data, info);
                }
            }
        }

        IEnumerable<string> GetValues(object data, Dictionary<string, int> countByName)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(data.GetType()))
            {
                if (IsPrimitive(info.FieldType))
                    result.Add(info.GetValue(data).ToString());
                else if (TypeIdentifier.IsCustom(info.FieldType))
                    result = GetCustomConcat(data, countByName, result, info);
                else if (TypeIdentifier.IsIEnumerable(info.FieldType))
                {
                    IEnumerable<string> values = new EnumerableTypeParser().GetIEnumerableValues(data, info);
                    result = GetConcatList(result, values);
                    AddBlank(countByName[info.Name] - values.Count());
                }
            }
            return result;

            List<string> GetCustomConcat(object data, Dictionary<string, int> countByName, List<string> result, FieldInfo info)
            {
                result.Add("");
                if (TypeIdentifier.IsIEnumerable(info.FieldType))
                {
                    foreach (var item in info.GetValue(data) as IEnumerable)
                        result = GetCustomList(result, () => GetValues(item, countByName));
                }
                else
                    result = GetCustomList(result, () => GetValues(info.GetValue(data), countByName));

                return result;
            }

            void AddBlank(int blankCount = 1)
            {
                for (int i = 0; i < blankCount; i++)
                    result.Add("");
            }
        }

        List<string> GetCustomList(List<string> result, Func<IEnumerable<string>> OriginFunc, string blank = "")
        {
            result = GetConcatList(result, OriginFunc());
            result.Add(blank);
            return result;
        }

        int GetValueLength(object data, FieldInfo info)
        {
            if (IsPrimitive(info.FieldType))
                return 1;
            else if (TypeIdentifier.IsIEnumerable(info.FieldType))
                return new EnumerableTypeParser().GetIEnumerableValues(data, info).Length;
            return 0;
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
