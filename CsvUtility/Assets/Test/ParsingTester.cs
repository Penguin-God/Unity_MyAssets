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
        Log("상속 테스트!!");
        _chileDatas = CsvUtility.CsvToArray<ChileData>(_inheritanceData.text);
        Assert(_chileDatas[0].parentNum == 1 && _chileDatas[1].childNum == 2 && _chileDatas[1].parentNum == 3 && _chileDatas[1].parentNum == 4);
    }


    [SerializeField] TextAsset basicTypeParseTestData;
    // 이런 형태의 csv파일
    /*
    first, second
    1, 2
    3, 4
    */
    [ContextMenu("csv 기본형 parsing 테스트")]
    public void TestCsvParseToBasicType()
    {
        Log("csv 기본형 parsing 테스트");
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
    // 이런 형태의 csv파일
    /*
    first, second, third, thrid
    "1,23,4", "2,41,2", "1,2,3", 4
    "Hello,World", "You, Shold, Know, Me", h, i
    */
    [ContextMenu("csv 열거형 parsing 테스트")]
    public void TestCsvParseToIEnumerable()
    {
        Log("csv 열거형 parsing 테스트");
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
    // 이런 형태의 csv파일
    /* 공백은 보기 좋게 하려고 넣은거임. 원래는 없음
    first,first,first,      second,second,second,second
    "1,23,4",   ,   ,       "2,41,2",3    ,"44,55",
    "Hello,World",With,Unity,You,Should,Know,Me
    */
    [ContextMenu("csv 기본형, 열거형 parsing 테스트")]
    public void TestCsvParseToIEnumerableAndBasic()
    {
        Log("csv 기본형, 열거형 parsing 테스트");
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
