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


    [SerializeField] TextAsset parsingTestData;
    // 이런 형태의 csv파일
    /*
    first, second
    1, 2
    3, 4
    */
    [ContextMenu("csv생성 테스트")]
    public void TestCsvParsin()
    {
        Log("csv생성 테스트");
        string testData = parsingTestData.text;
        var parser = new CsvParser(testData);
        Assert(parser.ValuesByName.Count == 2);
        Assert(parser.GetCell("first") == "1" && parser.GetCell("second") == "2");
        Assert(parser.Moveable);
        parser.MoveNextLine();
        Assert(parser.CurrentIndex == 1);
        Assert(parser.GetCell("first") == "3" && parser.GetCell("second") == "4");
        Assert(parser.Moveable == false);
    }
}
