using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Debug;
using System.Linq;

class ParentData
{
    public int parentNum;
}

class ChileData : ParentData
{
    public int childNum;
}

public class ParsingTester : MonoBehaviour
{
    [SerializeField] TextAsset _inheritanceData;
    [SerializeField] ChileData[] _chileDatas;
    [ContextMenu("Test Inheritance")]
    void TestInheritance()
    {
        Log("��� �׽�Ʈ!!");
        _chileDatas = CsvUtility.CsvToArray<ChileData>(_inheritanceData.text);
        Assert(_chileDatas[0].parentNum == 1 && _chileDatas[1].childNum == 2 && _chileDatas[1].parentNum == 3 && _chileDatas[1].parentNum == 4);
    }

    void TestCsvParse(TextAsset csvFile, string testMessage = "csv parsing Text", params string[] values)
    {
        Log(testMessage);
        string testData = csvFile.text;
        var parser = new CsvParser(testData);
        Assert(parser.ValuesByName.Count == 2);
        Log(parser.GetCell("first"));
        Log(parser.GetCell("second"));
        Assert(parser.GetCell("first") == values[0] && parser.GetCell("second") == values[1]);
        Assert(parser.Moveable);
        parser.MoveNextLine();
        Assert(parser.CurrentIndex == 1);
        Log(parser.GetCell("first"));
        Log(parser.GetCell("second"));
        Assert(parser.GetCell("first") == values[2] && parser.GetCell("second") == values[3]);
        Assert(parser.Moveable == false);
    }

    [SerializeField] TextAsset basicTypeParseTestData;
    // �̷� ������ csv����
    /*
    first, second
    1, 2
    3, 4
    */
    [ContextMenu("csv �⺻�� parsing �׽�Ʈ")]
    void TestCsvParseToBasicType() => TestCsvParse(basicTypeParseTestData, "csv �⺻�� parsing �׽�Ʈ", "1", "2", "3", "4");


    [SerializeField] TextAsset IEnumerableTypeParseTestData;
    // �̷� ������ csv����
    /*
    first, second, third, thrid
    "1,23,4", "2,41,2", "1,2,3", 4
    "Hello,World", "You, Shold, Know, Me", h, i
    */
    [ContextMenu("csv ������ parsing �׽�Ʈ")]
    void TestCsvParseToIEnumerable() => TestCsvParse(IEnumerableTypeParseTestData, "csv ������ parsing �׽�Ʈ"
        , "1,23,4", "2,41,2", "Hello,World", "You,Shold,Know,Me");


    [SerializeField] TextAsset BasicAndIEnumerableTypeParseTestData;
    // �̷� ������ csv����
    /* ������ ���� ���� �Ϸ��� ��������. ������ ����
    first,first,first,      second,second,second,second
    "1,23,4",   ,   ,       "2,41,2",3    ,"44,55",
    "Hello,World",With,Unity,You,Should,Know,Me
    */
    [ContextMenu("csv �⺻��, ������ parsing �׽�Ʈ")]
    // TODO : ���߿� third���� �߰��ؼ� �⺻���̶� ������ ���� �׽�Ʈ�ϱ�
    void TestCsvParseToIEnumerableAndBasic() => TestCsvParse(BasicAndIEnumerableTypeParseTestData, "csv �⺻��, ������ parsing �׽�Ʈ"
        , "1,23,4", "2,41,2,3,44,55", "Hello,World,With,Unity", "You,Shold,Know,Me");
}
