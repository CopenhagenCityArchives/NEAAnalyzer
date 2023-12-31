﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using NEA.Archiving;
using System.Windows.Media;
using System.Windows;
using NEA.Analysis;
using System.Windows.Controls;
using Caliburn.Micro;
using System.Data;
using System.Text.RegularExpressions;
using NEA.Utility;

namespace NEA.Analyzer.Utilities
{
    public class AdditionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var num = value as int?;
            var param = parameter as int?;
            if (num.HasValue && param.HasValue)
            {
                return num.Value + param.Value;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var num = value as int?;
            var param = value as int?;
            if (num.HasValue && param.HasValue)
            {
                return num.Value - param.Value;
            }
            return null;
        }
    }

    public class ValuesToCategoryAxisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var values = value as IEnumerable<uint>;

            if (values != null)
            {
                var list = new HashSet<uint>(values).ToList();
                list.Sort();
                return list.Select(v => v.ToString());
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class KeyTestResultListToDataTable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var keyTestResults = value as Tuple<ForeignKey, int, int, IEnumerable<KeyValuePair<ForeignKeyValue, int>>>;
            if (keyTestResults == null)
            {
                return null;
            }
            var resultsList = keyTestResults.Item4;
            var foreignKey = keyTestResults.Item1;

            if (foreignKey == null || keyTestResults == null)
            {
                return null;
            }

            var dataTable = new DataTable();
            foreach (var reference in foreignKey.References)
            {
                var dataColumn = new DataColumn(string.Format("<{0}: {1}>", reference.Column.ColumnId, reference.ColumnName.Replace("_", "__")), typeof(Post));
                dataTable.Columns.Add(dataColumn);
            }
            dataTable.Columns.Add("Antal fejl", typeof(int));

            foreach (var result in resultsList)
            {
                var objs = new List<object>(result.Key.Values);
                objs.Add(result.Value);
                dataTable.Rows.Add(objs.ToArray());
            }

            return dataTable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotificationTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            NotificationType? type = value as NotificationType?;

            if (!type.HasValue)
            {
                return null;
            }

            return NotificationsUtility.NotificationTypeToString(type.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DivisionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? val = value as double?;
            double? param = parameter as double?;
            if (!val.HasValue || !param.HasValue)
                return null;
            return val / param;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? val = value as double?;
            double? param = parameter as double?;
            if (!val.HasValue || !param.HasValue)
                return null;
            return val * param;
        }
    }

    public class CellIsEmptyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is System.Data.DataRow)
            {
                var cell = values[0] as System.Windows.Controls.DataGridCell;
                var row = values[1] as System.Data.DataRow;
                var columnName = cell.Column.SortMemberPath;

                var post = row[columnName] as Post;
                if (post == null)
                {
                    return false;
                }
                if (!post.IsNull && string.IsNullOrEmpty(post.Data))
                {
                    return true;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CellIsNullConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is System.Data.DataRow)
            {
                var cell = values[0] as System.Windows.Controls.DataGridCell;
                var row = values[1] as System.Data.DataRow;
                var columnName = cell.Column.SortMemberPath;

                var post = row[columnName] as Post;
                if (post == null)
                {
                    return false;
                }

                if (post.IsNull)
                {
                    return true;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class RecentLocationsMenuItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var locations = value as IEnumerable<string>;

            if (locations == null || locations.Count() == 0)
            {
                return Enumerable.Empty<MenuItem>();
            }

            var menuItems = new List<Control>();
            int i = 1;
            foreach (var location in locations)
            {
                var menuItem = new MenuItem();
                menuItem.Header = i++ + ": " + location;
                Message.SetAttach(menuItem, "LoadLocation('" + location + "')");
                menuItems.Add(menuItem);
            }

            if (menuItems.Count > 0)
            {
                menuItems.Add(new Separator());
            }

            return menuItems;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ParameterizedDataTypeToParameterStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramDataType = value as ParameterizedDataType;
            if (paramDataType == null || paramDataType.Parameter == null)
            {
                return null;
            }

            return paramDataType.Parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ParameterToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = value as Archiving.Parameter;
            return param == null ? "" : param.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HasErrorsColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? errors = value as bool?;

            if (errors.HasValue && errors.Value)
            {
                if (parameter == SystemColors.HighlightTextBrush)
                {
                    return new SolidColorBrush(Colors.Tomato);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
            return parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorCountColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? errorCount = value as int?;

            if (errorCount.HasValue && errorCount.Value > 0)
            {
                if (parameter == SystemColors.HighlightTextBrush)
                {
                    return new SolidColorBrush(Colors.Tomato);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
            return parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DataTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var datatype = value as DataType?;

            return datatype.ToString().Replace("_", " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotificationViewModelToForeignKeyDataTable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var notificationViewModel = value as ViewModels.NotificationViewModel;
            if (notificationViewModel == null)
                return null;

            var foreignKey = notificationViewModel.ForeignKey;
            var resultsList = notificationViewModel.ErrorValues;

            if (foreignKey == null || resultsList == null)
            {
                return null;
            }

            var dataTable = new DataTable();
            foreach (var reference in foreignKey.References)
            {
                var dataColumn = new DataColumn(string.Format("<{0}: {1}>", reference.Column.ColumnId, reference.ColumnName.Replace("_", "__")), typeof(Post));
                dataTable.Columns.Add(dataColumn);
            }
            dataTable.Columns.Add(new DataColumn("Antal fejl", typeof(int)));

            foreach (var result in resultsList)
            {
                var objs = new List<object>(result.Key.Values);
                objs.Add(result.Value);
                dataTable.Rows.Add(objs.ToArray());
            }

            return dataTable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            if (str != null)
                return str.Length;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AnalysisErrorTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var testType = value as AnalysisTestType?;
            if (testType.HasValue)
                return AnalysisUtility.AnalysisTestTypeToString(testType.Value);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var truthy = value as bool?;

            if (truthy.HasValue && truthy.Value)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
