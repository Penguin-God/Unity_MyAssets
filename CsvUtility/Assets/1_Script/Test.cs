using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

[Serializable]
public class TestClass
{
    [SerializeField] 
    public Dictionary<int, string> dict = new Dictionary<int, string>();

    [SerializeField] List<int> keys = new List<int>();
    [SerializeField] List<string> values = new List<string>();

    public void Setup()
    {
        foreach (var item in dict)
        {
            keys.Add(item.Key);
            values.Add(item.Value);
        }
    }
}

[Serializable]
public class MasterTest
{
    int number;
    string text;
    float actualNumber;
    bool boolean;
    KeyValuePair<string, int> textNumberPair;
    KeyValuePair<bool, float> booleanActualNumberPair;

    int[] numberArray;
    string[] textArray;
    float[] actualNumberArray;
    bool[] booleanArray;

    List<int> numberList;
    List<string> textList;
    List<float> actualNumberList;
    List<bool> booleanList;

    Dictionary<int, string> numberByText = new Dictionary<int, string>();
    Dictionary<float, bool> actualNumberByBoolean = new Dictionary<float, bool>();

    public bool IsSuccess()
    {
        return CheckSame(number, 123) || CheckSame(text, "Hello World") || CheckSame(actualNumber, 23.123f) || CheckSame(boolean, true);
    }


    // TODO : 틀렸을 때 정보도 LogError에 띄우기
    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue.CompareTo(value) != 0)
        {
            Debug.LogError("서로 달라요!!!!!!!!!");
            return false;
        }

        return true;
    }
}

public class Test : MonoBehaviour
{
    [SerializeField] TestClass[] testClass;
    [SerializeField] TestClass test;
    [SerializeField] TextAsset asset;

    [ContextMenu("Master Test")]
    void MasterTest()
    {

    }

    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue.CompareTo(value) != 0)
        {
            Debug.LogError("서로 달라요!!!!!!!!!");
            return false;
        }

        return true;
    }


    //[ContextMenu("Test")]
    void PairTest()
    {
        testClass = CsvUtility.GetEnumerableFromCsv<TestClass>(asset.text).ToArray();
        print(testClass[0].dict[33]);
        foreach (var item in testClass)
        {
            item.Setup();
        }
    }
}
