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
    // 이런 형태의 csv파일
    /*
    first, second
    1, 2
    3, 4
    */
    [ContextMenu("csv 기본형 parsing 테스트")]
    void TestCsvParseToBasicType() => TestCsvParse(basicTypeParseTestData, "csv 기본형 parsing 테스트", "1", "2", "3", "4");


    [SerializeField] TextAsset IEnumerableTypeParseTestData;
    // 이런 형태의 csv파일
    /*
    first, second, third, thrid
    "1,23,4", "2,41,2", "1,2,3", 4
    "Hello,World", "You, Shold, Know, Me", h, i
    */
    [ContextMenu("csv 열거형 parsing 테스트")]
    void TestCsvParseToIEnumerable() => TestCsvParse(IEnumerableTypeParseTestData, "csv 열거형 parsing 테스트"
        , "1,23,4", "2,41,2", "Hello,World", "You,Shold,Know,Me");


    [SerializeField] TextAsset BasicAndIEnumerableTypeParseTestData;
    // 이런 형태의 csv파일
    /* 공백은 보기 좋게 하려고 넣은거임. 원래는 없음
    first,first,first,      second,second,second,second
    "1,23,4",   ,   ,       "2,41,2",3    ,"44,55",
    "Hello,World",With,Unity,You,Should,Know,Me
    */
    [ContextMenu("csv 기본형, 열거형 parsing 테스트")]
    // TODO : 나중에 third까지 추가해서 기본형이랑 열거형 같이 테스트하기
    void TestCsvParseToIEnumerableAndBasic() => TestCsvParse(BasicAndIEnumerableTypeParseTestData, "csv 기본형, 열거형 parsing 테스트"
        , "1,23,4", "2,41,2,3,44,55", "Hello,World,With,Unity", "You,Shold,Know,Me");
}
