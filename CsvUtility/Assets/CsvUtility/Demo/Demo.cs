using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    [SerializeField] DemoData[] demoDatas;
    [SerializeField] TextAsset demoCsv;
    [SerializeField] Color color;
    void Start()
    {
        demoDatas = CsvUtility.CsvToArray<DemoData>(demoCsv.text);
    }

    [System.Serializable]
    class DemoData
    {
        [SerializeField] int number;
        [SerializeField] string text;
        [SerializeField] float actualNumber;
        [SerializeField] bool boolean;

        [SerializeField] int[] numberArray;
        [SerializeField] List<string> textList;
        [SerializeField] Dictionary<float, bool> actualNumberByBoolean = new Dictionary<float, bool>();

        [SerializeField] DemoType demoType;
        [SerializeField] DemoVector vector;
        [SerializeField] Color32 color;
    }

    [System.Serializable]
    class DemoVector
    {
        [SerializeField] float x;
        [SerializeField] float y;
        [SerializeField] float z;
    }

    enum DemoType
    {
        AAA,
        BBB,
        CCC,
        DDD,
        EEE,
    }
}
