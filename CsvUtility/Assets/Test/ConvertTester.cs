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
        Log("������ ���͸� �׽�Ʈ!!");
        Assert(new ConvertorFactory().GetCsvConvertor(typeof(int)) is PrimitiveConvertor);
        //Assert(new ConvertorFactory().GetCsvConvertor(typeof(List<>)) is ListConvertor);
    }

    [ContextMenu("Test Primitive Convertor")]
    void TestPrimitiveConvertor()
    {
        Log("�⺻�� ������ �׽�Ʈ!!");
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
        Log("�迭 ��ȯ �׽�Ʈ!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new ArrayConvertor().TextToObject("��,�̸���,����", typeof(int[])) == new string[] { "��", "�̸���", "����" });
    }

    void TestListConvert()
    {
        Log("����Ʈ ��ȯ �׽�Ʈ!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new DictionaryConvertor().TextToObject("�ȳ�, True", typeof(Dictionary<string, bool>)) == new Dictionary<string, bool> { { "�ȳ�", true } });
    }

    void TestDictionaryConvert()
    {
        Log("��ųʸ� ��ȯ �׽�Ʈ!!");
        Assert(new ArrayConvertor().TextToObject("10,20,30", typeof(int[])) == new int[] { 10, 20, 30 });
        Assert(new DictionaryConvertor().TextToObject("�ȳ�, True", typeof(Dictionary<string, bool>)) == new Dictionary<string, bool> { { "�ȳ�", true } });
    }
}
