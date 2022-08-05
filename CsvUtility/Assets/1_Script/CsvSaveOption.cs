using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CsvSaveOption
{
    [SerializeField] int _arrayCount;
    [SerializeField] int _listCount;
    [SerializeField] int _dictionaryCount;

    public int ArrayCount => (_arrayCount > 0) ? _arrayCount : 1;
    public int ListCount => (_listCount > 0) ? _listCount : 1;
    public int DitionaryCount => (_dictionaryCount > 0) ? _dictionaryCount : 1;

    public CsvSaveOption()
    {
        _arrayCount = 1;
        _listCount = 1;
        _dictionaryCount = 1;
    }

    public CsvSaveOption(int arrayCount, int listCount = 1, int dictionaryCount = 1)
    {
        _arrayCount = arrayCount;
        _listCount = listCount;
        _dictionaryCount = dictionaryCount;
    }
}
