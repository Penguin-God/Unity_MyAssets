using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
class BlogTest
{
    [SerializeField] int[] intArr;
    [SerializeField] List<string> stringList;
    public Dictionary<float, bool> booleanByFloat = new Dictionary<float, bool>();
    [SerializeField] Games[] gamesArr;
}

public class Blog : MonoBehaviour
{
    [SerializeField] BlogTest[] blogTests;
    [SerializeField] TextAsset csvAsset;

    [ContextMenu("Do Test")]
    void Test()
    {
        blogTests = CsvUtility.CsvToArray<BlogTest>(csvAsset.text);
        foreach (var item in blogTests)
        {
            foreach (var pair in item.booleanByFloat)
            {
                print($"{pair.Key}, {pair.Value}");
            }
        }
    }
}
