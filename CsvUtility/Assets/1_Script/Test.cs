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
    [SerializeField] HasTestClass[] hasTestClass;
    [SerializeField] string kkkk;
    [SerializeField] string sktt1;
}

[Serializable]
public class HasTestClass
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
            && HasClassIsSame();
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

    bool HasClassIsSame() => 777 == hasClass.aaa && "안녕하세요." == hasClass.AAA;
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

    [SerializeField] TestClass[] testClassArr;
    List<TestClass> testClassList = new List<TestClass>();
    [SerializeField] TestClass TestClass;

    [SerializeField] TextAsset testCsv;
    [ContextMenu("Test")]

    void Testss()
    {
        // 커스텀 클래스 배열 파싱할 때는 관련 값들을 긁어오고 csv를 새로 만들어서 파싱할거임
        __InstanceIEnumerableGenerator<TestClass> test = new __InstanceIEnumerableGenerator<TestClass>(testCsv.text);

    }
}


class __InstanceIEnumerableGenerator<T>
{
    const char comma = ',';
    const char lineBreak = '\n';
    const char arraw = '>';

    string _csv;
    string[] fieldNames;
    Dictionary<string, int[]> indexsByKey = new Dictionary<string, int[]>();

    string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();
    string[] GetCells(string line, int start, int end) => line.Split(comma).Select(x => x.Trim()).ToList().GetRange(start, end).ToArray();

    public __InstanceIEnumerableGenerator(string csv)
    {
        _csv = csv.Substring(0, csv.Length - 1); ;
        fieldNames = GetCells(_csv.Split(lineBreak)[0]);

        indexsByKey.Clear();
        SetIndexsByKey(typeof(T));
        foreach (var item in indexsByKey)
        {
            Debug.Log($"{item.Key}");
            foreach (var item2 in item.Value)
            {
                Debug.Log(item2);
            }
        }

        int SetIndexsByKey(Type type, string currentKey = "", int currentIndex = 0)
        {
            foreach (FieldInfo info in CsvUtility.GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
            {
                if (InfoIsCustomClass(info))
                {
                    currentIndex++;
                    currentIndex = SetIndexsByKey(info.FieldType, $"{currentKey}{info.Name}{arraw}", currentIndex);
                    currentIndex++;
                }
                else
                {
                    indexsByKey.Add(currentKey + info.Name, GetIndexs());
                    currentIndex++;
                }
            }
            return currentIndex;

            int[] GetIndexs()
            {
                List<int> indexs = new List<int>();
                while (true)
                {
                    indexs.Add(currentIndex);

                    if(string.IsNullOrEmpty(currentKey) == false)
                    {
                        string currentClass = currentKey.Split(arraw)[currentKey.Split(arraw).Length - 1];
                        if (fieldNames.Where(x => x == currentClass).Count() - 1 > indexs.Where(x => x == -1).Count())
                        {
                            indexs.Add(-1);
                            currentIndex++;
                            continue;
                        }
                    }
                    if (currentIndex + 1 >= fieldNames.Length || fieldNames[currentIndex] != fieldNames[currentIndex + 1]) break;
                    currentIndex++;
                }
                return indexs.ToArray();
            }
        }
    }

    public IEnumerable<T> GetInstanceIEnumerable() => _csv.Split(lineBreak).Skip(1).Select(x => (T)GetInstance(typeof(T), GetCells(x)));

    object GetInstance(Type type, string[] cells, string current = "")
    {
        object obj = Activator.CreateInstance(type);

        foreach (FieldInfo info in CsvUtility.GetSerializedFields(type))
        {
            if (InfoIsCustomClass(info))
                info.SetValue(obj, GetInstance(info.FieldType, cells, $"{current}{info.Name}{arraw}"));
            else
                CsvParsers.GetParser(info).SetValue(obj, info, GetFieldValues(current + info.Name, cells));
        }

        return obj;
    }

    string[] GetFieldValues(string key, string[] cells) => indexsByKey[key].Select(x => cells[x]).ToArray();

    bool InfoIsCustomClass(FieldInfo info)
    {
        string identifier = "System.";
        if (info.FieldType.ToString().StartsWith(identifier)) return false;
        // if (IsEnumerable(info.FieldType.Name) || IsPair(info.FieldType.Name)) return false;
        return true;

        bool IsEnumerable(string typeName) => typeName.Contains("[]") || typeName.Contains("List") || typeName.Contains("Dict");
        bool IsPair(string typeName) => typeName == "KeyValuePair`2";
    }
}