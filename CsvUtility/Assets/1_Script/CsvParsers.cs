using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;

enum EnumerableType
{
    Unknown,
    Array,
    List,
    Dictionary,
}

public interface CsvPrimitiveTypeParser
{
    object GetParserValue(string value);

    IEnumerable GetParserEnumerable(string[] values);
    Type GetParserType();
}

public interface CsvParser
{
    void SetValue(object obj, FieldInfo info, string[] values);
}

public abstract class CsvParsers
{
    public static CsvParser GetParser(FieldInfo info)
    {
        if (IsEnumerable(info.FieldType.Name))
            return new EnumerableTypeParser(info);
        else if (IsPair(info.FieldType.Name))
            return new CsvPairParser(info);
        else
            return new PrimitiveTypeParser();

        bool IsEnumerable(string typeName) => typeName.Contains("[]") || typeName.Contains("List");
        bool IsPair(string typeName) => typeName == "KeyValuePair`2";
    }
}


class PrimitiveTypeParser : CsvParser
{
    public static CsvPrimitiveTypeParser GetPrimitiveParser(string typeName)
    {
        typeName = typeName.Replace("System.", "");
        switch (typeName)
        {
            case nameof(Int32): return new CsvIntParser();
            case nameof(Single): return new CsvFloatParser();
            case nameof(Boolean): return new CsvBooleanParser();
            case nameof(String): return new CsvStringParser();
            default: Debug.LogError("Csv 파싱 타입을 찾지 못함"); break;
        }
        return null;
    }

    object GetParserValue(string typeName, string value) => GetPrimitiveParser(typeName).GetParserValue(value);

    public void SetValue(object obj, FieldInfo info, string[] value)
        => info.SetValue(obj, GetParserValue(info.FieldType.Name, value[0]));
}

class EnumerableTypeParser : CsvParser
{
    string _typeName;
    string _elementTypeName;
    EnumerableType _type;
    public EnumerableTypeParser(FieldInfo info)
    {
        _typeName = info.FieldType.Name;
        _elementTypeName = GetElementTypeName().Replace("System.", "");
        _type = GetEnumableType(_typeName);

        string GetElementTypeName()
        {
            if (info.FieldType.Name.Contains("[]"))
                return info.FieldType.GetElementType().Name;
            else if (info.FieldType.Name.Contains("List"))
                return info.FieldType.ToString().GetMiddleString("[", "]");

            return "";
        }

        EnumerableType GetEnumableType(string typeName)
        {
            if (typeName.Contains("[]")) return EnumerableType.Array;
            else if (typeName.Contains("List")) return EnumerableType.List;
            return EnumerableType.Unknown;
        }
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (_type == EnumerableType.Array)
            new CsvArrayParser().SetValue(obj, info, values, _elementTypeName);
        else if (_type == EnumerableType.List)
            new CsvListParser().SetValue(obj, info, values, _elementTypeName);
    }
}

#region 기본형 파싱

class CsvIntParser : CsvPrimitiveTypeParser
{
    public object GetParserValue(string value)
    {
        Int32.TryParse(value, out int valueInt);
        return valueInt;
    }

    public IEnumerable GetParserEnumerable(string[] value) => value.Select(x => (int)GetParserValue(x));

    public Type GetParserType() => typeof(int);
}

class CsvFloatParser : CsvPrimitiveTypeParser
{
    public object GetParserValue(string value)
    {
        float.TryParse(value, out float valueFloat);
        return valueFloat;
    }

    public IEnumerable GetParserEnumerable(string[] value) => value.Select(x => (float)GetParserValue(x));
    public Type GetParserType() => typeof(float);
}

class CsvStringParser : CsvPrimitiveTypeParser
{
    public object GetParserValue(string value) => value;
    public IEnumerable GetParserEnumerable(string[] value) => value.AsEnumerable();
    public Type GetParserType() => typeof(string);
}

class CsvBooleanParser : CsvPrimitiveTypeParser
{
    public object GetParserValue(string value) => value == "True" || value == "TRUE";
    public IEnumerable GetParserEnumerable(string[] value) => value.Select(x => (bool)GetParserValue(x));
    public Type GetParserType() => typeof(bool);
}

#endregion 기본형 파싱 End


#region 열거형 파싱
public class CsvListParser
{
    public void SetValue(object obj, FieldInfo info, string[] values, string typeName)
    {
        ConstructorInfo constructor = info.FieldType.GetConstructors()[2];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(typeName).GetParserEnumerable(values) }));
    }
}

public class CsvArrayParser
{
    public void SetValue(object obj, FieldInfo info, string[] values, string typeName)
    {
        Array array = Array.CreateInstance(PrimitiveTypeParser.GetPrimitiveParser(typeName).GetParserType(), values.Length);
        for (int i = 0; i < array.Length; i++)
            array.SetValue(PrimitiveTypeParser.GetPrimitiveParser(typeName).GetParserValue(values[i]), i);
        info.SetValue(obj, array);
    }
}
#endregion 열거형 파싱 End

public class CsvPairParser : CsvParser
{
    string keyTypeName;
    string valueTypeName;
    public CsvPairParser(FieldInfo info)
    {
        string[] elementTypeNames = info.FieldType.ToString().GetMiddleString("[", "]").Split(',');
        keyTypeName = elementTypeNames[0].Replace("System.", "");
        valueTypeName = elementTypeNames[1].Replace("System.", "");
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        ConstructorInfo constructor = info.FieldType.GetConstructors()[0];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(keyTypeName).GetParserValue(values[0]),
                                                             PrimitiveTypeParser.GetPrimitiveParser(valueTypeName).GetParserValue(values[1]) } ));
    }
}


public static class Extension
{
    public static string GetMiddleString(this string str, string begin, string end)
    {
        if (string.IsNullOrEmpty(str)) return null;

        string result = null;
        if (str.IndexOf(begin) > -1)
        {
            str = str.Substring(str.IndexOf(begin) + begin.Length);
            if (str.IndexOf(end) > -1) result = str.Substring(0, str.IndexOf(end));
            else result = str;
        }
        return result;
    }
}