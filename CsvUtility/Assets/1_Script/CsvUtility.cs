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
        string[] columns = SubLastLine(csv).Split('\n');
        CheckFieldNames<T>(GetFieldNames());

        return columns.Skip(1)
                      .Select(x => (T)SetFiledValue(Activator.CreateInstance<T>(), GetCells(x)));

        string[] GetCells(string column) => column.Split(',').Select(x => x.Trim()).ToArray();
        string[] GetFieldNames() => GetCells(columns[0]);
        object SetFiledValue(object obj, string[] values)
        {
            Dictionary<string, int[]> indexsByFieldName = GetIndexsByFieldName();
            foreach (FieldInfo info in GetSerializedFields())
                SetValue(obj, info, GetValues(info));
            return obj;

            // 중첩 함수
            IEnumerable<FieldInfo> GetSerializedFields() => CsvUtility.GetSerializedFields(obj).Where(x => indexsByFieldName.ContainsKey(x.Name));
            string[] GetValues(FieldInfo info) => indexsByFieldName[info.Name].Select(x => values[x]).ToArray();
            Dictionary<string, int[]> GetIndexsByFieldName()
            {
                return GetFieldNames().Distinct().ToDictionary(x => x, x => GetIndexs(x));

                int[] GetIndexs(string fieldName)
                {
                    int[] result = GetFieldNames().Where(cell => cell == fieldName)
                                                  .Select(cell => Array.IndexOf(GetFieldNames(), cell)).ToArray();

                    List<int> upValueIndexs = new List<int>();
                    for (int i = 1; i < result.Length; i++)
                    {
                        if (result[i] == result[i - 1])
                            upValueIndexs.Add(i);
                    }
                    upValueIndexs.ForEach(x => result[x] = result[x - 1] + 1);
                    return result;
                }
            }
        }
    }

    [Conditional("UNITY_EDITOR")]
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

    static void SetValue(object obj, FieldInfo info, string[] values) => CsvParsers.GetParser(info).SetValue(obj, info, values);
    static string SubLastLine(string text) => text.Substring(0, text.Length - 1);
    static IEnumerable<FieldInfo> GetSerializedFields(object obj)
        => obj.GetType()
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
