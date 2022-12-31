using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    [SerializeField] ConstData data;
    [SerializeField] TextAsset text;
    
    [ContextMenu("Load")]
    void Load()
    {
        data = CSVSerializer.DeserializeIdValue<ConstData>(text.text);
    }
}
