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
            return new CsvPairParser();
        else
            return new PrimitiveTypeParser();

        bool IsEnumerable(Type type) 
            => type.IsArray || 
            ( type.IsGenericType && ( type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ) );
        bool IsPair(string typeName) => typeName == "KeyValuePair`2";
    }
}


class PrimitiveTypeParser : CsvParser
{
    public static CsvPrimitiveTypeParser GetPrimitiveParser(Type type)
    {
        if(type == typeof(int)) return new CsvIntParser();
        else if(type == typeof(string)) return new CsvStringParser();
        else if (type == typeof(float)) return new CsvFloatParser();
        else if (type == typeof(bool)) return new CsvBooleanParser();
        else Debug.LogError("Csv 파싱 타입을 찾지 못함");
        return null;
    }

    object GetParserValue(Type type, string value) => GetPrimitiveParser(type).GetParserValue(value);
    public void SetValue(object obj, FieldInfo info, string[] value) => info.SetValue(obj, GetParserValue(info.FieldType, value[0]));
}

class EnumerableTypeParser : CsvParser
{
    EnumerableType _type;
    public EnumerableTypeParser(FieldInfo info)
    {
        _type = GetEnumableType(info.FieldType);

        EnumerableType GetEnumableType(Type type)
        {
            if (type.IsArray) return EnumerableType.Array;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return EnumerableType.List;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return EnumerableType.Dictionary;
            return EnumerableType.Unknown;
        }
    }

    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        switch (_type)
        {
            case EnumerableType.Array: new CsvArrayParser().SetValue(obj, info, values); break;
            case EnumerableType.List: new CsvListParser().SetValue(obj, info, values); break;
            case EnumerableType.Dictionary: new CsvDictionaryParser().SetValue(obj, info, values); break;
        }    
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
    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        Type elementType = info.FieldType.GetGenericArguments()[0];
        ConstructorInfo constructor = info.FieldType.GetConstructors()[2];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementType).GetParserEnumerable(values) }));
    }
}

public class CsvArrayParser
{
    public void SetValue(object obj, FieldInfo info, string[] values)
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
    public void SetValue(object obj, FieldInfo info, string[] values)
    {
        if (values.Length != 2) Debug.LogError($"{info.Name} 입력이 올바르지 않습니다. Key Value 쌍을 정확히 입력했는지 확인해주세요");
        Type[] elementTypes = info.FieldType.GetGenericArguments();
        ConstructorInfo constructor = info.FieldType.GetConstructors()[0];
        info.SetValue(obj, constructor.Invoke(new object[] { PrimitiveTypeParser.GetPrimitiveParser(elementTypes[0]).GetParserValue(values[0]),
                                                             PrimitiveTypeParser.GetPrimitiveParser(elementTypes[1]).GetParserValue(values[1]) } ));
    }
}
