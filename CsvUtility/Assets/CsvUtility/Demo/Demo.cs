using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

class BattleGameData
{

}

public class Demo : MonoBehaviour
{
    [SerializeField] DemoData[] demoDatas;
    [SerializeField, TextArea] string demoCsvText;

    [SerializeField] TextAsset demoCsv;
    [SerializeField] Color color;

    BattleGameData BattleGameData;
    TextAsset textAsset;
    void Start()
    {
        // Load
        demoDatas = CsvUtility.CsvToArray<DemoData>(demoCsv.text);

        // Save (Run the demo scene and check your folder)
        demoCsvText = CsvUtility.ArrayToCsv(demoDatas, 2, 1, 1);
        string filePath = Application.dataPath + "/CsvUtility/Demo/SaveCsv.csv";
        SaveCsvFile(demoCsvText, filePath);




        BattleGameData = JsonUtility.FromJson<BattleGameData>(textAsset.text);



    }

    void SaveCsvFile(string csv, string filePath)
    {
        Stream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        StreamWriter outStream = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        outStream.Write(csv);
        outStream.Close();
    }

    [System.Serializable]
    class DemoData
    {
        [SerializeField] int number;
        [SerializeField] string text;
        [SerializeField] float actualNumber;
        [SerializeField] bool boolean;

        [SerializeField] int[] numberArray;
        [SerializeField] List<string> textList;
        [SerializeField] Dictionary<float, bool> actualNumberByBoolean = new Dictionary<float, bool>();

        [SerializeField] DemoType demoType;
        [SerializeField] DemoVector vector;
        [SerializeField] Color32 color;
    }

    [System.Serializable]
    class DemoVector
    {
        [SerializeField] float x;
        [SerializeField] float y;
        [SerializeField] float z;
    }

    enum DemoType
    {
        AAA,
        BBB,
        CCC,
        DDD,
        EEE,
    }
}
