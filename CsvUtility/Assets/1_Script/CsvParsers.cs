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
        if (IsEnumerable(info.FieldType))
            return new EnumerableTypeParser(info);
        else if (IsPair(info.FieldType.Name))
            return new CsvPairParser(info);
        else
            return new PrimitiveTypeParser();

        bool IsEnumerable(Type type) 
            => type.IsArray || 
            ( type.IsGenericType && ( type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ) );
        // bool IsEnumerable(string typeName) => typeName.Contains("[]") || typeName.Contains("List") || typeName.Contains("Dict");
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

    public static CsvPrimitiveTypeParser GetPrimitiveParser(Type type)
    {
        if(type == typeof(int)) return new CsvIntParser();
        else if(type == typeof(string)) return new CsvStringParser();
        else if (type == typeof(float)) return new CsvFloatParser();
        else if (type == typeof(bool)) return new CsvBooleanParser();
        else Debug.LogError("Csv 파싱 타입을 찾지 못함");
        return null;
    }

    object GetParserValue(string typeName, string value) => GetPrimitiveParser(typeName).GetParserValue(value);
    object GetParserValue(Type type, string value) => GetPrimitiveParser(type).GetParserValue(value);

    public void SetValue(object obj, FieldInfo info, string[] value) => info.SetValue(obj, GetParserValue(info.FieldType, value[0]));
}

class EnumerableTypeParser : CsvParser
{
    string _typeName;
    string _elementTypeName;
    EnumerableType _type;
    public EnumerableTypeParser(FieldInfo info)
    {
        _typeName = info.FieldType.Name;
        _type = GetEnumableType(info.FieldType);
        // _elementTypeName = GetElementTypeName().Replace("System.", "");
        // _elementTypeName = GetElementTypeName(info.FieldType).ToString().Replace("System.", "");

        //string GetElementTypeName(Type type)
        //{
        //    if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
        //        return (type.IsArray) ? type.GetElementType().ToString() : type.GetGenericArguments()[0].ToString();
        //    else
        //        return "";
        //}

        //string GetElementTypeName()
        //{
        //    if (info.FieldType.Name.Contains("[]"))
        //        return info.FieldType.GetElementType().Name;
        //    else if (info.FieldType.Name.Contains("List"))
        //        return info.FieldType.ToString().GetMiddleString("[", "]");

        //    return "";
        //}

        EnumerableType GetEnumableType(Type type)
        {
            if (type.IsArray) return EnumerableType.Array;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return EnumerableType.List;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return EnumerableType.Dictionary;
            return EnumerableType.Unknown;
        }


        //EnumerableType GetEnumableType(string typeName, int a)
        //{
        //    if (typeName.Contains("[]")) return EnumerableType.Array;
        //    else if (typeName.Contains("List")) return EnumerableType.List;
        //    else if (typeName.Contains("Dict")) return EnumerableType.Dictionary;
        //    return EnumerableType.Unknown;
        //}
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (_type == EnumerableType.Array)
            new CsvArrayParser().SetValue(obj, info, values, _elementTypeName);
        else if (_type == EnumerableType.List)
            new CsvListParser().SetValue(obj, info, values, _elementTypeName);
        else if (_type == EnumerableType.Dictionary)
            new CsvDictionaryParser(info).SetValue(obj, info, values);
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
        Type elementType = info.FieldType.GetGenericArguments()[0];
        ConstructorInfo constructor = info.FieldType.GetConstructors()[2];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserEnumerable(values) }));
    }
}

public class CsvArrayParser
{
    public void SetValue(object obj, FieldInfo info, string[] values, string typeName)
    {
        Type elementType = info.FieldType.GetElementType();
        Array array = Array.CreateInstance(PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserType(), values.Length);
        for (int i = 0; i < array.Length; i++)
            array.SetValue(PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserValue(values[i]), i);
        info.SetValue(obj, array);
    }
}

public class CsvDictionaryParser
{
    //string keyTypeName;
    //string valueTypeName;
    //Type keyType;
    //Type valueType;
    public CsvDictionaryParser(FieldInfo info)
    {
        //string[] elementTypeNames = info.FieldType.ToString().GetMiddleString("[", "]").Split(',');
        //keyTypeName = elementTypeNames[0].Replace("System.", "");
        //valueTypeName = elementTypeNames[1].Replace("System.", "");

        //Type[] elementTypes = info.FieldType.GetGenericArguments();
        //keyType = elementTypes[0];
        //valueType = elementTypes[1];
        //keyTypeName = elementTypes[0].ToString().Replace("System.", "");
        //valueTypeName = elementTypes[1].ToString().Replace("System.", "");
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        if (values.Length % 2 != 0) Debug.LogError($"{info.Name} 입력이 올바르지 않습니다. Key Value 쌍을 정확히 입력했는지 확인해주세요");

        Type[] elementTypes = info.FieldType.GetGenericArguments();
        MethodInfo methodInfo = info.FieldType.GetMethod("Add");
        for (int i = 0; i < values.Length; i+=2)
        {
            methodInfo.Invoke(info.GetValue(obj), new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementTypes[0]).GetParserValue(values[i]),
                                                                 PrimitiveTypeParser.GetPrimitiveParser(elementTypes[1]).GetParserValue(values[i+1]) });
        }
    }
}
#endregion 열거형 파싱 End

public class CsvPairParser : CsvParser
{
    string keyTypeName;
    string valueTypeName;
    //Type[] elementTypes;
    public CsvPairParser(FieldInfo info)
    {
        //string[] elementTypeNames = info.FieldType.ToString().GetMiddleString("[", "]").Split(',');
        //keyTypeName = elementTypeNames[0].Replace("System.", "");
        //valueTypeName = elementTypeNames[1].Replace("System.", "");

        //elementTypes = info.FieldType.GetGenericArguments();
        //keyTypeName = elementTypes[0].ToString().Replace("System.", "");
        //valueTypeName = elementTypes[1].ToString().Replace("System.", "");
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        if (values.Length != 2) Debug.LogError($"{info.Name} 입력이 올바르지 않습니다. Key Value 쌍을 정확히 입력했는지 확인해주세요");
        Type[] elementTypes = info.FieldType.GetGenericArguments();
        ConstructorInfo constructor = info.FieldType.GetConstructors()[0];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementTypes[0]).GetParserValue(values[0]),
                                                             PrimitiveTypeParser.GetPrimitiveParser(elementTypes[1]).GetParserValue(values[1]) } ));
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