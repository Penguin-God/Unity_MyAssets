using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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
    [SerializeField] TestType testType;

    public bool IsSuccess()
    {
        return CheckSame(number, 123) && CheckSame(text, "Hello World") && CheckSame(actualNumber, 23.123f) && CheckSame(boolean, true)
            && CheckSame(textNumberPair.Key, "사무라이 하트") && CheckSame(textNumberPair.Value, 243) && CheckSame(booleanActualNumberPair.Key, false)
            && CheckSame(booleanActualNumberPair.Value, 22.411f) && CheckArraySame(numberArray, new int[] { 341235, 13123 })
            && CheckArraySame(textArray, new string[] { "고타에", "안녕", "라헤야" }) && CheckArraySame(actualNumberArray, new float[] { 4224.12f, 1245.12f })
            && CheckArraySame(booleanArray, new bool[] { true, false }) && CheckListSame(numberList, new List<int>() { 341235, 13123 })
            && CheckListSame(textList, new List<string>() { "고타에", "라헤야" }) && CheckListSame(actualNumberList, new List<float>() { 4224.12f, 1245.12f })
            && CheckListSame(booleanList, new List<bool>() { true, false }) && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(2, "조찬자"))
            && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(12432, "아 루즈 마이셀프"))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(2134.22f, true))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(11.11f, false))
            && HasClassIsSame(hasClass) && HasClassEnumerableIsSame(hasClassArray)
            && CheckSame(testType, TestType.Devlop);
    }

    // TODO : 틀렸을 때 정보도 LogError에 띄우기
    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue.CompareTo(value) != 0)
        {
            // Debug.LogError("서로 달라요!!!!!!!!!");
            Debug.LogError($"서로 달라요!!!!!!!!! \n 파싱한 값 : {parsingValue} :  정답 : {value}");
            return false;
        }

        return true;
    }

    bool CheckArraySame<T>(T[] parsingValue, T[] value) where T : IComparable
    {
        if (parsingValue.Length != value.Length) return false;

        for (int i = 0; i < parsingValue.Length; i++)
        {
            if (CheckSame(parsingValue[i], value[i]) == false)
                return false;
        }

        return true;
    }

    bool CheckListSame<T>(List<T> parsingValue, List<T> value) where T : IComparable
    {
        if (parsingValue.Count != value.Count) return false;

        for (int i = 0; i < parsingValue.Count; i++)
        {
            if (CheckSame(parsingValue[i], value[i]) == false)
                return false;
        }

        return true;
    }
    
    bool CheckDictionarySame<T, T2>(Dictionary<T, T2> parsingValue, KeyValuePair<T, T2> value) where T : IComparable where T2 : IComparable
    {
        if (parsingValue.TryGetValue(value.Key, out T2 t2) == false)
        {
            Debug.LogError($"딕셔너리에 {value.Key} 라는 키가 있어야 되는데 없어요!!!");
            return false;
        }
        if (CheckSame(t2, value.Value) == false) return false;

        return true;
    }

    bool HasClassIsSame(HasTestClass testClass) => CheckSame(testClass.aaa, 777) && CheckSame(testClass.AAA, "안녕하세요.");
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
    [SerializeField] public int aaab = 123;
    [SerializeField] public string AAAb = "안녕 세상";
    [SerializeField] public string[] test1 = new string[] { "22", "fasdasd" };
    [SerializeField] public List<int> test2 = new List<int>() { 1, 23, 123 };
    public Dictionary<string, bool> test3 = new Dictionary<string, bool>();
    public TestType testType = TestType.Devlop;
    [SerializeField] HasTestClass hasClass;
    
    [SerializeField] HasTestClass[] hasClassArray;
    [SerializeField] int a;
}

public enum TestType
{
    Happy,
    Fun,
    Life,
    Game,
    Patten,
    Testing,
    Devlop,
}

[Serializable]
public struct HasTestClass
{
    [SerializeField] public int aaa;
    [SerializeField] public string AAA;
}

[Serializable]
public struct Testssss
{
    public int[] arr;
    public int t;
}


public class Test : MonoBehaviour
{
    [SerializeField] TextAsset loadCsv;
    [SerializeField] MasterTest[] masterTests;

    [ContextMenu("Master Test")]
    void MasterTest()
    {
        masterTests = CsvUtility.CsvToArray<MasterTest>(loadCsv.text);
        if (masterTests.All(x => x.IsSuccess())) print("GOOD!!");
        else print("Bad!!");
    }

    [Header("Save Test Values")]
    [SerializeField] TextAsset saveCsv;
    [SerializeField] SaveTestCalss[] saveTests;

    [ContextMenu("Save Test")]
    void SaveTest()
    {
        foreach (var item in saveTests)
        {
            item.test3.Clear();
            item.test3.Add("딕셔너리 true입니당", true);
            item.test3.Add("딕셔너리 false입니당", false);
        }

        CsvUtility.SaveCsv(saveTests, "Assets/2_Data/save.csv", 2, 2, 2);
        return;
    }

    [Header("아무거나 테스트")]
    [SerializeField] TextAsset testCsv;
    [SerializeField] Testssss[] Tests;
    [ContextMenu("Test")]
    void TTTTTT()
    {
        Tests = CsvUtility.GetEnumerableFromCsv<Testssss>(testCsv.text).ToArray();
    }
}