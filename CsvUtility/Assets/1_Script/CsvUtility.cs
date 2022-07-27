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
    static IEnumerable<FieldInfo> GetSerializedFields(object obj) => GetSerializedFields(obj.GetType());
    public static IEnumerable<FieldInfo> GetSerializedFields(Type type)
    => type
        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .Where(x => CsvSerializedCondition(x));

    static bool CsvSerializedCondition(FieldInfo info) => info.IsPublic || info.GetCustomAttribute(typeof(SerializeField)) != null;


    class CsvLoder<T>
    {
        const char comma = ',';
        const char lineBreak = '\n';
        const char arraw = '>';

        string _csv;
        string[] fieldNames;
        Dictionary<string, List<int>> indexsByKey = new Dictionary<string, List<int>>();

        string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();

        [Conditional("UNITY_EDITOR")]
        void CheckFieldNames(Type type, string[] filedNames)
        {
            List<string> realFieldNames = GetInfoNames(typeof(T));

            for (int i = 0; i < filedNames.Length; i++)
                Debug.Assert(realFieldNames.Contains(filedNames[i]), $"변수명과 일치하지 않는 {i + 1}번째 컬럼명 : {filedNames[i]}");

            List<string> GetInfoNames(Type type)
            {
                List<string> restul = new List<string>();
                foreach (FieldInfo info in CsvUtility.GetSerializedFields(type))
                {
                    restul.Add(info.Name);

                    if (InfoIsCustomClass(info))
                    {
                        if (TypeIdentifier.IsIEnumerable(info.FieldType))
                        {
                            Type elementType = 
                                TypeIdentifier.IsList(info.FieldType) ? info.FieldType.GetGenericArguments()[0] : info.FieldType.GetElementType();
                            restul = restul.Concat(GetInfoNames(elementType)).ToList();
                        }
                        else
                            restul = restul.Concat(GetInfoNames(info.FieldType)).ToList();
                    }
                }
                return restul;
            }
        }

        public CsvLoder(string csv)
        {
            _csv = csv.Substring(0, csv.Length - 1); ;
            fieldNames = GetCells(_csv.Split(lineBreak)[0]);

            indexsByKey.Clear();
            SetIndexsByKey(typeof(T));
            CheckFieldNames(typeof(T), fieldNames);

            int SetIndexsByKey(Type type, string currentKey = "", int currentIndex = 0)
            {
                foreach (FieldInfo info in GetSerializedFields(type))
                {
                    if (InfoIsCustomClass(info)) // 커스텀 클래스 or 구조체면 SetCustom() 내부에서 재귀 돌림
                        currentIndex = SetCustom(currentKey, currentIndex, info);
                    else
                        currentIndex = AddIndexs(info.Name, currentIndex);
                }
                return currentIndex;

                int AddIndexs(string name, int currentIndex)
                {
                    indexsByKey.Add(currentKey + name, GetIndexs(ref currentIndex));

                    currentIndex++;
                    return currentIndex;

                    List<int> GetIndexs(ref int currentIndex)
                    {
                        List<int> indexs = new List<int>();
                        while (true)
                        {
                            indexs.Add(currentIndex);
                            if (currentIndex + 1 >= fieldNames.Length || fieldNames[currentIndex] != fieldNames[currentIndex + 1]) break;
                            currentIndex++;
                        }
                        return indexs;
                    }
                }

                int SetCustom(string currentKey, int currentIndex, FieldInfo info)
                {
                    if (TypeIdentifier.IsIEnumerable(info.FieldType))
                    {
                        int length = fieldNames.Where(x => x == info.Name).Count() - 1;

                        for (int i = 0; i < length; i++)
                        {
                            currentIndex++;
                            currentIndex = SetCustomIEnumeralbe(info.Name, $"{currentKey}{info.Name}{arraw}{i}", currentIndex);
                        }
                        currentIndex++;
                    }
                    else
                    {
                        currentIndex++;
                        currentIndex = SetIndexsByKey(info.FieldType, $"{currentKey}{info.Name}{arraw}", currentIndex);
                        currentIndex++;
                    }

                    return currentIndex;

                    int SetCustomIEnumeralbe(string name, string key, int currentIndex)
                    {
                        while (name != fieldNames[currentIndex])
                            currentIndex = AddIndexs(key + fieldNames[currentIndex], currentIndex);
                        return currentIndex;
                    }
                }
            }
        }

        public IEnumerable<T> GetInstanceIEnumerable() => _csv.Split(lineBreak).Skip(1).Select(x => (T)GetInstance(typeof(T), GetCells(x)));

        object GetInstance(Type type, string[] cells, string current = "")
        {
            object obj = Activator.CreateInstance(type);

            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
            {
                if (InfoIsCustomClass(info)) // 커스텀은 내부에서 재귀 돌림
                    SetCustomValue(cells, current, obj, info);
                else
                    CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(current + info.Name, cells));
            }
            return obj;

            void SetCustomValue(string[] cells, string current, object obj, FieldInfo info)
            {
                if (TypeIdentifier.IsIEnumerable(info.FieldType))
                    SetCsutomIEnumerableValue(obj, GetArray(cells, current, info), info);
                else
                    info.SetValue(obj, GetInstance(info.FieldType, cells, $"{current}{info.Name}{arraw}"));

                // 중첩 함수
                Array GetArray(string[] cells, string current, FieldInfo info)
                {
                    int length = fieldNames.Where(x => x == info.Name).Count() - 1;
                    Type elementType = TypeIdentifier.IsList(info.FieldType) ? info.FieldType.GetGenericArguments()[0] : info.FieldType.GetElementType();
                    Array array = Array.CreateInstance(elementType, length);

                    for (int i = 0; i < length; i++)
                        array.SetValue(GetInstance(elementType, cells, $"{current}{info.Name}{arraw}{i}"), i);
                    return array;
                }

                void SetCsutomIEnumerableValue(object obj, Array array, FieldInfo info)
                {
                    if (TypeIdentifier.IsList(info.FieldType) == false)
                        info.SetValue(obj, array);
                    else
                        info.SetValue(obj, info.FieldType.GetConstructors()[2].Invoke(new object[] { ArrayToIEnumerable(array) }));

                    IEnumerable ArrayToIEnumerable(Array array)
                    {
                        IEnumerable vs;
                        vs = array;
                        return vs;
                    }
                }
            }
        }

        string[] GetFieldValues(string key, string[] cells)
        {
            if (indexsByKey.ContainsKey(key))
                return indexsByKey[key].Select(x => cells[x]).ToArray();
            return new string[] { "" };
        }

        bool InfoIsCustomClass(FieldInfo info)
        {
            string identifier = "System.";
            if (TypeIdentifier.IsList(info.FieldType))
            {
                if (info.FieldType.GetGenericArguments()[0] != null && info.FieldType.GetGenericArguments()[0].ToString().StartsWith(identifier) == false)
                    return true;
            }

            if (info.FieldType.ToString().StartsWith(identifier)) return false;
            return true;
        }
    }

    class CsvSaver<T>
    {
        public string EnumerableToCsv(IEnumerable<T> datas)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Join(",", GetSerializedFields(typeof(T)).Select(x => x.Name)));

            foreach (var data in datas)
            {
                IEnumerable<string> values = GetValues(data);
                stringBuilder.AppendLine(string.Join(",", values));
            }

            return SubLastLine(stringBuilder.ToString());
        }

        IEnumerable<string> GetValues(T data)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(data))
            {
                if(info.FieldType.IsPrimitive || info.FieldType == typeof(string))
                    result.Add(info.GetValue(data).ToString());
                else if (TypeIdentifier.IsIEnumerable(info.FieldType))
                {
                    result = result.Concat(new EnumerableTypeParser().GetIEnumerableValues(data, info)).ToList();
                }
            }
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
