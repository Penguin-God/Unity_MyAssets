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
    public static IEnumerable<T> GetEnumerableFromCsv<T>(string csv)
    {
        CsvLoder<T> generator = new CsvLoder<T>(csv);
        //CheckFieldNames<T>();

        return generator.GetInstanceIEnumerable();
    }

    public static List<T> CsvToList<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToList();
    public static T[] CsvToArray<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToArray();

    [Conditional("UNITY_EDITOR")] // TODO : CsvLoder 안에 넣기
    static void CheckFieldNames<T>(string[] fieldNames)
    {
        string[] fields = GetSerializedFields(Activator.CreateInstance<T>()).Select(x => x.Name).ToArray();
        fieldNames = fieldNames.Distinct().ToArray();

        for (int i = 0; i < fieldNames.Length; i++)
        {
            if (fields.Contains(fieldNames[i]) == false)
                Debug.LogError($"찾을 수 없는 필드명 : {fieldNames[i]}");
        }
    }

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

        public CsvLoder(string csv)
        {
            _csv = csv.Substring(0, csv.Length - 1); ;
            fieldNames = GetCells(_csv.Split(lineBreak)[0]);

            indexsByKey.Clear();
            SetIndexsByKey(typeof(T));

            int SetIndexsByKey(Type type, string currentKey = "", int currentIndex = 0)
            {
                foreach (FieldInfo info in CsvUtility.GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                {
                    if (InfoIsCustomClass(info))
                    {
                        if (IsEnumerable(info.FieldType.ToString()))
                        {
                            int length = fieldNames.Where(x => x == info.Name).Count() - 1;

                            for (int i = 0; i < length; i++)
                            {
                                currentIndex++;
                                SetCustomIEnumeralbe(info.Name, $"{currentKey}{info.Name}{arraw}{i}");
                            }
                            currentIndex++;
                        }
                        else
                        {
                            currentIndex++;
                            currentIndex = SetIndexsByKey(info.FieldType, $"{currentKey}{info.Name}{arraw}", currentIndex);
                            currentIndex++;
                        }
                    }
                    else
                        AddIndexs(info.Name);
                }
                return currentIndex;

                void AddIndexs(string name)
                {
                    //Debug.Log(currentKey + name);
                    indexsByKey.Add(currentKey + name, GetIndexs());
                    currentIndex++;
                }

                List<int> GetIndexs()
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

                void SetCustomIEnumeralbe(string name, string key)
                {
                    while (name != fieldNames[currentIndex])
                        AddIndexs(key + fieldNames[currentIndex]);
                }
            }
        }

        public IEnumerable<T> GetInstanceIEnumerable() => _csv.Split(lineBreak).Skip(1).Select(x => (T)GetInstance(typeof(T), GetCells(x)));

        object GetInstance(Type type, string[] cells, string current = "")
        {
            object obj = Activator.CreateInstance(type);

            foreach (FieldInfo info in CsvUtility.GetSerializedFields(type))
            {
                if (InfoIsCustomClass(info))
                {
                    if (IsEnumerable(info.FieldType.ToString()))
                        SetIEnumerableValue(obj, GetArray(cells, current, info), info);
                    else
                        info.SetValue(obj, GetInstance(info.FieldType, cells, $"{current}{info.Name}{arraw}"));
                }
                else
                    CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(current + info.Name, cells));
            }
            return obj;

            Array GetArray(string[] cells, string current, FieldInfo info)
            {
                int length = fieldNames.Where(x => x == info.Name).Count() - 1;
                Type elementType = IsList(info.FieldType.ToString()) ? info.FieldType.GetGenericArguments()[0] : info.FieldType.GetElementType();
                Array array = Array.CreateInstance(elementType, length);

                for (int i = 0; i < length; i++)
                    array.SetValue(GetInstance(elementType, cells, $"{current}{info.Name}{arraw}{i}"), i);
                return array;



            }

            void SetIEnumerableValue(object obj, Array array, FieldInfo info)
            {

                if (IsList(info.FieldType.ToString()) == false)
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

        string[] GetFieldValues(string key, string[] cells) => indexsByKey[key].Select(x => cells[x]).ToArray();

        bool InfoIsCustomClass(FieldInfo info)
        {
            string identifier = "System.";
            if (IsList(info.FieldType.ToString()))
            {
                if (info.FieldType.GetGenericArguments()[0] != null && info.FieldType.GetGenericArguments()[0].ToString().StartsWith(identifier) == false)
                    return true;
            }

            if (info.FieldType.ToString().StartsWith(identifier)) return false;
            return true;
        }
        bool IsEnumerable(string typeName) => typeName.Contains("[]") || typeName.Contains("List");
        bool IsList(string typeName) => typeName.Contains("List");
    }

    class CsvSaver<T>
    {
        public static string EnumerableToCsv<T>(IEnumerable<T> datas)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Join(",", GetSerializedFields(datas.First()).Select(x => x.Name)));

            foreach (var data in datas)
            {
                IEnumerable<string> values = GetSerializedFields(data).Select(x => x.GetValue(data).ToString());
                stringBuilder.AppendLine(string.Join(",", values));
            }

            return SubLastLine(stringBuilder.ToString());
        }

        public static void SaveCsv(string text, string filePath)
        {
            Stream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            StreamWriter outStream = new StreamWriter(fileStream, Encoding.UTF8);
            outStream.Write(text);
            outStream.Close();
        }

        public static void SaveCsv<T>(IEnumerable<T> enumerable, string filePath)
        {
            Stream fileStream = new FileStream($"{filePath}.csv", FileMode.Create, FileAccess.Write);
            StreamWriter outStream = new StreamWriter(fileStream, Encoding.UTF8);
            outStream.Write(EnumerableToCsv<T>(enumerable));
            outStream.Close();
        }

    }
}
