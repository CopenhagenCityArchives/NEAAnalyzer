﻿using HardHorn.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HardHorn.Archiving
{
    public class ParameterizedDataType : IComparable
    {
        static Regex regex = new Regex(@"^(?<datatype>[a-zA-Z]+( *[a-zA-Z]+)*) *(\((?<params>\d+(, *\d+)*)\))?$", RegexOptions.Compiled);

        DataType _dataType;
        public DataType DataType
        {
            get { return _dataType; }
            set
            {
                // If it is modified, it will stay modified
                IsModified = IsModified || _dataType != value;
                _dataType = value;
            }
        }

        public bool IsModified { get; private set; }

        Parameter _parameter;
        public Parameter Parameter
        {
            get { return _parameter; }
            set
            {
                // If it is modified, it will stay modified
                IsModified = IsModified || _parameter != value && (_parameter == null || _parameter.CompareTo(value) != 0);
                _parameter = value;
            }
        }

        public string Parsed { get; private set; }

        public ParameterizedDataType(DataType dataType, Parameter parameter, string parsed = null)
        {
            IsModified = false;
            _dataType = dataType;
            _parameter = parameter;
            Parsed = parsed;
        }

        public static ParameterizedDataType GetUndefined()
        {
            return new ParameterizedDataType(DataType.UNDEFINED, null);
        }

        public static ParameterizedDataType Parse(XElement element, Table table, Column column)
        {
            var match = regex.Match(element.Value);
            if (match.Success)
            {
                DataType dataType;
                try
                {
                    dataType = DataTypeUtility.Parse(match.Groups["datatype"].Value);
                }
                catch (InvalidOperationException)
                {
                    throw new ArchiveVersionColumnTypeParsingException("Could not parse the datatype.", element.Value, element, column, table);
                }

                var parameterGroup = match.Groups["params"];
                uint[] parameters = null;
                if (parameterGroup.Success)
                {
                    parameters = new List<string>(parameterGroup.Value.Split(',')).Select(n => uint.Parse(n)).ToArray<uint>();
                }

                try
                {
                    Parameter parameter = Parameter.Parse(dataType, parameters);
                    return new ParameterizedDataType(dataType, parameter, element.Value);
                }
                catch (InvalidOperationException)
                {
                    throw new ArchiveVersionColumnTypeParsingException("Invalid parameters.", match.Groups["datatype"].Value, element, column, table);
                }
            }
            else
            {
                throw new ArchiveVersionColumnTypeParsingException("Could not parse the datatype.", element.Value, element, column, table);
            }
        }

        public int CompareTo(object obj)
        {
            var other = obj as ParameterizedDataType;
            if (other != null)
            {
                var dataTypeComp = DataType.CompareTo(other.DataType);
                if (dataTypeComp != 0)
                {
                    return dataTypeComp;
                }
                else if (Parameter != null)
                {
                    return Parameter.CompareTo(other.Parameter);
                }
                else
                {
                    return other.Parameter == null ? 0 : -1;
                }
            }
            else
            {
                return 1;
            }
        }

        public string ToString(bool overwriteUnchangedDataTypes)
        {
            if (overwriteUnchangedDataTypes)
            {
                var repr = "";

                repr += DataTypeUtility.ToString(DataType);

                if (Parameter != null)
                {
                    repr += " " + Parameter.ToString();
                }

                return repr;
            }
            else
            {
                return ToString();
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Parsed) && !IsModified)
            {
                return Parsed;
            }

            var repr = "";

            repr += DataTypeUtility.ToString(DataType);

            if (Parameter != null)
            {
                repr += " " + Parameter.ToString();
            }

            return repr;
        }
    }
}
