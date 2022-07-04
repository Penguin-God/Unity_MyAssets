using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

[Serializable]
public class TestClass
{
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

public class Test : MonoBehaviour
{
    [SerializeField] TestClass[] testClass;
    [SerializeField] TestClass test;
    [SerializeField] TextAsset asset;

    Dictionary<int, string> a = new Dictionary<int, string>();
    [ContextMenu("Test")]
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
