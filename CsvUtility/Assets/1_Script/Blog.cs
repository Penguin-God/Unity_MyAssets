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
class HasClass
{
    [SerializeField] int number;
    [SerializeField] string text;
}

[Serializable]
class BlogTest
{
    [SerializeField] Vector3 vector;
    [SerializeField] HasClass hasClass;
    [SerializeField] Color color;
}

public class Blog : MonoBehaviour
{
    [SerializeField] BlogTest[] blogTests;
    [SerializeField] TextAsset csvAsset;

    [ContextMenu("Do Test")]
    void Test() => blogTests = CsvUtility.CsvToArray<BlogTest>(csvAsset.text);
}
