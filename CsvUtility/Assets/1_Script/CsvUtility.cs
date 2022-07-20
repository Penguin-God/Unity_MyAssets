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

class InstanceIEnumerableGenerator<T>
{
    const char comma = ',';
    const char lineBreak = '\n';

    string _csv;
    string[] fieldNames;
    Dictionary<string, int[]> indexsByKey = new Dictionary<string, int[]>();

    string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();
    string[] GetCells(string line, int start, int end) => line.Split(comma).Select(x => x.Trim()).ToList().GetRange(start, end).ToArray();

    public InstanceIEnumerableGenerator(string csv)
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
                    currentIndex++;
                    currentIndex = SetIndexsByKey(info.FieldType, $"{currentKey}{info.Name}->", currentIndex);
                    currentIndex++;
                }
                else
                {
                    indexsByKey.Add(currentKey + info.Name, GetIndexs());
                    currentIndex++;
                }
            }
            return currentIndex;

            int[] GetIndexs()
            {
                List<int> indexs = new List<int>();
                while (true)
                {
                    indexs.Add(currentIndex);

                    if (currentIndex + 1 >= fieldNames.Length || fieldNames[currentIndex] != fieldNames[currentIndex + 1]) break;
                    currentIndex++;
                }
                return indexs.ToArray();
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
                info.SetValue(obj, GetInstance(info.FieldType, cells, $"{current}{info.Name}->"));
            else
                CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(current + info.Name, cells));
        }

        return obj;
    }

    string[] GetFieldValues(string key, string[] cells) => indexsByKey[key].Select(x => cells[x]).ToArray();

    bool InfoIsCustomClass(FieldInfo info)
    {
        string identifier = "System.";
        if (info.FieldType.ToString().StartsWith(identifier)) return false;
        // if (IsEnumerable(info.FieldType.Name) || IsPair(info.FieldType.Name)) return false;
        return true;

        bool IsEnumerable(string typeName) => typeName.Contains("[]") || typeName.Contains("List") || typeName.Contains("Dict");
        bool IsPair(string typeName) => typeName == "KeyValuePair`2";
    }
}

public static class CsvUtility
{
    public static IEnumerable<T> GetEnumerableFromCsv<T>(string csv)
    {
        InstanceIEnumerableGenerator<T> generator = new InstanceIEnumerableGenerator<T>(csv);
        //CheckFieldNames<T>();

        return generator.GetInstanceIEnumerable();
    }

    [Conditional("UNITY_EDITOR")] // TODO : InstanceIEnumerableGenerator 안에 넣기
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
