using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


[Serializable]
public class TestClass
{
    [SerializeField] int TT;
    [SerializeField] bool Ta;
    [SerializeField] HasTestClass[] hasTestClass;
    [SerializeField] string[] kkkk;
    [SerializeField] string sktt1;
}

[Serializable]
public struct HasTestClass
{
    [SerializeField] public int aaa;
    [SerializeField] public string AAA;
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

    [SerializeField] HasTestClass hasClass;
    [SerializeField] HasTestClass[] hasClassArray;

    public bool IsSuccess()
    {
        return CheckSame(number, 123) && CheckSame(text, "Hello World") && CheckSame(actualNumber, 23.123f) && CheckSame(boolean, true)
            && CheckSame(textNumberPair.Key, "사무라이 하트") && CheckSame(textNumberPair.Value, 243) && CheckSame(booleanActualNumberPair.Key, false)
            && CheckSame(booleanActualNumberPair.Value, 22.411f) && CheckArraySame(numberArray, new int[] { 341235, 13123 })
            && CheckArraySame(textArray, new string[] { "고타에", "라헤야" }) && CheckArraySame(actualNumberArray, new float[] { 4224.12f, 1245.12f })
            && CheckArraySame(booleanArray, new bool[] { true, false }) && CheckListSame(numberList, new List<int>() { 341235, 13123 })
            && CheckListSame(textList, new List<string>() { "고타에", "라헤야" }) && CheckListSame(actualNumberList, new List<float>() { 4224.12f, 1245.12f })
            && CheckListSame(booleanList, new List<bool>() { true, false }) && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(2, "조찬자"))
            && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(12432, "아 루즈 마이셀프"))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(2134.22f, true))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(11.11f, false))
            && HasClassIsSame(hasClass);
    }

    // TODO : 틀렸을 때 정보도 LogError에 띄우기
    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue.CompareTo(value) != 0)
        {
            Debug.LogError("서로 달라요!!!!!!!!!");
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

    bool HasClassIsSame(HasTestClass testClass) => 777 == testClass.aaa && "안녕하세요." == testClass.AAA;
    bool HasClassEnumerableIsSame(IEnumerable<HasTestClass> hasClass)
    {
        foreach (HasTestClass item in hasClass)
        {
            if (HasClassIsSame(item) == false)
                return false;
        }
        return true;
    }
}

[Serializable]
public class SaveTestCalss
{
    [SerializeField] public int aaa = 123;
    [SerializeField] public string AAA = "안녕 세상";
    [SerializeField] public string[] test1 = new string[] { "22", "fasdasd" };
    [SerializeField] public List<int> test2 = new List<int>() { 1, 23, 123 };
    public Dictionary<float, bool> test3 = new Dictionary<float, bool>();
}

public class Test : MonoBehaviour
{
    [SerializeField] MasterTest[] masterTests;
    [SerializeField] TextAsset asset;

    [ContextMenu("Master Test")]
    void MasterTest()
    {
        masterTests = CsvUtility.CsvToArray<MasterTest>(asset.text).ToArray();
        if (masterTests.All(x => x.IsSuccess())) print("GOOD!!");
        else print("Bad!!");
    }

    [SerializeField] List<TestClass> testClassList = new List<TestClass>();
    [SerializeField] TextAsset testCsv;
    [SerializeField] SaveTestCalss saveTest;

    [ContextMenu("Test")]
    void Testss()
    {
        saveTest.test3.Add(11.22f, false);
        saveTest.test3.Add(331.2555f, false);
        saveTest.test3.Add(11123.22f, false);
        foreach (var info in saveTest.GetType().GetFields())
        {
            if (info.FieldType.IsArray)
            {
                Array array = info.GetValue(saveTest) as Array;
                foreach (var item in array)
                {
                    print(item);
                }
            }

            if (info.FieldType.IsGenericType && (info.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                IList array = info.GetValue(saveTest) as IList;
                foreach (var item in array)
                {
                    print(item);
                }
            }

            if(info.FieldType.IsGenericType && info.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                print(saveTest.test3.Count);
                IDictionary array = info.GetValue(saveTest) as IDictionary;
                foreach (var item in array.Keys)
                {
                    print(item);
                }
                foreach (var item in array.Values)
                {
                    print(item);
                }
            }
            print(info.GetValue(saveTest));
        }
    }
}