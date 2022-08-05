using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    [SerializeField] ConstData data;
    [SerializeField] TextAsset text;
    [SerializeField] TextAsset testText;

    [ContextMenu("Load")]
    void Load()
    {
        data = CSVSerializer.DeserializeIdValue<ConstData>(text.text);
    }

    [SerializeField] string[] result;

    [ContextMenu("NewTest")]
    void NewTest()
    {
        string line = testText.text.Split('\n')[0];
        string[] tokens = line.Split('\"');
        
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            if (i % 2 == 1)
            {
                tokens[i] = tokens[i].Replace(',', '.');
            }
        }
        result = tokens;
    }
}
