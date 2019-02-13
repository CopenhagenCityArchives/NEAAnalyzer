﻿using HardHorn.Analysis;
using HardHorn.Archiving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HardHorn.Utility
{
    public enum Severity
    {
        Hint,
        Error
    }

    public enum NotificationType
    {
        XmlError,
        ColumnTypeError,
        TableRowCountError,
        ForeignKeyTypeError,
        ColumnParsing,
        DataTypeSuggestion,
        ForeignKeyTestError,
        ForeignKeyTestBlank,
        AnalysisErrorBlank,
        AnalysisErrorFormat,
        AnalysisErrorOverflow,
        AnalysisErrorRegex,
        AnalysisErrorUnderflow,
        ParameterSuggestion,
        DataTypeIllegalAlias,
        Suggestion,
        AnalysisErrorRepeatingChar,
        AnalysisErrorUnallowedKeyword,
        ForeignKeyReferencedTableMissing
    }

    public delegate void NotificationCallback(INotification notification);


    public interface INotification
    {
        NotificationType Type { get; }
        Severity Severity { get; }
        Column Column { get; }
        Table Table { get; }
        string Message { get; }
        int? Count { get; }
    }

    public class DataTypeIllegalAliasNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.DataTypeIllegalAlias; } }
        public Severity Severity { get { return Severity.Hint; } }
        public Column Column { get; private set; }
        public Table Table { get { return Column.Table; } }
        public string Message { get { return $"DataTypen '{DataTypeValue}' er et ulovligt alias for '{DataTypeUtility.ToString(DataType)}'."; } }
        public int? Count { get { return null; } }
        public AnalysisTestType TestType { get; private set; }
        public string DataTypeValue { get; private set; }
        public DataType DataType { get; private set; }

        public DataTypeIllegalAliasNotification(Column column, string dataTypeValue, DataType dataType)
        {
            Column = column;
            DataTypeValue = dataTypeValue;
            DataType = dataType;
        }
    }


    public class AnalysisErrorNotification : INotification
    {
        public NotificationType Type { get; private set; }
        public Severity Severity { get; private set; }
        public Column Column { get; private set; }
        public Table Table { get { return Column.Table; } }
        public string Message { get; private set; }
        public int? Count { get; set; }
        public AnalysisTestType TestType { get; private set; }
        public Post Post { get; private set; }

        public AnalysisErrorNotification(Test test, Column column, Post post)
        {
            TestType = test.Type;
            Severity = Severity.Error;
            switch (TestType)
            {
                case AnalysisTestType.BLANK:
                    Type = NotificationType.AnalysisErrorBlank;
                    Message = "Der findes blanktegn i starten eller slutningen af visse felter.";
                    break;
                case AnalysisTestType.FORMAT:
                    Type = NotificationType.AnalysisErrorFormat;
                    Message = "Data har ikke det af datatypen påkrævede format.";
                    break;
                case AnalysisTestType.OVERFLOW:
                    Type = NotificationType.AnalysisErrorOverflow;
                    Message = "Data overskrider den maksimale længde defineret af datatypen.";
                    break;
                case AnalysisTestType.REGEX:
                    Type = NotificationType.AnalysisErrorRegex;
                    Message = null;
                    break;
                case AnalysisTestType.UNDERFLOW:
                    Type = NotificationType.AnalysisErrorUnderflow;
                    Message = "Data når ikke den minimale længde defineret af datatypen.";
                    Severity = Severity.Hint;
                    break;
                case AnalysisTestType.REPEATING_CHAR:
                    Severity = Severity.Hint;
                    var repcharTest = (Test.RepeatingChar) test;
                    var strB = new StringBuilder();
                    foreach (KeyValuePair<string, int> pair in repcharTest.Maximum)
                        strB.AppendFormat($"{pair.Key}({pair.Value}).");
                    Message = strB.ToString(); 
                    Type = NotificationType.AnalysisErrorRepeatingChar;
                    break;
                case AnalysisTestType.UNALLOWED_KEYWORD:
                    Severity = Severity.Hint;
                    var keywordTest = (Test.SuspiciousKeyword)test;
                    var entriesFound = keywordTest.Keywords;//.Where(pair => !pair.Value.Equals(0));
                    var keysFound = keywordTest.Keywords
                        .Where(entry => entry.Value != 0)
                        .Select(entry => entry.Key)
                        .ToList();
                    Message = keysFound.Count() == 0  ?  null : string.Join(" ", keysFound);
                    Type = NotificationType.AnalysisErrorUnallowedKeyword;
                    break;
            }

            Column = column;
            Count = 1;
            Post = post;
        }
    }


    public class ColumnParsingErrorNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ColumnTypeError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get; private set; }
        public Table Table { get; private set; }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public ColumnParsingErrorNotification(Table table, string message)
        {
            Table = Table;
            Message = message;
        }
    }

    public class ColumnTypeErrorNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ColumnTypeError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get; private set; }
        public Table Table { get { return Column.Table; } }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public ColumnTypeErrorNotification(Column column, string message)
        {
            Message = message;
            Column = column;
        }
    }

    public class TableRowCountNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.TableRowCountError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get { return null; } }
        public Table Table { get; private set; }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public TableRowCountNotification(Table table, int actualCount)
        {
            Table = table;
            Message = $"{actualCount} rækker i {Table.Folder}, {table.Rows} rækker defineret i tableIndex";
        }
    }

    public class ForeignKeyTypeErrorNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ForeignKeyTypeError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get { return null; } }
        public Table Table { get; private set; }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public ForeignKeyTypeErrorNotification(ForeignKey foreignKey)
        {
            Table = foreignKey.Table;
            Reference typeErrorReference = foreignKey.References.First(reference => reference.Column.ParameterizedDataType.CompareTo(reference.ReferencedColumn.ParameterizedDataType) != 0);
            Message = $"{foreignKey.Name} refererer {typeErrorReference.Column} til {typeErrorReference.ReferencedColumn} i {typeErrorReference.ReferencedColumn.Table}";
        }
    }

    public class XmlErrorNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.XmlError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get { return null; } }
        public Table Table { get { return null; } }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public XmlErrorNotification(string message)
        {
            Message = $"Xml-validering gav følgende meddelelse: {message}";
        }
    }

    public class SuggestionNotification : INotification
    {
        public NotificationType Type { get; private set; }
        public Severity Severity { get { return Severity.Hint; } }
        public Column Column { get; private set; }
        public Table Table { get { return Column.Table; } }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public int? Count { get { return null; } }

        public SuggestionNotification(Column column, ParameterizedDataType suggestion)
        {
            Column = column;

            if (column.ParameterizedDataType.DataType == suggestion.DataType)
            {
                Type = NotificationType.ParameterSuggestion;
            }
            else
            {
                Type = NotificationType.DataTypeSuggestion;
            }

            Message = suggestion.ToString();
        }
    }

    public class ForeignKeyTestErrorNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ForeignKeyTestError; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get { return null; } }
        public Table Table { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public int? Count { get; private set; }
        public ForeignKey ForeignKey { get; private set; }
        public IDictionary<ForeignKeyValue, int> ErrorValues { get; private set; }

        public ForeignKeyTestErrorNotification(ForeignKey foreignKey, int count, IDictionary<ForeignKeyValue, int> errorValues)
        {
            ForeignKey = foreignKey;
            Table = foreignKey.Table;
            Count = count;
            Message = $"{foreignKey.Name} refererer til værdier der ikke findes i {foreignKey.ReferencedTable}";
            ErrorValues = errorValues;
        }
    }

    public class ForeignKeyTestBlankNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ForeignKeyTestBlank; } }
        public Severity Severity { get { return Severity.Hint; } }
        public Column Column { get { return null; } }
        public Table Table { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public int? Count { get; private set; }
        public ForeignKey ForeignKey { get; private set; }

        public ForeignKeyTestBlankNotification(ForeignKey foreignKey, int count)
        {
            ForeignKey = foreignKey;
            Table = foreignKey.Table;
            Count = count;
            Message = $"Blanke (NULL-værdier) refereres i {foreignKey.Name} til {foreignKey.ReferencedTable}";
        }
    }

    public class ForeignKeyReferencedTableMissingNotification : INotification
    {
        public NotificationType Type { get { return NotificationType.ForeignKeyReferencedTableMissing; } }
        public Severity Severity { get { return Severity.Error; } }
        public Column Column { get { return null; } }
        public Table Table { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }
        public int? Count { get; private set; }
        public ForeignKey ForeignKey { get; private set; }

        public ForeignKeyReferencedTableMissingNotification(ForeignKey foreignKey)
        {
            Table = foreignKey.Table;
            Message = $"{foreignKey.Name} referer til tabellen \"{foreignKey.ReferencedTableName}\", der ikke kan findes.";
            ForeignKey = foreignKey;
        }
    }
}
