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


    [SerializeField] TextAsset basicTypeParseTestData;
    // �̷� ������ csv����
    /*
    first, second
    1, 2
    3, 4
    */
    [ContextMenu("csv �⺻�� parsing �׽�Ʈ")]
    public void TestCsvParseToBasicType()
    {
        Log("csv �⺻�� parsing �׽�Ʈ");
        string testData = basicTypeParseTestData.text;
        var parser = new CsvParser(testData);
        Assert(parser.ValuesByName.Count == 2);
        Assert(parser.GetCell("first") == "1" && parser.GetCell("second") == "2");
        Assert(parser.Moveable);
        parser.MoveNextLine();
        Assert(parser.CurrentIndex == 1);
        Assert(parser.GetCell("first") == "3" && parser.GetCell("second") == "4");
        Assert(parser.Moveable == false);
    }

    [SerializeField] TextAsset IEnumerableTypeParseTestData;
    // �̷� ������ csv����
    /*
    first, second, third, thrid
    "1,23,4", "2,41,2", "1,2,3", 4
    "Hello,World", "You, Shold, Know, Me", h, i
    */
    [ContextMenu("csv ������ parsing �׽�Ʈ")]
    public void TestCsvParseToIEnumerable()
    {
        Log("csv ������ parsing �׽�Ʈ");
        string testData = IEnumerableTypeParseTestData.text;
        var parser = new CsvParser(testData);
        Assert(parser.ValuesByName.Count == 2);
        Assert(parser.GetCell("first") == "1,23,4" && parser.GetCell("second") == "2,41,2");
        Assert(parser.Moveable);
        parser.MoveNextLine();
        Assert(parser.CurrentIndex == 1);
        Assert(parser.GetCell("first") == "Hello,World" && parser.GetCell("second") == "You,Shold,Know,Me");
        Assert(parser.Moveable == false);
    }


    [SerializeField] TextAsset BasicAndIEnumerableTypeParseTestData;
    // �̷� ������ csv����
    /* ������ ���� ���� �Ϸ��� ��������. ������ ����
    first,first,first,      second,second,second,second
    "1,23,4",   ,   ,       "2,41,2",3    ,"44,55",
    "Hello,World",With,Unity,You,Should,Know,Me
    */
    [ContextMenu("csv �⺻��, ������ parsing �׽�Ʈ")]
    public void TestCsvParseToIEnumerableAndBasic()
    {
        Log("csv �⺻��, ������ parsing �׽�Ʈ");
        string testData = BasicAndIEnumerableTypeParseTestData.text;
        var parser = new CsvParser(testData);
        Assert(parser.ValuesByName.Count == 2);
        Assert(parser.GetCell("first") == "1,23,4" && parser.GetCell("second") == "2,41,2,3,44,55");
        Assert(parser.Moveable);
        parser.MoveNextLine();
        Assert(parser.CurrentIndex == 1);
        Assert(parser.GetCell("first") == "Hello,World,With,Unity" && parser.GetCell("second") == "You,Shold,Know,Me");
        Assert(parser.Moveable == false);
    }
}
