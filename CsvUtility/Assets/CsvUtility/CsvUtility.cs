using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using CsvConvertors;
using Debug = UnityEngine.Debug;

public static class CsvUtility
{
    class CsvSaveOption
    {
        [SerializeField] int _arrayCount;
        [SerializeField] int _listCount;
        [SerializeField] int _dictionaryCount;

        public int ArrayCount => (_arrayCount > 0) ? _arrayCount : 1;
        public int ListCount => (_listCount > 0) ? _listCount : 1;
        public int DitionaryCount => (_dictionaryCount > 0) ? _dictionaryCount : 1;

        public CsvSaveOption(int arrayCount, int listCount = 1, int dictionaryCount = 1)
        {
            _arrayCount = arrayCount;
            _listCount = listCount;
            _dictionaryCount = dictionaryCount;
        }
    }

    
    public static List<T> CsvToList<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToList();
    public static T[] CsvToArray<T>(string csv) => GetEnumerableFromCsv<T>(csv).ToArray();
    static IEnumerable<T> GetEnumerableFromCsv<T>(string csv) => new CsvSerializer<T>().CsvToIEnumerable(csv);


    public static string ListToCsv<T>(List<T> list, int arrayLength = 1, int listLength = 1, int dictionaryLength = 1)
        => GetSaver<T>(arrayLength, listLength, dictionaryLength).EnumerableToCsv(list);
    public static string ArrayToCsv<T>(T[] array, int arrayLength = 1, int listLength = 1, int dictionaryLength = 1)
        => GetSaver<T>(arrayLength, listLength, dictionaryLength).EnumerableToCsv(array);
    static CsvSaver<T> GetSaver<T>(int arrayLength, int listLength, int dictionaryLength) => new CsvSaver<T>(arrayLength, listLength, dictionaryLength);


    static string SubLastLine(string text) => text.Substring(0, text.Length - 1);
    static IEnumerable<FieldInfo> GetSerializedFields(object obj)
    {
        Type type = obj as Type;
        return type == null ? GetSerializedFields(obj.GetType()) : GetSerializedFields(type);
    }
    static IEnumerable<FieldInfo> GetSerializedFields(Type type)
        => type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
               .Where(x => CsvSerializedCondition(x));

    static bool CsvSerializedCondition(FieldInfo info) => info.IsPublic || info.GetCustomAttribute(typeof(SerializeField)) != null;

    static Type GetElementType(Type type) => type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
    static Type GetCoustomType(Type type) => TypeIdentifier.IsIEnumerable(type) ? GetElementType(type) : type;
    static List<string> GetConcatList(List<string> origin, IEnumerable<string> addValue) => origin.Concat(addValue).ToList();
    static List<string> GetSerializedFieldNames(Type type)
    {
        List<string> restul = new List<string>();
        foreach (FieldInfo info in GetSerializedFields(type))
        {
            restul.Add(info.Name);

            if (TypeIdentifier.IsCustom(info.FieldType))
                restul = GetConcatList(restul, GetSerializedFieldNames(GetCoustomType(info.FieldType)));
        }
        return restul;
    }

    class CsvSerializer<T>
    {
        const char comma = ',';
        const char lineBreak = '\n';
        
        [Conditional("UNITY_EDITOR")]
        void CheckFieldNames(Type type, string[] filedNames)
        {
            List<string> realFieldNames = GetSerializedFieldNames(typeof(T));

            for (int i = 0; i < filedNames.Length; i++)
                Debug.Assert(realFieldNames.Contains(filedNames[i]), $"Unable to find {i + 1}th column name: {filedNames[i]}");
        }


        string _csv;
        string[] fieldNames;
        void Init(string csv)
        {
            _csv = csv.Substring(0, csv.Length - 1);
            fieldNames = GetCells(_csv.Split(lineBreak)[0]);
            _converter = new CustomObjectConverter(fieldNames, CsvLineToObject);
            CheckFieldNames(typeof(T), fieldNames);
        }

        string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();

        public IEnumerable<T> CsvToIEnumerable(string csv)
        {
            Init(csv);
            CsvLineProcecessor lineProcessor = new CsvLineProcecessor();
            return _csv.Split(lineBreak)
                .Skip(1)
                .Where(line => lineProcessor.IsValidLine(line))
                .Select(line => (T)CsvLineToObject(typeof(T), fieldNames, lineProcessor.GetValueList(line)));
        }

        object CsvLineToObject(Type type, string[] fieldNames, List<string> cells)
        {
            object obj = Activator.CreateInstance(type);
            SetObjInfo(type, fieldNames, cells, obj);
            return obj;
        }

        void SetObjInfo(Type type, string[] fieldNames, List<string> cells, object obj)
        {
            Dictionary<string, int> countByFieldName = new FieldNameMaker().GetCountByFieldName(type, fieldNames);

            // 유효한 멤버들 채움
            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                info.SetValue(obj, GetFiledValue(cells, countByFieldName[info.Name], info));
        }

        object GetFiledValue(List<string> cells, int filedCount, FieldInfo info)
            => TypeIdentifier.IsCustom(info.FieldType) ? GetCustomFiledValue(cells, info) : GetFieldValue(cells, filedCount, info.FieldType);

        CustomObjectConverter _converter;
        object GetCustomFiledValue(List<string> cells, FieldInfo info)
        {
            object result = _converter.GetCustomValue(info, cells);
            cells.RemoveAt(0);
            return result;
        }

        object GetFieldValue(List<string> cells, int fieldCount, Type filedType)
        {
            var values = new FiledValueGetter().GetFieldValues(fieldCount, cells).Select(x => x.Trim());

            // 여기서 try문 걸기
            return CsvConverter.TextToObject(string.Join("+", values), filedType);
        }
    }

    class CsvLineProcecessor
    {
        const char mark = '\"';
        const char replaceMark = '+';
        const char comma = ',';

        public List<string> GetValueList(string line)
        {
            string[] tokens = line.Split(mark);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (i % 2 == 1)
                    tokens[i] = tokens[i].Replace(',', replaceMark);
            }
            return GetCells(string.Join("", tokens).Replace("\"", "")).ToList();
        }
        string[] GetCells(string line) => line.Split(comma).Select(x => x.Trim()).ToArray();

        public bool IsValidLine(string line)
        {
            if (GetValueList(line)[0] == "PASS")
                return false;
            return !string.IsNullOrEmpty(line.Replace(",", "").Trim());
        }
    }

    class FieldNameMaker
    {
        // 이걸 팩터리로 바꾸고 interface주고 로직을 거기에 숨겨야 됨
        public Dictionary<string, int> GetCountByFieldName(Type type, string[] fieldNames)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (FieldInfo info in GetSerializedFields(type).Where(x => fieldNames.Contains(x.Name)))
                result.Add(info.Name, GetCount(info));
            return result;

            int GetCount(FieldInfo _info)
            {
                if (TypeIdentifier.IsPrimitive(_info.FieldType))
                    return 1;
                else if (TypeIdentifier.IsCustom(_info.FieldType))
                    return fieldNames.Count(x => x == _info.Name) - 1;
                else if (TypeIdentifier.IsIEnumerable(_info.FieldType))
                    return fieldNames.Count(x => x == _info.Name);
                else if (CsvConverter._customConvertorManager.IsUserCustomConvertor(_info.FieldType)) // 임시
                    return 1;

                Debug.LogError($"Unloadable type : {_info.FieldType}, variable name : {_info.Name}, class type : {type}");
                return 1;
            }
        }
    }

    class FiledValueGetter
    {
        public string[] GetFieldValues(int count, List<string> cells)
        {
            List<string> result = new List<string>();

            for (int i = 0; i < count; i++)
            {
                string value = cells[0];
                if (string.IsNullOrEmpty(value) == false)
                    result.Add(value);
                cells.RemoveAt(0);
            }

            // if (result.Count == 0) result.Add("");
            return result.ToArray();
        }
    }


    class CustomObjectConverter
    {
        string[] _fieldNames;
        Func<Type, string[], List<string>, object> GetInstance;
        FiledValueGetter _filedValueGetter = new FiledValueGetter();
        public CustomObjectConverter(string[] fieldNames, Func<Type, string[], List<string>, object> instanceMaker)
        {
            _fieldNames = fieldNames;
            GetInstance = instanceMaker;
        }

        public object GetCustomValue(FieldInfo _info, List<string> cells)
        {
            if (TypeIdentifier.IsIEnumerable(_info.FieldType))
                return GetCustomArray(_info, cells);
            else
                return GetSingleCustomValue(_info.FieldType, cells, _info.Name);
        }

        object GetSingleCustomValue(Type type, List<string> cells, string name, int index = 0)
        {
            cells.RemoveAt(0);
            if (_filedValueGetter.GetFieldValues(GetSerializedFields(type).Count(), 
                new List<string>(cells)).Where(x => string.IsNullOrEmpty(x) == false).Count() == 0)
            {
                _filedValueGetter.GetFieldValues(GetSerializedFields(type).Count(), cells);
                return null;
            }
            else
                return GetInstance(GetCoustomType(type), GetCustomFieldNames(index), cells);

            string[] GetCustomFieldNames(int startIndex)
            {
                int[] indexs = _fieldNames.Select((value, index) => new { value, index }).Where(x => x.value == name).Select(x => x.index).ToArray();
                return _fieldNames.Skip(indexs[startIndex] + 1).Take(indexs[startIndex + 1] - indexs[startIndex] - 1).ToArray();
            }
        }

        object GetCustomArray(FieldInfo info, List<string> cells)
        {
            int length = _fieldNames.Where(x => x == info.Name).Count() - 1;
            Type elementType = GetElementType(info.FieldType);

            List<object> objs = new List<object>();
            for (int i = 0; i < length; i++)
            {
                var value = GetSingleCustomValue(elementType, cells, info.Name, i);
                if (value != null)
                    objs.Add(value);
            }

            if (objs.Count == 0) return null;

            Array array = Array.CreateInstance(elementType, objs.Count);
            for (int i = 0; i < objs.Count; i++)
                array.SetValue(objs[i], i);

            return GetIEnumerableValue(info.FieldType, array);
        }
        object GetIEnumerableValue(Type type, Array array) => (type.IsArray) ? array : type.GetConstructors()[2].Invoke(new object[] { array });
    }



    class CsvSaver<T>
    {
        CsvSaveOption _option;

        Dictionary<Type, int> _customCountByType;

        Dictionary<Type, int> GetCountByType(IEnumerable<T> datas)
        {
            Dictionary<Type, int> countByType = new Dictionary<Type, int>();
            foreach (T data in datas)
            {
                foreach (FieldInfo info in GetSerializedFields(data).Where(x => TypeIdentifier.IsCustom(x.FieldType) && TypeIdentifier.IsIEnumerable(x.FieldType)))
                {
                    if (countByType.ContainsKey(info.FieldType) == false)
                        countByType.Add(info.FieldType, 1);

                    int count = 0;
                    foreach (var item in info.GetValue(data) as IEnumerable)
                        count++;
                    if (count > countByType[info.FieldType])
                        countByType[info.FieldType] = count;
                }
            }
            return countByType;
        }

        int GetTypeCellCount(Type type)
        {
            int result = 0;
            foreach (FieldInfo info in GetSerializedFields(type))
                result += GetOptionCount(info.FieldType);
            return result;
        }

        public CsvSaver(int arrayLength, int listLength, int dictionaryLength)
        {
            _option = new CsvSaveOption(arrayLength, listLength, dictionaryLength);
            _customCountByType = new Dictionary<Type, int>();
            _customCountByType.Clear();
        }

        int GetOptionCount(Type type)
        {
            if (TypeIdentifier.IsIEnumerable(type) == false) return 1;
            else if (type.IsArray) return _option.ArrayCount;
            else if (TypeIdentifier.IsList(type)) return _option.ListCount;
            else if (TypeIdentifier.IsDictionary(type)) return _option.DitionaryCount;
            else
            {
                Debug.LogWarning("정의할 수 없는 타입");
                return 1;
            }
        }

        public string EnumerableToCsv(IEnumerable<T> datas)
        {
            StringBuilder stringBuilder = new StringBuilder();
            _customCountByType = GetCountByType(datas);
            stringBuilder.AppendLine(string.Join(",", GetFirstRow(typeof(T), _customCountByType)));

            foreach (var data in datas)
            {
                IEnumerable<string> values = GetValues(data);
                stringBuilder.AppendLine(string.Join(",", values));
            }

            return SubLastLine(stringBuilder.ToString());
        }

        List<string> GetFirstRow(object type, Dictionary<Type, int> countByType)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(type))
            {
                if (TypeIdentifier.IsCustom(info.FieldType))
                {
                    result.Add(info.Name);
                    if (TypeIdentifier.IsIEnumerable(info.FieldType))
                        result = GetCustomConcat(result, info, countByType);
                    else
                        result = GetCustomList(result, () => GetFirstRow(info.FieldType, countByType), info.Name);
                }
                else
                {
                    for (int i = 0; i < GetOptionCount(info.FieldType); i++)
                        result.Add(info.Name);
                }
            }
            return result;
        }

        List<string> GetCustomConcat(List<string> result, FieldInfo info, Dictionary<Type, int> countByType)
        {
            for (int i = 0; i < countByType[info.FieldType]; i++)
                result = GetCustomList(result, () => GetFirstRow(GetElementType(info.FieldType), countByType), info.Name);
            return result;
        }


        IEnumerable<string> GetValues(object data)
        {
            List<string> result = new List<string>();
            foreach (FieldInfo info in GetSerializedFields(data.GetType()))
            {
                if (TypeIdentifier.IsPrimitive(info.FieldType))
                    result.Add(info.GetValue(data).ToString());
                else if (TypeIdentifier.IsCustom(info.FieldType))
                    result = GetCustomConcat(data, result, info);
                else if (TypeIdentifier.IsIEnumerable(info.FieldType))
                {
                    result = 
                        GetConcatList(result, GetIEnumerableValue(GetOptionCount(info.FieldType), new IEnumerableConvertor().GetIEnumerableValues(data, info)));
                }
            }
            return result;
        }
        string[] GetIEnumerableValue(int count, string[] values)
        {
            string[] result = new string[count];
            int[] counts = GetCounts(count, values.Length);
            int current = 0;
            for (int i = 0; i < count; i++)
            {
                result[i] = GetValue(values.Skip(current).Take(counts[i]));
                current += counts[i];
            }

            return result;
        }

        int[] GetCounts(int count, int valueLength)
        {
            int length = valueLength;
            int[] counts = new int[count];
            while (length > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    counts[i]++;
                    length--;
                    if (length <= 0) break;
                }
            }

            return counts;
        }

        string GetValue(IEnumerable<string> values)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\"");
            stringBuilder.Append(string.Join(", ", values));
            stringBuilder.Append("\"");
            return stringBuilder.ToString();
        }

        List<string> GetCustomConcat(object data, List<string> result, FieldInfo info)
        {
            result.Add("");
            if (TypeIdentifier.IsIEnumerable(info.FieldType))
            {
                int count = _customCountByType[info.FieldType];
                foreach (var item in info.GetValue(data) as IEnumerable)
                {
                    result = GetCustomList(result, () => GetValues(item));
                    count--;
                }
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < GetTypeCellCount(GetElementType(info.FieldType)) + 1; j++)
                        result.Add("");
                }
            }
            else
                result = GetCustomList(result, () => GetValues(info.GetValue(data)));

            return result;
        }

        List<string> GetCustomList(List<string> result, Func<IEnumerable<string>> OriginFunc, string blank = "")
        {
            result = GetConcatList(result, OriginFunc());
            result.Add(blank);
            return result;
        }
    }
}

public class CsvParser
{
    Dictionary<string, string[]> _valuesByName = new Dictionary<string, string[]>();
    public Dictionary<string, string[]> ValuesByName => _valuesByName;
    int _currentIndex = 0;
    public int CurrentIndex => _currentIndex;
    public bool Moveable => _valuesByName.Count > _currentIndex + 1;
    public void MoveNextLine() => _currentIndex++;
    
    readonly char COMMA = ',';
    readonly char IENUMERABLE_MARK = '\"';
    readonly string REPLACE_MARK = "$%^";

    string GetLine(string line)
    {
        string[] tokens = line.Split(IENUMERABLE_MARK);

        for (int i = 0; i < tokens.Length - 1; i++)
        {
            if (i % 2 == 1)
                tokens[i] = tokens[i].Replace(COMMA.ToString(), REPLACE_MARK);
        }
        return string.Join("", tokens).Replace("\"", "");
    }

    string[] CsvToVaildLines(string csv)
    {
        char lineBreak = '\n';

        List<string> result = new List<string>();
        var allCells = csv.Substring(0, csv.Length - 1).Split(lineBreak).Select(line => line.Split(COMMA).Select(cell => cell.Trim()));
        foreach (var lineCells in allCells)
            result.Add(string.Join(COMMA.ToString(), lineCells));
        return result.ToArray();
    }

    public CsvParser(string csv)
    {
        string[] lines = CsvToVaildLines(csv);
        string[] fieldNames = lines[0].Split(COMMA);

        for (int i = 0; i < fieldNames.Length; i++)
            _valuesByName.Add(fieldNames[i], GetFiledValues(lines, i));
    }

    string[] GetFiledValues(string[] lines, int fieldIndex)
    {
        List<string> values = new List<string>();
        foreach (var currentLine in lines.Skip(1).Select(line => GetLine(line)))
            values.Add(currentLine.Split(COMMA)[fieldIndex].Replace(REPLACE_MARK, COMMA.ToString()));
        return values.ToArray();
    }

    public string GetCell(string fieldName) => _valuesByName[fieldName][_currentIndex];
}
