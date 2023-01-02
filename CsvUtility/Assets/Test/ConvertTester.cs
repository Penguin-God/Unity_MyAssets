using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvConvertors;
using static UnityEngine.Debug;
using System.Linq;

public class ConvertTester : MonoBehaviour
{
    [ContextMenu("Test All")]
    void TestAll()
    {
        TestPrimitiveConvertor();
        TestIEnumerableConvertor();
    }

    [ContextMenu("Test Primitive Convertor")]
    void TestPrimitiveConvertor()
    {
        Log("기본형 컨버터 테스트!!");
        var convertor = new PrimitiveConvertor();
        Assert((byte)convertor.TextToObject("25", typeof(byte)) == 25);
        Assert((int)convertor.TextToObject("25", typeof(int)) == 25);
        Assert((long)convertor.TextToObject("7223372036854775807", typeof(long)) == 7223372036854775807);
        Assert((float)convertor.TextToObject("1.52", typeof(float)) == 1.52f);
        Assert((bool)convertor.TextToObject("True", typeof(bool)) == true);
        Assert((bool)convertor.TextToObject("False", typeof(bool)) == false);
    }

    [ContextMenu("Test Enum Convertor")]
    void EnumTest()
    {
        Log("Enum 변환 테스트!!");
        var convertor = new EnumConvertor();
        Assert( (TestEnumType)convertor.TextToObject("Happy", typeof(TestEnumType)) == TestEnumType.Happy);
        Assert((TestEnumType)convertor.TextToObject("Patten", typeof(TestEnumType)) == TestEnumType.Patten);
    }

    
    [ContextMenu("Test IEnumerables Convertor")]
    void TestIEnumerableConvertor()
    {
        TestArrayConvert();
        TestListConvert();
        TestDictionaryConvert();
    }

    void TestArrayConvert()
    {
        Log("배열 변환 테스트!!");

        Assert((new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) as int[]).Except(new int[] { 10, 20, 30 }).Count() == 0);
        Assert((new ArrayConvertor().TextToObject("내,이름은,박준", typeof(string[])) as string[]).Except(new string[] { "내", "이름은", "박준" }).Count() == 0);
    }

    void TestListConvert()
    {
        Log("리스트 변환 테스트!!");
        Assert((new ListConvertor().TextToObject("10,20,30", typeof(List<int>)) as List<int>).Except(new List<int> { 10, 20, 30 }).Count() == 0);
        Assert((new ListConvertor().TextToObject("내,이름은,박준", typeof(List<string>)) as List<string>).Except(new List<string> { "내", "이름은", "박준" }).Count() == 0);
    }

    void TestDictionaryConvert()
    {
        Log("딕셔너리 변환 테스트!!");
        Assert((new DictionaryConvertor().TextToObject("안녕,True,잘 가,False", typeof(Dictionary<string, bool>)) as Dictionary<string, bool>)
            .Except(new Dictionary<string, bool> { { "안녕", true }, { "잘 가", false } }).Count() == 0);
    }
}
