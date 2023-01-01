using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvConvertors;
using static UnityEngine.Debug;

public class ConvertTester : MonoBehaviour
{
    [ContextMenu("Test Convertor Factory")]
    void TestConvertorFactory()
    {
        Log("컨버터 팩터리 테스트!!");
        Assert(new ConvertorFactory().GetCsvConvertor(typeof(int)) is PrimitiveConvertor);
        //Assert(new ConvertorFactory().GetCsvConvertor(typeof(List<>)) is ListConvertor);
    }

    [ContextMenu("Test Primitive Convertor")]
    void TestPrimitiveConvertor()
    {
        Log("기본형 컨버터 테스트!!");
        ICsvConvertor convertor = new PrimitiveConvertor();
        Assert((byte)convertor.TextToObject("25", typeof(byte)) == 25);
        Assert((int)convertor.TextToObject("25", typeof(int)) == 25);
        Assert((long)convertor.TextToObject("7223372036854775807", typeof(long)) == 7223372036854775807);
        Assert((float)convertor.TextToObject("1.52", typeof(float)) == 1.52f);
        Assert((bool)convertor.TextToObject("True", typeof(bool)) == true);
    }

    [ContextMenu("Test Enum Convertor")]
    void EnumTest()
    {

    }

    
    [ContextMenu("Test IEnumerables Convertor")]
    void TestIEnumerableConvertor()
    {
        TestArrayConvert();
    }


    void TestArrayConvert()
    {
        Log("배열 변환 테스트!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new ArrayConvertor().TextToObject("내,이름은,박준", typeof(int[])) == new string[] { "내", "이름은", "박준" });
    }

    void TestListConvert()
    {
        Log("리스트 변환 테스트!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new DictionaryConvertor().TextToObject("안녕, True", typeof(Dictionary<string, bool>)) == new Dictionary<string, bool> { { "안녕", true } });
    }

    void TestDictionaryConvert()
    {
        Log("딕셔너리 변환 테스트!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new DictionaryConvertor().TextToObject("안녕, True", typeof(Dictionary<string, bool>)) == new Dictionary<string, bool> { { "안녕", true } });
    }
}
