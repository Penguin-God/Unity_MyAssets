using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

[Serializable]
public class TestClass
{
    public KeyValuePair<int, string> pair;

    [SerializeField] int key;
    [SerializeField] string value;

    public void Setup()
    {
        key = pair.Key;
        value = pair.Value;
    }
}

public class Test : MonoBehaviour
{
    [SerializeField] TestClass[] testClass;
    [SerializeField] TestClass test;
    [SerializeField] TextAsset asset;

    [ContextMenu("Test")]
    void PairTest()
    {
        testClass = CsvUtility.GetEnumerableFromCsv<TestClass>(asset.text).ToArray();
        testClass.ToList().ForEach(x => x.Setup());
    }
}
