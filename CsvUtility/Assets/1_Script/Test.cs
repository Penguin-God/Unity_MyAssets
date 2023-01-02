using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using Debug = UnityEngine.Debug;
using CsvConvertors;

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

    [SerializeField] public HasTestClass hasClass;
    [SerializeField] public HasTestClass[] hasClassArray;
    [SerializeField] TestEnumType testType;

    public bool IsSuccess()
    {
         //&& CheckSame(textNumberPair.Key, "사무라이 하트") && CheckSame(textNumberPair.Value, 243) && CheckSame(booleanActualNumberPair.Key, false)
         //   && CheckSame(booleanActualNumberPair.Value, 22.411f)
        return CheckSame(number, 123) && CheckSame(text, "Hello World") && CheckSame(actualNumber, 23.123f) && CheckSame(boolean, true)
           
            && CheckArraySame(numberArray, new int[] { 341235, 13123 })
            && CheckArraySame(textArray, new string[] { "고타에", "안녕", "라헤야" }) && CheckArraySame(actualNumberArray, new float[] { 4224.12f, 1245.12f })
            && CheckArraySame(booleanArray, new bool[] { true, false }) && CheckListSame(numberList, new List<int>() { 341235, 13123 })
            && CheckListSame(textList, new List<string>() { "고타에", "라헤야" }) && CheckListSame(actualNumberList, new List<float>() { 4224.12f, 1245.12f })
            && CheckListSame(booleanList, new List<bool>() { true, false }) && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(2, "조찬자"))
            && CheckDictionarySame(numberByText, new KeyValuePair<int, string>(12432, "아 루즈 마이셀프"))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(2134.22f, true))
            && CheckDictionarySame(actualNumberByBoolean, new KeyValuePair<float, bool>(11.11f, false))
            && HasClassIsSame(hasClass) && HasClassEnumerableIsSame(hasClassArray)
            && CheckSame(testType, TestEnumType.Devlop);
    }

    // TODO : 틀렸을 때 정보도 LogError에 띄우기
    bool CheckSame<T>(T parsingValue, T value) where T : IComparable
    {
        if (parsingValue == null)
            Debug.Log(value);
        if (parsingValue.CompareTo(value) != 0)
        {
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

    bool HasClassIsSame(HasTestClass testClass) => CheckSame(testClass.number, 777) && CheckSame(testClass.AAA, "안녕하세요.");
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

public enum TestEnumType
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
public class HasTestClass
{
    [SerializeField] public int number;
    [SerializeField] public string AAA;
}

[Serializable]
public class DeSerializeData
{
    [SerializeField] int number;
    [SerializeField] string text;
    [SerializeField] float actualNumber;
    [SerializeField] bool boolean;
    
    [SerializeField] int[] intArray;
    [SerializeField] List<int> numberList;

    public Dictionary<int, string> numberByText = new Dictionary<int, string>()
    {
        {123, "백이십삼" }, {456, "사백오십육"}
    };

    [SerializeField] public HasTestClass hasClass;
    [SerializeField] public HasTestClass[] hasClassArray;
    [SerializeField] TestEnumType testType;
}

[Serializable]
class HasVector3
{
    [SerializeField] Vector3 vector;
}

public class Test : MonoBehaviour
{
    [SerializeField] TextAsset loadCsv;
    [SerializeField] MasterTest[] masterTests;

    [ContextMenu("Master Test")]
    void MasterTest()
    {
        masterTests = CsvUtility.CsvToArray<MasterTest>(loadCsv.text);
        if (masterTests.All(x => x.IsSuccess()) && masterTests.Length == 6) print("GOOD!!");
        else print("Bad!!");
    }


    [Header("Save Test")]
    [TextArea, SerializeField] string saveResult;
    [SerializeField] DeSerializeData[] _deSerializeDatas;

    [ContextMenu("Save Test")]
    void TestDeSerializede()
    {
        if (CsvUtility.ArrayToCsv(_deSerializeDatas) == saveResult) print("GOOD!!");
        else print("Bad!!");
        SaveCsv(CsvUtility.ArrayToCsv(_deSerializeDatas));
    }

    void SaveCsv(string csv)
    {
        Stream fileStream = new FileStream("Assets/2_Data/save.csv", FileMode.Create, FileAccess.Write);
        StreamWriter outStream = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        outStream.Write(csv);
        outStream.Close();
    }

    [SerializeField] HasVector3[] has;
    [ContextMenu("Test")]
    void TestTest()
    {
        // has = CsvUtility.CsvToArray<HasVector3>("vector\n1,2,3\n ");

        var interfaceType = typeof(ICsvConvertor);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => interfaceType.IsAssignableFrom(p) && p.IsInterface == false);
        foreach (var type in types)
        {
            var parser = Activator.CreateInstance(type) as ICsvConvertor;
            type.GetMethod("TextToObject").Invoke(parser, new object[] { "1,2,3", type });
        }
    }
}

class VectorParser : ICsvConvertor
{
    public Type ConverteType => typeof(Vector3);

    public object TextToObject(string text, Type type)
    {
        Debug.Log(text);
        List<float> values = new List<float>() { 0, 0, 0 };
        var texts = text.Split(',');
        for (int i = 0; i < 3; i++)
        {
            if (texts.Length < i + 1 || float.TryParse(texts[i], out float inputValue) == false)
                values[i] = 0;
            else
                values[i] = inputValue;
        }
        return new Vector3(values[0], values[1], values[2]);
    }
}