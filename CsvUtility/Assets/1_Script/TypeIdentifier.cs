using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class TypeIdentifier
{
    public static bool IsList(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    public static bool IsDictionary(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    public static bool IsIEnumerable(Type type) => type.IsArray || IsList(type) || IsDictionary(type);
    public static bool IsCustom(Type type) => type.ToString().StartsWith("System.") == false;
}
