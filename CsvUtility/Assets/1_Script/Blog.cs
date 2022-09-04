using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public enum Games
{
    Ori_and_the_Will_of_the_Wisps,
    OMORI,
    OneShot,
    Katana_ZERO,
    Danganronpa,
    VA_11_Hall_A_Cyberpunk_Bartender_Action,
}

[Serializable]
class HasClass
{
    [SerializeField] int number;
    [SerializeField] string text;
}

[Serializable]
class BlogTest
{
    [SerializeField] string text;
    [SerializeField] int[] numbers; 
    public Dictionary<Games, float> MetacriticScoreByGame = new Dictionary<Games, float>();
    [SerializeField] Vector3 vector;
}

public class Blog : MonoBehaviour
{
    [SerializeField] BlogTest[] blogTests;
    [SerializeField] TextAsset csvAsset;

    string filePath => Path.Combine(Application.dataPath, "saveTest.csv");

    [ContextMenu("Do Test")]
    void Test()
    {
        blogTests[0].MetacriticScoreByGame.Add(Games.Ori_and_the_Will_of_the_Wisps, 8.9f);
        blogTests[0].MetacriticScoreByGame.Add(Games.OMORI, 9.2f);
        blogTests[1].MetacriticScoreByGame.Add(Games.OneShot, 8.9f);
        blogTests[1].MetacriticScoreByGame.Add(Games.Katana_ZERO, 8.9f);
        blogTests[2].MetacriticScoreByGame.Add(Games.Danganronpa, 8.7f); // 단간론파2 기준 점수입니다.
        blogTests[2].MetacriticScoreByGame.Add(Games.VA_11_Hall_A_Cyberpunk_Bartender_Action, 8.3f);

        string csv = CsvUtility.ArrayToCsv(blogTests, 2, 1, 2);

        Stream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        StreamWriter outStream = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        outStream.Write(csv);
        outStream.Close();
    }
}
