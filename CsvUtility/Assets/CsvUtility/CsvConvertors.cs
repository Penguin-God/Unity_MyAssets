using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ParserCore;

namespace CsvConvertors
{
    interface ICsvConvertor
    {
        object TextToObject(string text, Type type);
    }

    class ConvertorFactory
    {
        public ICsvConvertor GetCsvConvertor(Type type)
        {
            if (type == typeof(int)) return new PrimitiveConvertor();
            else if (TypeIdentifier.IsList(type)) return new ListConvertor();
            return null;
        }
    }

    class PrimitiveConvertor : ICsvConvertor
    {
        public object TextToObject(string text, Type type)
        {
            return Convert.ChangeType(text, type);
        }
    }

    class ListConvertor : ICsvConvertor
    {
        public object TextToObject(string text, Type type) => null;
    }
}
