using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

[Serializable]
public class TestClass
{
    [SerializeField] int TT;
    [SerializeField] bool Ta;
    [SerializeField] HasTestClass hasTestClass;
    [SerializeField] string kkkk;
    [SerializeField] string sktt1;
}

[Serializable]
public class HasTestClass
{
    [SerializeField] int aaa;
    [SerializeField] string AAA;
}

[Serializable]
public class MasterTest
{
    [SerializeField] int number;
    [SerializeField] string text;
    [SerializeField] float actualNumber;
    [SerializeField] bool boolean;
    [SerializeField] KeyValuePair<string, int> textNumberPair;
    [SerializeField] KeyValuePair<bool, float> booleanActualNumberPair;
    
    [SerializeField] int[] numberArray;
    [SerializeField] string[] textArray;
    [SerializeField] float[] actualNumberArray;
    [SerializeField] bool[] booleanArray;
    
    [SerializeField] List<int> numberList;
    [SerializeField] List<string> textList;
    [SerializeField] List<float> actualNumberList;
    [SerializeField] List<bool> booleanList;
    
    [SerializeField] Dictionary<int, string> numberByText = new Dictionary<int, string>();
    [SerializeField] Dictionary<float, bool> actualNumberByBoolean = new Dictionary<float, bool>();
    
    public bool IsSuccess()
    {
        return CheckSame(number, 123) && CheckSame(text, "Hello World") && CheckSame(actualNumber, 23.123f) && CheckSame(boolean, true)
            && CheckSame(textNumberPair.Key, "�繫���� ��Ʈ") && CheckSame(textNumberPair.Value, 243) && CheckSame(booleanActualNumberPair.Key, false)
            && CheckSame(booleanActualNumberPair.Value, 22.411f) && CheckArraySame(numberArray, new int[] { 341235, 13123 })
            && CheckArraySame(textArray, new string[] { "��Ÿ��", "�����" }) && CheckArraySame(actualNumberArray, new float[] { 4224.12f, 1245.12f })
            && CheckArraySame(booleanArray, new bool[] { true, false }) && CheckListSame(numberList, new List<int>() { 341235, 13123 })
            && CheckListSame(textList, new List<string>() { "��Ÿ��", "�����" }) && CheckListSame(actualNumberList, new List<float>() { 4224.12f, 1245.12f })
            && CheckListSame(booleanList, new List<bool>() { true, false }) && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(2, "������"))
            && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(12432, "�� ���� ���̼���"))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(2134.22f, true))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(11.11f, false));
    }

    // TODO : Ʋ���� �� ������ LogError�� ����
    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue.CompareTo(value) != 0)
        {
            Debug.LogError("���� �޶��!!!!!!!!!");
            Debug.Log($"{parsingValue} : {value}");
            return false;
        }

        return true;
    }

    bool CheckArraySame<T>(T[] parsingValue, T[] value) where T : IComparable
    {
        if (parsingValue.Length != value.Length) return false;

        for (int i = 0; i < parsingValue.Length; i++)
        {
            if (parsingValue[i].CompareTo(value[i]) != 0)
                return false;
        }

        return true;
    }

    bool CheckListSame<T>(List<T> parsingValue, List<T> value) where T : IComparable
    {
        if (parsingValue.Count != value.Count) return false;

        for (int i = 0; i < parsingValue.Count; i++)
        {
            if (parsingValue[i].CompareTo(value[i]) != 0)
                return false;
        }

        return true;
    }
    
    bool CheckDictionarySame<T, T2>(Dictionary<T, T2> parsingValue, KeyValuePair<T, T2> value) where T : IComparable where T2 : IComparable
    {
        if (parsingValue.TryGetValue(value.Key, out T2 t2) == false) return false;
        if (t2.CompareTo(value.Value) != 0) return false;

        return true;
    }
}

public class Test : MonoBehaviour
{
    [SerializeField] MasterTest[] masterTests;
    [SerializeField] TextAsset asset;

    [ContextMenu("Master Test")]
    void MasterTest()
    {
        masterTests = CsvUtility.GetEnumerableFromCsv<MasterTest>(asset.text).ToArray();
        if (masterTests.All(x => x.IsSuccess())) print("GOOD!!");
        else print("Bad!!");
    }

    [SerializeField] string aa;
    [SerializeField] TestClass[] testClass;
    [SerializeField] TestClass test;
    [SerializeField] TextAsset testCsv;
    [ContextMenu("Test")]
    void Testss()
    {
        testClass = null;
        testClass = CsvToList<TestClass>(testCsv.text);
    }

    T[] CsvToList<T>(string csv)
    {
        string[] columns = csv.Substring(0, csv.Length - 1).Split('\n');
        Dictionary<string, int[]> dict = GetIndexsByKey(Activator.CreateInstance<T>(), GetCells(columns[0]));
        return columns.Skip(1)
                      .Select(x => (T)ClassParsing(dict, typeof(T), GetCells(x)))
                      .ToArray();

        object ClassParsing(Dictionary<string, int[]> dict, Type type, string[] cells, string current = "")
        {
            object obj = Activator.CreateInstance(type);

            foreach (FieldInfo info in GetSerializedFields(obj))
            {
                if (info.FieldType.IsPrimitive == false && typeof(string) != info.FieldType && info.GetType().IsClass)
                    info.SetValue(obj, ClassParsing(dict, info.FieldType, cells, $"{current}{info.Name}->"));
                else
                    CsvParsers.GetParser(info).SetValue(obj, info, new string[] { cells[dict[current + info.Name][0]] });
            }

            return obj;
        }

        Dictionary<string, int[]> GetIndexsByKey(object obj, string[] fieldNames)
        {
            Dictionary<string, int[]> indexsByKey = new Dictionary<string, int[]>();
            indexsByKey.Clear();
            SetDict(obj, indexsByKey, "", 0, fieldNames);
            return indexsByKey;

            int SetDict(object obj, Dictionary<string, int[]> dict, string currentKey, int currentIndex, string[] fieldNames)
            {
                foreach (var info in GetSerializedFields(obj).Where(x => fieldNames.Contains(x.Name)))
                {
                    if (info.FieldType.IsPrimitive == false && typeof(string) != info.FieldType && info.GetType().IsClass)
                    {
                        string className = fieldNames[currentIndex];
                        currentIndex++;
                        currentIndex = SetDict(Activator.CreateInstance(info.FieldType), dict, $"{currentKey}{info.Name}->", currentIndex, fieldNames);
                        currentIndex++;
                    }
                    else
                    {
                        dict.Add(currentKey + info.Name, new int[] { currentIndex });
                        currentIndex++;
                    }
                }
                return currentIndex;
            }
        }
        string[] GetCells(string column) => column.Split(',').Select(x => x.Trim()).ToArray();
    }

    IEnumerable<FieldInfo> GetSerializedFields(object obj)
    => obj.GetType()
        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .Where(x => CsvSerializedCondition(x));
    bool CsvSerializedCondition(FieldInfo info) => info.IsPublic || info.GetCustomAttribute(typeof(SerializeField)) != null;
}
