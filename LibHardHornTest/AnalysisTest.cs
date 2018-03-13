﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HardHorn.Archiving;
using System.Collections.Generic;
using HardHorn.Analysis;
using LibHardHornTest.Utilities;
using System.Text.RegularExpressions;
using System.Linq;

namespace LibHardHornTest
{
    [TestClass]
    public class AnalysisTest
    {
        [TestMethod]
        public void TestAnalyzeLengths()
        {
            var av = new ArchiveVersion("AVID.TEST.1", "E:\\AVID.TEST.1",
                new Table[] {
                    new Table("TABLE1", "table1", 10, "desc", new List<Column>(new Column[] {
                        new Column(null, "ID", new ParameterizedDataType(DataType.INTEGER, null), "INTEGER", false, "The ID.", "c1", 1, null, null),
                        new Column(null, "DATE", new ParameterizedDataType(DataType.DATE, null), "DATE", false, "", "c2", 2, null, null),
                        new Column(null, "NAME", new ParameterizedDataType(DataType.CHARACTER_VARYING, Parameter.WithLength(10)), "VARCHAR(10)", false, "", "c3", 3, null, null),
                        new Column(null, "UUID", new ParameterizedDataType(DataType.CHARACTER,  Parameter.WithLength(10)), "CHAR(10)", false, "", "c4", 4, null, null)
                    }), new PrimaryKey("PK1", new List<string>(new string[] {"ID" })), new List<ForeignKey>()),
                    new Table("TABLE2", "table2", 10, "desc", new List<Column>(new Column[] {
                        new Column(null, "FOREIGN_ID", new ParameterizedDataType(DataType.INTEGER, null), "INTEGER", false, "", "c1", 1, null, null),
                        new Column(null, "DATA", new ParameterizedDataType(DataType.CHARACTER_VARYING,  Parameter.WithLength(10)), "VARCHAR (10)", false, "", "c2", 2, null, null),
                        new Column(null, "TIMECOL", new ParameterizedDataType(DataType.TIMESTAMP, Parameter.WithPrecision(6)), "TIMESTAMP (6)", false, "", "c3", 3, null, null)
                    }), new PrimaryKey("PK2", new List<string>(new string[] {"ID" })), new List<ForeignKey>())
                });

            var analyzer = new Analyzer(av, new TestLogger());
        }

        public void AssertOkay(Column column, Test test, string data, int line = 0, int pos = 0, bool isNull = false)
        {
            var result = test.GetResult(new Post(data, line, pos, isNull), column);
            Assert.AreEqual(Test.Result.OKAY, result, string.Format("Test {0} on column {1} failed on data '{2}', when expecting success.", test, column, data));
        }

        public void AssertError(Column column, Test test, string data)
        {
            var result = test.GetResult(new Post(data, 0, 0, false), column);
            Assert.AreEqual(Test.Result.ERROR, result, string.Format("Test {0} on column {1} succeeded on data '{2}', when expecting failure.", test, column, data));
        }

        [TestMethod]
        public void TestAnalyzeCharacterOverflowUnderflow()
        {
            var overflowTest = new Test.Overflow();
            var underflowTest = new Test.Underflow();

            uint length = 0;
            var colchar = new Column(null, null, new ParameterizedDataType(DataType.CHARACTER, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            var colvarchar = new Column(null, null, new ParameterizedDataType(DataType.CHARACTER_VARYING, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            var colnatchar = new Column(null, null, new ParameterizedDataType(DataType.NATIONAL_CHARACTER, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            var colnatvarchar = new Column(null, null, new ParameterizedDataType(DataType.NATIONAL_CHARACTER_VARYING, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);

            AssertOkay(colchar, overflowTest, "");
            AssertOkay(colvarchar, overflowTest, "");
            AssertOkay(colnatchar, overflowTest, "");
            AssertOkay(colnatvarchar, overflowTest, "");
            AssertError(colchar, overflowTest, "abc");
            AssertError(colvarchar, overflowTest, "abc");
            AssertError(colnatchar, overflowTest, "abc");
            AssertError(colnatvarchar, overflowTest, "abc");
            AssertOkay(colchar, underflowTest, "");
            AssertOkay(colvarchar, underflowTest, "");
            AssertOkay(colnatchar, underflowTest, "");
            AssertOkay(colnatvarchar, underflowTest, "");

            length = 8;
            colchar = new Column(null, null, new ParameterizedDataType(DataType.CHARACTER, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            colvarchar = new Column(null, null, new ParameterizedDataType(DataType.CHARACTER_VARYING, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            colnatchar = new Column(null, null, new ParameterizedDataType(DataType.NATIONAL_CHARACTER, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);
            colnatvarchar = new Column(null, null, new ParameterizedDataType(DataType.NATIONAL_CHARACTER_VARYING, Parameter.WithLength(length)), null, false, null, "c1", 1, null, null);

            AssertOkay(colchar, overflowTest, "abcdefg");
            AssertOkay(colvarchar, overflowTest, "abcdefg");
            AssertOkay(colnatchar, overflowTest, "abcdefg");
            AssertOkay(colnatvarchar, overflowTest, "abcdefg");

            AssertError(colchar, overflowTest, "abcdefghi");
            AssertError(colvarchar, overflowTest, "abcdefghi");
            AssertError(colnatchar, overflowTest, "abcdefghi");
            AssertError(colnatvarchar, overflowTest, "abcdefghi");

            AssertOkay(colchar, underflowTest, "abcdefgh");
            AssertOkay(colvarchar, underflowTest, "abcdefgh");
            AssertOkay(colnatchar, underflowTest, "abcdefgh");
            AssertOkay(colnatvarchar, underflowTest, "abcdefgh");

            AssertError(colchar, underflowTest, "abcd");
            AssertOkay(colvarchar, underflowTest, "abcd");
            AssertError(colnatchar, underflowTest, "abcd");
            AssertOkay(colnatvarchar, underflowTest, "abcd");
        }

        [TestMethod]
        public void TestAnalyzeBlankTest()
        {
            var test = new Test.Blank();

            foreach (DataType dataType in Enum.GetValues(typeof(DataType)))
            {
                var column = new Column(null, null, new ParameterizedDataType(dataType, Parameter.ChangeDataType(dataType, null)), null, false, null, "c1", 1, null, null);
                AssertOkay(column, test, "abcdefgh!");
                AssertError(column, test, " abcdefgh!");
                AssertError(column, test, "\nabcdefgh!");
                AssertError(column, test, "\rabcdefgh!");
                AssertError(column, test, "\tabcdefgh!");
                AssertError(column, test, "abcdefgh! ");
                AssertError(column, test, "abcdefgh!\t");
                AssertError(column, test, "abcdefgh!\r");
                AssertError(column, test, "abcdefgh!\n");
            }
        }

        [TestMethod]
        public void TestAnalyzeDateFormatTest()
        {
            var test = Test.DateFormatTest();
            var column = new Column(null, "name", new ParameterizedDataType(DataType.DATE, null), "DATE", false, "desc", "c1", 1, null, null);

            AssertOkay(column, test, "0001-01-01");
            AssertOkay(column, test, "2016-05-23");
            AssertOkay(column, test, "2016-05-23");
            AssertOkay(column, test, "2016-05-23");
            AssertOkay(column, test, "1994-12-31");
            AssertOkay(column, test, "2016-12-01");
            AssertOkay(column, test, "2017-01-31");
            AssertOkay(column, test, "1991-05-16");
            AssertOkay(column, test, "9999-12-31");

            AssertError(column, test, "99-1-31"); // two-digit year
            AssertError(column, test, "2017-12-1"); // one-digit-month
            AssertError(column, test, "2017-1-31"); // one-digit day
            AssertError(column, test, "abc"); // letters
            AssertError(column, test, "2016-15-23"); // Month out of range
            AssertError(column, test, "2016-06-80"); // Day out of range
            AssertError(column, test, "1905-10"); // Day missing
            AssertError(column, test, ""); // Empty string
            AssertError(column, test, "   "); // Blank string
        }

        [TestMethod]
        public void TestAnalyzeTimeFormatTest()
        {
            var test = Test.TimeFormatTest();
            var column = new Column(null, "name", new ParameterizedDataType(DataType.TIME, Parameter.WithPrecision(10)), "TIME (10)", false, "desc", "c1", 1, null, null);

            AssertOkay(column, test, "14:15:20");
            AssertOkay(column, test, "00:00:00"); // minimum
            AssertOkay(column, test, "23:59:59"); // maximum

            AssertError(column, test, "14:15:20 "); // with a blank
            AssertError(column, test, " 14:15:20"); // with a blank
            AssertError(column, test, "25:20:11"); // Hour out of range
            AssertError(column, test, "1:11:11"); // 1-digit hour
            AssertError(column, test, "111:11:11"); // 3-digit hour
            AssertError(column, test, "10:63:30"); // minute out of range
            AssertError(column, test, "10:5:30"); // 1-digit minute
            AssertError(column, test, "10:555:30"); // 3-digit minute
            AssertError(column, test, "10:10:99"); // second out of range
            AssertError(column, test, "10:10:1"); // 1-digit second
            AssertError(column, test, "10:10:123"); // 3-digit second
            AssertError(column, test, ""); // empty string
        }

        [TestMethod]
        public void TestAnalyzeTimeWithTimezoneFormatTest()
        {
            var test = Test.TimeWithTimeZoneTest();
            var column = new Column(null, "name", new ParameterizedDataType(DataType.TIME_WITH_TIME_ZONE, Parameter.WithPrecision(10)), "TIME WITH TIME ZONE (10)", false, "desc", "c1", 1, null, null);

            AssertOkay(column, test, "14:15:20Z");
            AssertOkay(column, test, "14:15:20+12:00");
            AssertOkay(column, test, "14:15:20-12:00");
            AssertOkay(column, test, "13:37:00+05:30");
            AssertOkay(column, test, "00:00:00Z"); // minimum
            AssertOkay(column, test, "23:59:59Z"); // maximum
            AssertOkay(column, test, "00:00:00+00:00"); // minimum
            AssertOkay(column, test, "23:59:59+12:00"); // maximum
            AssertOkay(column, test, "23:59:59-00:00"); // minimum
            AssertOkay(column, test, "23:59:59-12:00"); // maximum

            AssertError(column, test, "14:15:20+5:00"); // 1 digit time zone hour
            AssertError(column, test, "14:15:20+005:00"); // 3 digit time zone hour
            AssertError(column, test, "14:15:20+05:0"); // 1 digit time zone minute
            AssertError(column, test, "14:15:20+05:000"); // 3 digit time zone minute
            AssertError(column, test, "14:15:20+13:00"); // time zone hour out of range
            AssertError(column, test, "14:15:20-13:00"); // time zone hour out of range
            AssertError(column, test, "14:15:20+01:60"); // time zone minute out of range
            AssertError(column, test, "14:15:20"); // missing time zone
            AssertError(column, test, "14:15:20+01:00 "); // with a blank
            AssertError(column, test, " 14:15:20+01:00"); // with a blank
            AssertError(column, test, "25:20:11+01:00"); // Hour out of range
            AssertError(column, test, "1:11:11+01:00"); // 1-digit hour
            AssertError(column, test, "111:11:11+01:00"); // 3-digit hour
            AssertError(column, test, "10:63:30+01:00"); // minute out of range
            AssertError(column, test, "10:5:30+01:00"); // 1-digit minute
            AssertError(column, test, "10:555:30+01:00"); // 3-digit minute
            AssertError(column, test, "10:10:99+01:00"); // second out of range
            AssertError(column, test, "10:10:1+01:00"); // 1-digit second
            AssertError(column, test, "10:10:123+01:00"); // 3-digit second
            AssertError(column, test, "abc"); // non-format string
            AssertError(column, test, ""); // empty string
        }

        [TestMethod]
        public void TestAnalyzeTimestampFormatTest()
        {
            var test = Test.TimestampFormatTest();
            var column = new Column(null, "name", new ParameterizedDataType(DataType.TIMESTAMP, Parameter.WithPrecision(10)), "TIMESTAMP (10)", false, "desc", "c1", 1, null, null);

            AssertOkay(column, test, "2000-01-01T14:15:20");
            AssertOkay(column, test, "1899-12-31T10:30:00");
            AssertOkay(column, test, "2018-01-03T17:30:00");
            AssertOkay(column, test, "2000-08-10T13:37:00");
            AssertOkay(column, test, "2000-08-10T00:00:00"); // minimum time
            AssertOkay(column, test, "2000-08-10T23:59:59"); // maximum time

            AssertError(column, test, "2000-08-10T14:15:20+05:00"); // contains correctly formatted time zone
            AssertError(column, test, "2000-08-10T14:15:20+5:00"); // contains incorrectly formatted time zonee
            AssertError(column, test, "2000-08-10T14:15:20 "); // with a blank
            AssertError(column, test, " 2000-08-10T14:15:20"); // with a blank
            AssertError(column, test, "2000-08-10T25:20:11"); // Hour out of range
            AssertError(column, test, "2000-08-10T1:11:11"); // 1-digit hour
            AssertError(column, test, "2000-08-10T111:11:11"); // 3-digit hour
            AssertError(column, test, "2000-08-10T10:63:30"); // minute out of range
            AssertError(column, test, "2000-08-10T10:5:30"); // 1-digit minute
            AssertError(column, test, "2000-08-10T10:555:30"); // 3-digit minute
            AssertError(column, test, "2000-08-10T10:10:99"); // second out of range
            AssertError(column, test, "2000-08-10T10:10:1"); // 1-digit second
            AssertError(column, test, "2000-08-10T10:10:123"); // 3-digit second
            AssertError(column, test, "abc"); // non-format string
            AssertError(column, test, ""); // empty string

            // dates with minimum time
            AssertOkay(column, test, "0001-01-01T00:00:00");
            AssertOkay(column, test, "2016-05-23T00:00:00");
            AssertOkay(column, test, "2016-05-23T00:00:00");
            AssertOkay(column, test, "2016-05-23T00:00:00");
            AssertOkay(column, test, "1994-12-31T00:00:00");
            AssertOkay(column, test, "2016-12-01T00:00:00");
            AssertOkay(column, test, "2017-01-31T00:00:00");
            AssertOkay(column, test, "1991-05-16T00:00:00");
            AssertOkay(column, test, "9999-12-31T00:00:00");

            // dates with maximum time
            AssertOkay(column, test, "0001-01-01T23:59:59");
            AssertOkay(column, test, "2016-05-23T23:59:59");
            AssertOkay(column, test, "2016-05-23T23:59:59");
            AssertOkay(column, test, "2016-05-23T23:59:59");
            AssertOkay(column, test, "1994-12-31T23:59:59");
            AssertOkay(column, test, "2016-12-01T23:59:59");
            AssertOkay(column, test, "2017-01-31T23:59:59");
            AssertOkay(column, test, "1991-05-16T23:59:59");
            AssertOkay(column, test, "9999-12-31T23:59:59");

            // dates with other time
            AssertOkay(column, test, "0001-01-01T14:15:20");
            AssertOkay(column, test, "2016-05-23T14:15:20");
            AssertOkay(column, test, "2016-05-23T14:15:20");
            AssertOkay(column, test, "2016-05-23T14:15:20");
            AssertOkay(column, test, "1994-12-31T14:15:20");
            AssertOkay(column, test, "2016-12-01T14:15:20");
            AssertOkay(column, test, "2017-01-31T14:15:20");
            AssertOkay(column, test, "1991-05-16T14:15:20");
            AssertOkay(column, test, "9999-12-31T14:15:20");

            AssertError(column, test, "99-1-31T14:15:20"); // two-digit year
            AssertError(column, test, "2017-12-1T14:15:20"); // one-digit-month
            AssertError(column, test, "2017-1-31T14:15:20"); // one-digit day
            AssertError(column, test, "abc"); // letters
            AssertError(column, test, "2016-15-23T14:15:20"); // Month out of range
            AssertError(column, test, "2016-06-80T14:15:20"); // Day out of range
            AssertError(column, test, "1905-10T14:15:20"); // Day missing
            AssertError(column, test, ""); // Empty string
            AssertError(column, test, "   "); // Blank string
        }

        [TestMethod]
        public void TestAnalyzeTimestampWithTimezoneFormatTest()
        {
            var test = Test.TimestampWithTimeZoneFormatTest();
            var column = new Column(null, "name", new ParameterizedDataType(DataType.TIMESTAMP_WITH_TIME_ZONE, Parameter.WithPrecision(10)), "TIMESTAMP WITH TIME ZONE (10)", false, "desc", "c1", 1, null, null);

            AssertOkay(column, test, "2000-08-10T14:15:20Z");
            AssertOkay(column, test, "2000-08-10T14:15:20+12:00");
            AssertOkay(column, test, "2000-08-10T14:15:20-12:00");
            AssertOkay(column, test, "2000-08-10T13:37:00+05:30");
            AssertOkay(column, test, "2000-08-10T00:00:00Z"); // minimum tz
            AssertOkay(column, test, "2000-08-10T23:59:59Z"); // maximum tz
            AssertOkay(column, test, "2000-08-10T00:00:00+00:00"); // minimum time with positive minimum tz
            AssertOkay(column, test, "2000-08-10T23:59:59+12:00"); // maximum time with positive maximum tz
            AssertOkay(column, test, "2000-08-10T23:59:59-00:00"); // minimum time with negative minimum tz
            AssertOkay(column, test, "2000-08-10T23:59:59-12:00"); // maximum time with negative maximum tz

            AssertError(column, test, "2000-08-10T14:15:20+5:00"); // 1 digit time zone hour
            AssertError(column, test, "2000-08-10T14:15:20+005:00"); // 3 digit time zone hour
            AssertError(column, test, "2000-08-10T14:15:20+05:0"); // 1 digit time zone minute
            AssertError(column, test, "2000-08-10T14:15:20+05:000"); // 3 digit time zone minute
            AssertError(column, test, "2000-08-10T14:15:20+13:00"); // time zone hour out of range
            AssertError(column, test, "2000-08-10T14:15:20-13:00"); // time zone hour out of range
            AssertError(column, test, "2000-08-10T14:15:20+01:60"); // time zone minute out of range
            AssertError(column, test, "2000-08-10T14:15:20"); // missing time zone
            AssertError(column, test, "2000-08-10T14:15:20+01:00 "); // with a blank
            AssertError(column, test, " 2000-08-10T14:15:20+01:00"); // with a blank
            AssertError(column, test, "2000-08-10T25:20:11+01:00"); // Hour out of range
            AssertError(column, test, "2000-08-10T1:11:11+01:00"); // 1-digit hour
            AssertError(column, test, "2000-08-10T111:11:11+01:00"); // 3-digit hour
            AssertError(column, test, "2000-08-10T10:63:30+01:00"); // minute out of range
            AssertError(column, test, "2000-08-10T10:5:30+01:00"); // 1-digit minute
            AssertError(column, test, "2000-08-10T10:555:30+01:00"); // 3-digit minute
            AssertError(column, test, "2000-08-10T10:10:99+01:00"); // second out of range
            AssertError(column, test, "2000-08-10T10:10:1+01:00"); // 1-digit second
            AssertError(column, test, "2000-08-10T10:10:123+01:00"); // 3-digit second
            AssertError(column, test, "abc"); // non-format string
            AssertError(column, test, ""); // empty string

            // dates with minimum time
            AssertOkay(column, test, "0001-01-01T00:00:00Z");
            AssertOkay(column, test, "2016-05-23T00:00:00Z");
            AssertOkay(column, test, "2016-05-23T00:00:00Z");
            AssertOkay(column, test, "2016-05-23T00:00:00Z");
            AssertOkay(column, test, "1994-12-31T00:00:00Z");
            AssertOkay(column, test, "2016-12-01T00:00:00Z");
            AssertOkay(column, test, "2017-01-31T00:00:00Z");
            AssertOkay(column, test, "1991-05-16T00:00:00Z");
            AssertOkay(column, test, "9999-12-31T00:00:00Z");

            // dates with maximum time
            AssertOkay(column, test, "0001-01-01T23:59:59Z");
            AssertOkay(column, test, "2016-05-23T23:59:59Z");
            AssertOkay(column, test, "2016-05-23T23:59:59Z");
            AssertOkay(column, test, "2016-05-23T23:59:59Z");
            AssertOkay(column, test, "1994-12-31T23:59:59Z");
            AssertOkay(column, test, "2016-12-01T23:59:59Z");
            AssertOkay(column, test, "2017-01-31T23:59:59Z");
            AssertOkay(column, test, "1991-05-16T23:59:59Z");
            AssertOkay(column, test, "9999-12-31T23:59:59Z");

            // dates with other time
            AssertOkay(column, test, "0001-01-01T14:15:20Z");
            AssertOkay(column, test, "2016-05-23T14:15:20Z");
            AssertOkay(column, test, "2016-05-23T14:15:20Z");
            AssertOkay(column, test, "2016-05-23T14:15:20Z");
            AssertOkay(column, test, "1994-12-31T14:15:20Z");
            AssertOkay(column, test, "2016-12-01T14:15:20Z");
            AssertOkay(column, test, "2017-01-31T14:15:20Z");
            AssertOkay(column, test, "1991-05-16T14:15:20Z");
            AssertOkay(column, test, "9999-12-31T14:15:20Z");

            AssertError(column, test, "99-1-31T14:15:20Z"); // two-digit year
            AssertError(column, test, "2017-12-1T14:15:20Z"); // one-digit-month
            AssertError(column, test, "2017-1-31T14:15:20Z"); // one-digit day
            AssertError(column, test, "abc"); // letters
            AssertError(column, test, "2016-15-23T14:15:20Z"); // Month out of range
            AssertError(column, test, "2016-06-80T14:15:20Z"); // Day out of range
            AssertError(column, test, "1905-10T14:15:20Z"); // Day missing
            AssertError(column, test, ""); // Empty string
            AssertError(column, test, "   "); // Blank string
        }

        [TestMethod]
        public void TestPatternTest()
        {
            var test = new Test.Pattern(new Regex(@"[a-zA-Z][0-9]"), m =>
            {
                foreach (Match match in m)
                {
                    if (match.Success)
                    {
                        return Test.Result.OKAY;
                    }
                }

                return Test.Result.ERROR;
            });
            var column = new Column(null, "name", new ParameterizedDataType(DataType.CHARACTER_VARYING, Parameter.WithLength(100)), "VARCHAR (100)", false, "desc", "c1", 1, null, null);
            AssertOkay(column, test, "a1");
            AssertError(column, test, "12");
        }

        [TestMethod]
        [DeploymentItem(@"..\..\TestResources", @"TestResources")]
        public void TestAnalyzer()
        {
            var AV = ArchiveVersion.Load(@"TestResources\AVID.OVERFLOW.1.1", new TestLogger());
            var Analyzer = new Analyzer(AV, new TestLogger());

            var table1 = AV.Tables.First();
            var idColumn = table1.Columns[0];
            Assert.AreEqual(idColumn.ColumnIdNumber, 1);
            var nameColumn = table1.Columns[1];
            Assert.AreEqual(nameColumn.ColumnIdNumber, 2);
            var phoneColumn = table1.Columns[2];
            Assert.AreEqual(phoneColumn.ColumnIdNumber, 3);
            var timeColumn = table1.Columns[3];
            Assert.AreEqual(timeColumn.ColumnIdNumber, 4);

            foreach (var column in AV.Tables.First().Columns)
            {
                if (column.ColumnIdNumber != 1) // First column is integer
                {
                    Analyzer.AddTest(column, new Test.Overflow());
                }
            }

            Analyzer.MoveNextTable();
            Analyzer.InitializeTable();
            Analyzer.AnalyzeRows(3);

            foreach (var columnAnalysis in Analyzer.TestHierachy[table1].Values)
            {
                columnAnalysis.SuggestType();
            }

            Assert.AreEqual(0, Analyzer.TestHierachy[table1][idColumn].ErrorCount);
            Assert.AreEqual(1, Analyzer.TestHierachy[table1][nameColumn].ErrorCount);
            Assert.AreEqual(1, Analyzer.TestHierachy[table1][phoneColumn].ErrorCount);
            Assert.AreEqual(1, Analyzer.TestHierachy[table1][timeColumn].ErrorCount);

            Assert.AreEqual(1, Analyzer.TestHierachy[table1][nameColumn].SuggestedType.Parameter.CompareTo(nameColumn.ParameterizedDataType.Parameter));
            Assert.AreEqual(1, Analyzer.TestHierachy[table1][phoneColumn].SuggestedType.Parameter.CompareTo(phoneColumn.ParameterizedDataType.Parameter));
            Assert.AreEqual(1, Analyzer.TestHierachy[table1][timeColumn].SuggestedType.Parameter.CompareTo(timeColumn.ParameterizedDataType.Parameter));
        }
    }
}
