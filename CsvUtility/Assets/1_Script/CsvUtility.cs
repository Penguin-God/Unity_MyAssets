using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

public static class CsvUtility
{
    public static IEnumerable<T> GetEnumerableFromCsv<T>(string csv)
    {
        string[] columns = SubLastLine(csv).Split('\n');
        return columns.Skip(1)
                      .Select(x => (T)SetFiledValue(Activator.CreateInstance<T>(), GetCells(x)));

        object SetFiledValue(object obj, string[] values)
        {
            Dictionary<string, int[]> indexsByFieldName = SetDict();
            
            foreach (FieldInfo info in GetSerializedFields())
                SetValue(obj, info, GetValues(info));
            return obj;

            // 중첩 함수
            IEnumerable<FieldInfo> GetSerializedFields() => CsvUtility.GetSerializedFields(obj).Where(x => indexsByFieldName.ContainsKey(x.Name));
            string[] GetValues(FieldInfo info) => indexsByFieldName[info.Name].Select(x => values[x]).ToArray();
            Dictionary<string, int[]> SetDict()
            {
                return GetCells(columns[0]).Distinct().ToDictionary(x => x, x => GetIndexs(x));

                int[] GetIndexs(string key)
                {
                    int[] result = GetCells(columns[0]).Where(cell => cell == key)
                                                       .Select(cell => Array.IndexOf(GetCells(columns[0]), cell))
                                                       .ToArray();
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
        string[] GetCells(string column) => column.Split(',').Select(x => x.Trim()).ToArray();
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
