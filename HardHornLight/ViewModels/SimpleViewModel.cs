﻿using Caliburn.Micro;

using NEA.Analysis;
using NEA.Archiving;
using NEA.Utility;
using NEA.Logging;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace NEA.Analyzer.ViewModels
{
    class InitializeForeignKeyTestException : Exception
    {
        public InitializeForeignKeyTestException(string message, Exception innerException) : base(message, innerException) { }
    }

    class InitializeAnalysisException : Exception
    {
        public InitializeAnalysisException(string message, Exception innerException) : base(message, innerException) { }
    }

    class SimpleViewModel : PropertyChangedBase
    {
        #region Properties
        ArchiveVersion archiveVersion;
        public ArchiveVersion ArchiveVersion
        {
            get { return archiveVersion; }
            private set { archiveVersion = value; NotifyOfPropertyChange("ArchiveVersion"); NotifyOfPropertyChange("WindowTitle"); }
        }

        public Analysis.Analyzer Analyzer { get; private set; }

        public string WindowTitle
        {
            get
            {
                return $"{(ArchiveVersion != null ? ArchiveVersion.Id + " - " : string.Empty)}{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}";
            }
        }

        ObservableCollection<RecentLocationViewModel> recentLocations;
        public ObservableCollection<RecentLocationViewModel> RecentLocations
        {
            get
            {
                if (recentLocations == null)
                {
                    if (Properties.Settings.Default.RecentLocations == null)
                    {
                        Properties.Settings.Default.RecentLocations = new System.Collections.Specialized.StringCollection();
                        Properties.Settings.Default.Save();
                    }

                    recentLocations = new ObservableCollection<RecentLocationViewModel>();
                    foreach (var location in Properties.Settings.Default.RecentLocations)
                    {
                        recentLocations.Add(new RecentLocationViewModel(location, LoadLocation));
                    }
                }

                return recentLocations;
            }
        }

        string statusMessage;
        public string StatusMessage
        {
            get { return statusMessage; }
            private set { statusMessage = value; NotifyOfPropertyChange("StatusMessage"); }
        }

        LogLevel? statusLevel;
        public LogLevel? StatusLevel
        {
            get { return statusLevel; }
            private set { statusLevel = value; NotifyOfPropertyChange("StatusLevel"); }
        }

        bool loadingArchiveVersion = false;
        public bool LoadingArchiveVersion
        {
            get { return loadingArchiveVersion; }
            private set { loadingArchiveVersion = value; NotifyOfPropertyChange("LoadingArchiveVersion"); }
        }

        TaskbarItemProgressState progressState = TaskbarItemProgressState.None;
        public TaskbarItemProgressState ProgressState
        {
            get { return progressState; }
            private set { progressState = value; NotifyOfPropertyChange("ProgressState"); }
        }

        public ObservableCollection<NotificationViewModel> Notifications { get; private set; }
        public Dictionary<Column, Dictionary<AnalysisTestType, NotificationViewModel>> AnalysisNotificationsMap { get; private set; }
        Dictionary<string, NotificationViewModel> ForeignKeyTestErrorNotificationsMap { get; set; }
        Dictionary<string, NotificationViewModel> ForeignKeyTestBlankNotificationsMap { get; set; }

        System.Timers.Timer Notifications_RefreshViewTimer = new System.Timers.Timer(1000);
        int notificationCount = 0;
        public int NotificationCount
        {
            get { return notificationCount; }
            set { notificationCount = value; NotifyOfPropertyChange("NotificationCount"); }
        }

        bool notifications_ShowHints = true;
        public bool Notifications_ShowHints
        {
            get { return notifications_ShowHints; }
            set { notifications_ShowHints = value; Notifications_RefreshViews(); }
        }
        bool notifications_ShowErrors = true;
        public bool Notifications_ShowErrors
        {
            get { return notifications_ShowErrors; }
            set { notifications_ShowErrors = value; Notifications_RefreshViews(); }
        }

        public bool? Notifications_ShowCategoryAnalysis
        {
            get
            {
                if (Notifications_ShowOverflow
                    && Notifications_ShowUnderflow
                    && Notifications_ShowFormat
                    && Notifications_ShowBlank
                    && Notifications_ShowRegex
                    && Notifications_ShowRepeatingChar
                    && Notifications_ShowSuspiciousKeywords)
                {
                    return true;
                }
                else if (Notifications_ShowOverflow
                    || Notifications_ShowUnderflow
                    || Notifications_ShowFormat
                    || Notifications_ShowBlank
                    || Notifications_ShowRegex
                    || Notifications_ShowRepeatingChar
                    || Notifications_ShowSuspiciousKeywords)
                {
                    return null;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (value.HasValue)
                {
                    Notifications_ShowOverflow = value.Value;
                    Notifications_ShowUnderflow = value.Value;
                    Notifications_ShowFormat = value.Value;
                    Notifications_ShowBlank = value.Value;
                    Notifications_ShowRegex = value.Value;
                    Notifications_ShowRepeatingChar = value.Value;
                    Notifications_ShowSuspiciousKeywords = value.Value;
                }
                NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis");
            }
        }
        bool notifications_ShowOverflow = true;
        public bool Notifications_ShowOverflow
        {
            get { return notifications_ShowOverflow; }
            set { notifications_ShowOverflow = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowOverflow"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowUnderflow = true;
        public bool Notifications_ShowUnderflow
        {
            get { return notifications_ShowUnderflow; }
            set { notifications_ShowUnderflow = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowUnderflow"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowFormat = true;
        public bool Notifications_ShowFormat
        {
            get { return notifications_ShowFormat; }
            set { notifications_ShowFormat = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowFormat"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowBlank = true;
        public bool Notifications_ShowBlank
        {
            get { return notifications_ShowBlank; }
            set { notifications_ShowBlank = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowBlank"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowRegex = true;
        public bool Notifications_ShowRegex
        {
            get { return notifications_ShowRegex; }
            set { notifications_ShowRegex = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowRegex"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowSuspiciousKeywords = true;
        public bool Notifications_ShowSuspiciousKeywords
        {
            get { return notifications_ShowSuspiciousKeywords; }
            set { notifications_ShowSuspiciousKeywords = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowSuspiciousKeywords"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }
        bool notifications_ShowRepeatingChar = true;
        public bool Notifications_ShowRepeatingChar
        {
            get { return notifications_ShowRepeatingChar; }
            set { notifications_ShowRepeatingChar = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowRepeatingChar"); NotifyOfPropertyChange("Notifications_ShowCategoryAnalysis"); }
        }

        public bool? Notifications_ShowCategoryForeignKeyTest
        {
            get
            {
                if (Notifications_ShowForeignKeyTestErrors && Notifications_ShowForeignKeyTestBlanks)
                {
                    return true;
                }
                else if (Notifications_ShowForeignKeyTestErrors || Notifications_ShowForeignKeyTestBlanks)
                {
                    return null;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (value.HasValue)
                {
                    Notifications_ShowForeignKeyTestErrors = value.Value;
                    Notifications_ShowForeignKeyTestBlanks = value.Value;
                }
                NotifyOfPropertyChange("Notifications_ShowCategoryForeignKeyTest");
            }
        }
        bool notifications_ShowForeignKeyTestErrors = true;
        public bool Notifications_ShowForeignKeyTestErrors
        {
            get { return notifications_ShowForeignKeyTestErrors; }
            set { notifications_ShowForeignKeyTestErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowForeignKeyTestErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryForeignKeyTest"); }
        }
        bool notifications_ShowForeignKeyTestBlanks = true;
        public bool Notifications_ShowForeignKeyTestBlanks
        {
            get { return notifications_ShowForeignKeyTestBlanks; }
            set { notifications_ShowForeignKeyTestBlanks = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowForeignKeyTestBlanks"); NotifyOfPropertyChange("Notifications_ShowCategoryForeignKeyTest"); }
        }

        public bool? Notifications_ShowCategorySuggestions
        {
            get
            {
                if (Notifications_ShowDatatypeSuggestions && Notifications_ShowParameterSuggestions && Notifications_ShowSuggestionsWhereErrors)
                {
                    return true;
                }
                else if (Notifications_ShowDatatypeSuggestions || Notifications_ShowParameterSuggestions || Notifications_ShowSuggestionsWhereErrors)
                {
                    return null;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (value.HasValue)
                {
                    Notifications_ShowDatatypeSuggestions = value.Value;
                    Notifications_ShowParameterSuggestions = value.Value;
                    Notifications_ShowSuggestionsWhereErrors = value.Value;
                }
                NotifyOfPropertyChange("Notifications_ShowCategorySuggestions");
            }
        }
        bool notifications_ShowParameterSuggestions = true;
        public bool Notifications_ShowParameterSuggestions
        {
            get { return notifications_ShowParameterSuggestions; }
            set { notifications_ShowParameterSuggestions = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowParameterSuggestions"); NotifyOfPropertyChange("Notifications_ShowCategorySuggestions"); }
        }
        bool notifications_ShowDatatypeSuggestions = true;
        public bool Notifications_ShowDatatypeSuggestions
        {
            get { return notifications_ShowDatatypeSuggestions; }
            set { notifications_ShowDatatypeSuggestions = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowDatatypeSuggestions"); NotifyOfPropertyChange("Notifications_ShowCategorySuggestions"); }
        }
        bool notifications_ShowSuggestionsWhereErrors = true;
        public bool Notifications_ShowSuggestionsWhereErrors
        {
            get { return notifications_ShowSuggestionsWhereErrors; }
            set { notifications_ShowSuggestionsWhereErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowSuggestionsWhereErrors"); NotifyOfPropertyChange("Notifications_ShowCategorySuggestions"); }
        }


        public bool? Notifications_ShowCategoryStructure
        {
            get
            {
                if (Notifications_ShowXmlValidationErrors
                    && Notifications_ShowDataTypeIllegalAliasErrors
                    && Notifications_ShowColumnErrors
                    && Notifications_ShowColumnTypeErrors
                    && Notifications_ShowForeignKeyTypeErrors
                    && Notifications_ShowTableRowCountErrors)
                {
                    return true;
                }
                else if (Notifications_ShowXmlValidationErrors
                    || Notifications_ShowDataTypeIllegalAliasErrors
                    || Notifications_ShowColumnErrors
                    || Notifications_ShowColumnTypeErrors
                    || Notifications_ShowForeignKeyTypeErrors

                    || Notifications_ShowTableRowCountErrors)
                {
                    return null;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (value.HasValue)
                {
                    Notifications_ShowXmlValidationErrors = value.Value;
                    Notifications_ShowDataTypeIllegalAliasErrors = value.Value;
                    Notifications_ShowColumnErrors = value.Value;
                    Notifications_ShowColumnTypeErrors = value.Value;
                    Notifications_ShowForeignKeyTypeErrors = value.Value;
                    Notifications_ShowTableRowCountErrors = value.Value;
                }
                NotifyOfPropertyChange("Notifications_ShowCategoryStructure");
            }
        }
        bool notifications_ShowXmlValidationErrors = true;
        public bool Notifications_ShowXmlValidationErrors
        {
            get { return notifications_ShowXmlValidationErrors; }
            set { notifications_ShowXmlValidationErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowXmlValidationErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowColumnErrors = true;
        public bool Notifications_ShowColumnErrors
        {
            get { return notifications_ShowColumnErrors; }
            set { notifications_ShowColumnErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowColumnErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowColumnTypeErrors = true;
        public bool Notifications_ShowColumnTypeErrors
        {
            get { return notifications_ShowColumnTypeErrors; }
            set { notifications_ShowColumnTypeErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowColumnTypeErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowDataTypeIllegalAliasErrors = true;
        public bool Notifications_ShowDataTypeIllegalAliasErrors
        {
            get { return notifications_ShowDataTypeIllegalAliasErrors; }
            set { notifications_ShowDataTypeIllegalAliasErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowDataTypeIllegalAliasErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowForeignKeyTypeErrors = true;
        public bool Notifications_ShowForeignKeyTypeErrors
        {
            get { return notifications_ShowForeignKeyTypeErrors; }
            set { notifications_ShowForeignKeyTypeErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowForeignKeyTypeErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowTableRowCountErrors = true;
        public bool Notifications_ShowTableRowCountErrors
        {
            get { return notifications_ShowTableRowCountErrors; }
            set { notifications_ShowTableRowCountErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowTableRowCountErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }
        bool notifications_ShowForeignKeyReferencedTableMissingErrors = true;
        public bool Notifications_ShowForeignKeyReferencedTableMissingErrors
        {
            get { return notifications_ShowForeignKeyReferencedTableMissingErrors; }
            set { notifications_ShowForeignKeyReferencedTableMissingErrors = value; Notifications_RefreshViews(); NotifyOfPropertyChange("Notifications_ShowForeignKeyReferencedTableMissingErrors"); NotifyOfPropertyChange("Notifications_ShowCategoryStructure"); }
        }

        public CollectionViewSource NotificationsViewSource { get; private set; }
        public ICollectionView NotificationsView { get; set; }
        public CollectionViewSource NotificationsCategoryViewSource { get; private set; }
        public ICollectionView NotificationsCategoryView { get; set; }
        public int Notifications_SelectedGroupingIndex { get; set; }
        NotificationViewModel selectedNotification;
        public NotificationViewModel SelectedNotification
        {
            get { return selectedNotification; }
            set { selectedNotification = value; NotifyOfPropertyChange("SelectedNotification"); }
        }

        public ObservableCollection<TaskViewModel> Tasks { get; private set; }
        public TaskViewModel CurrentTask { get; private set; }
        public long ProgressTaskTotal { get; private set; }
        public long ProgressKeyTestTotal { get; private set; }
        public long ProgressAnalysisTotal { get; private set; }
        public double ProgressValue { get; private set; }
        public double ProgressValueTask { get; private set; }

        public bool PerformAnalysis
        {
            get
            {
                return Properties.Settings.Default.PerformAnalysis;
            }

            set
            {
                Properties.Settings.Default.PerformAnalysis = value;
                Properties.Settings.Default.Save();
                NotifyOfPropertyChange("PerformAnalysis");
            }
        }


        public bool PerformKeyTest
        {
            get
            {
                return Properties.Settings.Default.PerformKeyTest;
            }

            set
            {
                Properties.Settings.Default.PerformKeyTest = value;
                Properties.Settings.Default.Save();
                NotifyOfPropertyChange("PerformKeyTest");
            }
        }

        List<Post> AnalysisErrorSamplesSelection = new List<Post>();
        List<System.Data.DataRowView> ForeignKeyTestErrorSamplesSelection = new List<System.Data.DataRowView>();
        #endregion

        #region Constructors
        public SimpleViewModel()
        {
            Notifications = new ObservableCollection<NotificationViewModel>();
            AnalysisNotificationsMap = new Dictionary<Column, Dictionary<AnalysisTestType, NotificationViewModel>>();
            ForeignKeyTestErrorNotificationsMap = new Dictionary<string, NotificationViewModel>();
            ForeignKeyTestBlankNotificationsMap = new Dictionary<string, NotificationViewModel>();
            Notifications.CollectionChanged += (o, a) => Notifications_RefreshViews();
            NotificationsViewSource = new CollectionViewSource() { Source = Notifications };
            NotificationsView = NotificationsViewSource.View;
            NotificationsCategoryViewSource = new CollectionViewSource() { Source = Notifications };
            NotificationsCategoryView = NotificationsCategoryViewSource.View;
            NotificationsView.GroupDescriptions.Add(new PropertyGroupDescription("Table"));
            NotificationsView.SortDescriptions.Add(new SortDescription("Table.FolderNumber", ListSortDirection.Ascending));
            NotificationsView.SortDescriptions.Add(new SortDescription("Column.ColumnIdNumber", ListSortDirection.Ascending));
            NotificationsView.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
            NotificationsView.SortDescriptions.Add(new SortDescription("Message", ListSortDirection.Ascending));
            NotificationsView.Filter += Notifications_Filter;
            NotificationsCategoryView.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
            NotificationsCategoryView.SortDescriptions.Add(new SortDescription("Table.FolderNumber", ListSortDirection.Ascending));
            NotificationsCategoryView.SortDescriptions.Add(new SortDescription("Column.ColumnIdNumber", ListSortDirection.Ascending));
            NotificationsCategoryView.SortDescriptions.Add(new SortDescription("Message", ListSortDirection.Ascending));
            NotificationsCategoryView.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
            NotificationsCategoryView.Filter += Notifications_Filter;
            Notifications_RefreshViewTimer.Elapsed += Notifications_RefreshViewTimer_Elapsed;
            Tasks = new ObservableCollection<TaskViewModel>();

            var args = Environment.GetCommandLineArgs();
            string location = args.FirstOrDefault(Directory.Exists);

            if (location != null)
            {
                LoadLocation(location);
            }
        }
        #endregion

        #region Methods
        private bool Notifications_Filter(object obj)
        {
            var noti = obj as INotification;
            bool includeBySeverity = (noti.Severity == Severity.Hint && Notifications_ShowHints)
                                  || (noti.Severity == Severity.Error && Notifications_ShowErrors);
            bool includeByNotificationType = (noti.Type == NotificationType.AnalysisErrorOverflow && Notifications_ShowOverflow)
                || (noti.Type == NotificationType.AnalysisErrorUnderflow && Notifications_ShowUnderflow)
                || (noti.Type == NotificationType.AnalysisErrorFormat && Notifications_ShowFormat)
                || (noti.Type == NotificationType.AnalysisErrorBlank && Notifications_ShowBlank)
                || (noti.Type == NotificationType.AnalysisErrorRegex && Notifications_ShowRegex)
                || (noti.Type == NotificationType.AnalysisErrorUnallowedKeyword && Notifications_ShowSuspiciousKeywords)
                || (noti.Type == NotificationType.AnalysisErrorRepeatingCharacter && Notifications_ShowRepeatingChar)
                || (noti.Type == NotificationType.ForeignKeyTestError && Notifications_ShowForeignKeyTestErrors)
                || (noti.Type == NotificationType.ForeignKeyTestBlank && Notifications_ShowForeignKeyTestBlanks)
                || (noti.Type == NotificationType.ForeignKeyReferencedTableMissing && Notifications_ShowForeignKeyReferencedTableMissingErrors)
                || (noti.Type == NotificationType.ParameterSuggestion && Notifications_ShowParameterSuggestions)
                || (noti.Type == NotificationType.DataTypeSuggestion && Notifications_ShowDatatypeSuggestions)
                || (noti.Type == NotificationType.XmlError && Notifications_ShowXmlValidationErrors)
                || (noti.Type == NotificationType.ColumnParsing && Notifications_ShowColumnErrors)
                || (noti.Type == NotificationType.ColumnTypeError && Notifications_ShowColumnTypeErrors)
                || (noti.Type == NotificationType.DataTypeIllegalAlias && Notifications_ShowDataTypeIllegalAliasErrors)
                || (noti.Type == NotificationType.ForeignKeyTypeError && Notifications_ShowForeignKeyTypeErrors)
                || (noti.Type == NotificationType.TableRowCountError && Notifications_ShowTableRowCountErrors);
            bool includeSuggestionsWhereErrors =
                (noti.Type == NotificationType.DataTypeSuggestion || noti.Type == NotificationType.ParameterSuggestion)
                && AnalysisNotificationsMap.ContainsKey(noti.Column)
                && (AnalysisNotificationsMap[noti.Column].ContainsKey(AnalysisTestType.FORMAT) || AnalysisNotificationsMap[noti.Column].ContainsKey(AnalysisTestType.OVERFLOW));
            return includeSuggestionsWhereErrors || (includeBySeverity && includeByNotificationType);
        }

        public void SetStatus(string msg, LogLevel level = LogLevel.NORMAL)
        {
            if (level == LogLevel.SECTION || level == LogLevel.ERROR)
            {
                StatusMessage = msg;
                StatusLevel = level;
            }
        }

        Random random = new Random();
        public void HandleNotification(INotification notification)
        {
            NotificationViewModel viewModel = null;
            switch (notification.Type)
            {
                case NotificationType.AnalysisErrorBlank:
                case NotificationType.AnalysisErrorOverflow:
                case NotificationType.AnalysisErrorUnderflow:
                case NotificationType.AnalysisErrorFormat:
                case NotificationType.AnalysisErrorRegex:
                case NotificationType.AnalysisErrorRepeatingCharacter:
                case NotificationType.AnalysisErrorUnallowedKeyword:
                    if (!AnalysisNotificationsMap.ContainsKey(notification.Column))
                    {
                        AnalysisNotificationsMap[notification.Column] = new Dictionary<AnalysisTestType, NotificationViewModel>();
                    }

                    var testType = (notification as AnalysisErrorNotification).TestType;
                    if (AnalysisNotificationsMap[notification.Column].ContainsKey(testType))
                    {
                        AnalysisNotificationsMap[notification.Column][(notification as AnalysisErrorNotification).TestType].Count++;
                        if (AnalysisNotificationsMap[notification.Column][testType].Sample.Count < Analysis.Analyzer.SampleSize)
                        {
                            AnalysisNotificationsMap[notification.Column][testType].Sample.Add((notification as AnalysisErrorNotification).Post);
                        }
                        else if (random.Next((notification as AnalysisErrorNotification).Post.RowIndex) < Analysis.Analyzer.SampleSize)
                        {
                            int index = random.Next(Analysis.Analyzer.SampleSize);
                            AnalysisNotificationsMap[notification.Column][testType].Sample[index] = (notification as AnalysisErrorNotification).Post;
                        }
                        if (notification.Type == NotificationType.AnalysisErrorUnallowedKeyword)
                        {
                            AnalysisNotificationsMap[notification.Column][testType].Message = notification.Message;
                        }
                        if (notification.Type == NotificationType.AnalysisErrorRepeatingCharacter)
                        {
                            if (!(AnalysisNotificationsMap[notification.Column][testType].Message.Contains(notification.Message)))
                            {
                                AnalysisNotificationsMap[notification.Column][testType].Message += notification.Message;
                            }
                        }
                    }
                    else
                    {
                        viewModel = new NotificationViewModel(notification);
                        AnalysisNotificationsMap[notification.Column][(notification as AnalysisErrorNotification).TestType] = viewModel;
                    }
                    break;
                case NotificationType.ForeignKeyTestError:
                    var foreignKeyTestErrorNotification = notification as ForeignKeyTestErrorNotification;
                    if (ForeignKeyTestErrorNotificationsMap.ContainsKey(foreignKeyTestErrorNotification.ForeignKey.Name))
                    {
                        ForeignKeyTestErrorNotificationsMap[foreignKeyTestErrorNotification.ForeignKey.Name].Count = foreignKeyTestErrorNotification.Count;
                        ForeignKeyTestErrorNotificationsMap[foreignKeyTestErrorNotification.ForeignKey.Name].ErrorValues = foreignKeyTestErrorNotification.ErrorValues;
                    }
                    else
                    {
                        viewModel = new NotificationViewModel(notification);
                        ForeignKeyTestErrorNotificationsMap[foreignKeyTestErrorNotification.ForeignKey.Name] = viewModel;
                    }
                    break;
                case NotificationType.ForeignKeyTestBlank:
                    var foreignKeyTestBlankNotification = notification as ForeignKeyTestBlankNotification;
                    if (ForeignKeyTestBlankNotificationsMap.ContainsKey(foreignKeyTestBlankNotification.ForeignKey.Name))
                    {
                        ForeignKeyTestBlankNotificationsMap[foreignKeyTestBlankNotification.ForeignKey.Name].Count = foreignKeyTestBlankNotification.Count;
                    }
                    else
                    {
                        viewModel = new NotificationViewModel(notification);
                        ForeignKeyTestBlankNotificationsMap[foreignKeyTestBlankNotification.ForeignKey.Name] = viewModel;
                    }
                    break;
                default:
                    viewModel = new NotificationViewModel(notification);
                    break;
            }

            if (viewModel != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Notifications.Add(viewModel);
                });
            }
        }

        public void Notifications_RefreshViews()
        {
            if (!Notifications_RefreshViewTimer.Enabled)
            {
                Notifications_RefreshViewTimer.Start();
            }
        }

        public void Notifications_RefreshViewTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs a)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Notifications_RefreshViewTimer.Stop();
                NotificationsView.Refresh();
                NotificationsCategoryView.Refresh();
                NotificationCount = NotificationsView.Cast<object>().Count();
            });
        }
        #endregion

        #region Actions
        public void OnDrag(DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            if (!args.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var filenames = args.Data.GetData(DataFormats.FileDrop) as string[];
            if (filenames == null || filenames.Length != 1)
                return;

            if (!Directory.Exists(filenames[0]))
                return;

            if (!File.Exists(Path.Combine(filenames[0], "indices", "tableIndex.xml")))
                return;

            args.Effects = DragDropEffects.Link;
        }
        
        public void OnDrop(DragEventArgs args)
        {
            args.Handled = true;

            if (!args.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] filenames = args.Data.GetData(DataFormats.FileDrop) as string[];
            if (filenames == null || filenames.Length != 1)
                return;

            if (!Directory.Exists(filenames[0]))
                return;

            if (!File.Exists(Path.Combine(filenames[0], "indices", "tableIndex.xml")))
                return;

            LoadLocation(filenames[0]);
        }

        public async void LoadLocation(string location)
        {
            if (!Directory.Exists(location) || !File.Exists(Path.Combine(location, "Indices", "tableIndex.xml")))
            {
                SetStatus($"Placeringen '{location}' er ikke en gyldig arkiveringsversion.", LogLevel.ERROR);
            }


            IProgress<long> analysisProgress = new Progress<long>(analysis =>
            {
                if (ProgressAnalysisTotal == 0)
                    ProgressValue = 0;
                else
                    ProgressValue = ((double)analysis / ProgressAnalysisTotal) * 50d;

                NotifyOfPropertyChange("ProgressValue");
            });

            IProgress<long> keyTestProgress = new Progress<long>(keyTest =>
            {
                if (ProgressKeyTestTotal == 0)
                    ProgressValue = 0;
                else
                    ProgressValue = 50d + ((double)keyTest / ProgressKeyTestTotal) * 50d;

                NotifyOfPropertyChange("ProgressValue");
            });

            IProgress<long> taskTotalProgress = new Progress<long>(taskTotal => {
                ProgressTaskTotal = taskTotal;
                NotifyOfPropertyChange("ProgressTaskTotal");
            });

            IProgress<long> taskProgress = new Progress<long>(task =>
            {
                if (ProgressTaskTotal == 0)
                    ProgressValueTask = 0;
                else
                    ProgressValueTask = ((double)task / ProgressTaskTotal) * 100d;

                NotifyOfPropertyChange("ProgressValueTask");
            });

            SetStatus($"Indlæser tabeller fra '{location}'", LogLevel.SECTION);
            LoadingArchiveVersion = true;
            try
            {
                ArchiveVersion av = await Task.Run(() =>
                {
                    return ArchiveVersion.Load(location, null, HandleNotification);
                });

                // Update recents list, since loading was successful.
                AddRecentLocation(location);

                Analysis.Analyzer analyzer = await Task.Run(() =>
                {
                    var ana = new Analysis.Analyzer(av, av.Tables, null);
                    ProgressAnalysisTotal = ana.TotalRowCount;
                    ana.Notify = HandleNotification;

                    foreach (var table in av.Tables)
                    {
                        foreach (var column in table.Columns)
                        {
                            switch (column.ParameterizedDataType.DataType)
                            {
                                case DataType.CHARACTER:
                                case DataType.NATIONAL_CHARACTER:
                                    ana.AddTest(column, new Test.Underflow());
                                    ana.AddTest(column, new Test.Overflow());
                                    ana.AddTest(column, new Test.Blank());
                                    ana.AddTest(column, new Test.RepeatingCharacter());
                                    ana.AddTest(column, new Test.SuspiciousKeyword());
                                    break;
                                case DataType.CHARACTER_VARYING:
                                case DataType.NATIONAL_CHARACTER_VARYING:
                                    ana.AddTest(column, new Test.Overflow());
                                    ana.AddTest(column, new Test.Blank());
                                    ana.AddTest(column, new Test.SuspiciousKeyword());
                                    ana.AddTest(column, new Test.RepeatingCharacter());
                                    break;
                                case DataType.TIMESTAMP:
                                    ana.AddTest(column, new Test.TimestampFormatTest());
                                    ana.AddTest(column, new Test.Overflow());
                                    break;
                                case DataType.TIME:
                                    ana.AddTest(column, new Test.TimeFormatTest());
                                    ana.AddTest(column, new Test.Overflow());
                                    break;
                                case DataType.INTEGER:
                                case DataType.SMALLINT:
                                case DataType.REAL:
                                case DataType.NUMERIC:
                                case DataType.DECIMAL:
                                    ana.AddTest(column, new Test.Overflow());
                                    break;
                            }
                        }
                    }

                    return ana;
                });

                if (Notifications.Count == 0)
                    SetStatus("Indlæsningen er fuldført.", LogLevel.SECTION);
                else
                    SetStatus($"Indlæsning er fuldført, med fejl. Fejlkategorier: {NotificationsCategoryView.Groups.Count}, antal fejl: {Notifications.Count}", LogLevel.ERROR);

                // Add analysis tasks
                if (PerformAnalysis)
                {
                    System.Action analysis = () => {
                        Analyzer.MoveNextTable();
                        Analyzer.InitializeTable();
                        taskTotalProgress.Report(Analyzer.TableRowCount);
                        taskProgress.Report(0);

                        bool readNext = false;
                        int chunk = 10000;
                        do
                        {
                            readNext = Analyzer.AnalyzeRows(chunk);
                            taskProgress.Report(Analyzer.TableDoneRows);
                            analysisProgress.Report(Analyzer.TotalDoneRows);
                        }
                        while (readNext);

                        foreach (var report in Analyzer.TestHierachy[Analyzer.CurrentTable].Values)
                        {
                            report.SuggestType(HandleNotification);
                        }
                    };

                    try
                    {
                        foreach (var table in av.Tables)
                        {
                            Tasks.Add(new TaskViewModel($"Analyse af {table.Name}", _ => analysis()));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InitializeAnalysisException("En fejl opstod under initialiseringen af data analysen.", ex);
                    }
                }

                // Add key test tasks
                if (PerformKeyTest)
                {
                    Action<ForeignKeyTest, object> foreignKeyTest = (keyTest, skipped) =>
                    {
                        int? tablesSkipped = skipped as int?;
                        if (!tablesSkipped.HasValue)
                            throw new InvalidOperationException("Task error");
                        bool readNext = false;

                        // move next for each task with no foreign keys
                        while (tablesSkipped > 0)
                        {
                            keyTest.MoveNextTable();
                            tablesSkipped--;
                        }

                        // move next for this task
                        keyTest.MoveNextTable();

                        keyTest.InitializeReferencedValueLoading();

                        taskTotalProgress.Report(keyTest.TableRowCount);
                        taskProgress.Report(0);

                        while (keyTest.MoveNextForeignKey())
                        {
                            do
                            {
                                readNext = keyTest.ReadReferencedForeignKeyValue();
                                taskProgress.Report(keyTest.TableDoneRows);
                                keyTestProgress.Report(keyTest.TotalDoneRows);
                            } while (readNext);
                        }

                        keyTest.InitializeTableTest();
                        do
                        {
                            readNext = keyTest.ReadForeignKeyValue();
                            taskProgress.Report(keyTest.TableDoneRows);
                            keyTestProgress.Report(keyTest.TotalDoneRows);
                        } while (readNext);
                    };

                    try
                    {
                        var keyTest = new ForeignKeyTest(av.Tables, HandleNotification);
                        ProgressKeyTestTotal = keyTest.TotalRowCount;
                        NotifyOfPropertyChange("ProgressKeyTestTotal");
                        int skippedTables = 0;
                        foreach (var table in av.Tables)
                        {
                            // Skip if no foreign keys.
                            if (table.ForeignKeys == null || table.ForeignKeys.Count == 0)
                            {
                                skippedTables++;
                                continue;
                            }

                            Tasks.Add(new TaskViewModel($"Fremmednøgletest af {table.Name}", skipped => foreignKeyTest(keyTest, skipped), skippedTables));

                            skippedTables = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("En fejl opstod under initialiseringen af fremmednøgletests. Alle tests er måske ikke tilføjet.");
                        //throw new InitializeForeignKeyTestException("En fejl opstod under intialiseringen af fremmednøgletesten.", ex);
                    }
                    
                }

                ArchiveVersion = av;
                Analyzer = analyzer;
            }
            catch (LoadArchiveIndexException ex)
            {
                SetStatus("Arkiv-indeks kunne ikke indlæses: " + ex.InnerException.Message, LogLevel.ERROR);
                ProgressState = TaskbarItemProgressState.Error;
                return;
            }
            catch (LoadTableIndexException ex)
            {
                string message = " ";
                Exception inner = ex.InnerException;
                while (inner != null)
                {
                    message = $"{inner.GetType()}: {inner.Message}";
                    inner = inner.InnerException;
                }
                SetStatus($"Tabel-indeks kunne ikke indlæses.{message}", LogLevel.ERROR);
                ProgressState = TaskbarItemProgressState.Error;
                return;
            }
            catch (InitializeForeignKeyTestException ex)
            {
                SetStatus($"{ex.Message}. {ex.InnerException.Message}", LogLevel.ERROR);
                ProgressState = TaskbarItemProgressState.Error;
                return;
            }
            catch (InitializeAnalysisException ex)
            {
                SetStatus($"{ex.Message}. {ex.InnerException.Message}", LogLevel.ERROR);
                ProgressState = TaskbarItemProgressState.Error;
                return;
            }
            catch (Exception ex)
            {
                SetStatus($"En undtagelse af typen '{ex.GetType()}' forekom under indlæsningen af arkiveringsversionen, med følgende besked: {ex.Message}", LogLevel.ERROR);
                MessageBox.Show(ex.StackTrace);
                ProgressState = TaskbarItemProgressState.Error;
                return;
            }
            finally
            {
                LoadingArchiveVersion = false;
                ProgressState = TaskbarItemProgressState.Normal;
            }

            var beginTime = DateTime.Now;

            foreach (var task in Tasks)
            {
                SetStatus($"Udfører {task.Name}.", LogLevel.SECTION);
                CurrentTask = task;
                NotifyOfPropertyChange("CurrentTask");
                await task.Run();
                SetStatus($"{task.Name} udført.", LogLevel.SECTION);
            }

            ProgressState = TaskbarItemProgressState.None;

            var elapsed = DateTime.Now - beginTime;

            SetStatus($"Test fuldført klokken {DateTime.Now.ToShortTimeString()} efter {elapsed.Hours} timer, {elapsed.Minutes} minutter og {elapsed.Seconds} sekunder.", LogLevel.SECTION);
        }

        public void SelectLocation()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Vælg roden af en arkiveringsversion, f. eks. AVID.KSA.1.1";
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadLocation(dialog.SelectedPath);
                }
            }
        }

        void AddRecentLocation(string location)
        {
            if (Properties.Settings.Default.RecentLocations == null)
            {
                Properties.Settings.Default.RecentLocations = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Save();
            }

            var removed = RecentLocations.FirstOrDefault(rl => rl.Location.Equals(location));
            if (removed != null)
            {
                Properties.Settings.Default.RecentLocations.Remove(removed.Location);
                RecentLocations.Remove(removed);
            }
                        RecentLocations.Insert(0, new RecentLocationViewModel(location, LoadLocation));
            Properties.Settings.Default.RecentLocations.Insert(0, location);
            while (RecentLocations.Count > 5)
            {
                Properties.Settings.Default.RecentLocations.RemoveAt(Properties.Settings.Default.RecentLocations.Count - 1);
                RecentLocations.RemoveAt(RecentLocations.Count - 1);
            }
            Properties.Settings.Default.Save();
        }

        public void AnalysisErrorSamplesSelectionChanged(SelectionChangedEventArgs eventArgs)
        {
            AnalysisErrorSamplesSelection.AddRange(eventArgs.AddedItems.Cast<Post>());
            foreach (var item in eventArgs.RemovedItems.Cast<Post>())
            {
                AnalysisErrorSamplesSelection.Remove(item);
            }
        }

        public void CopyAnalysisErrorSamples(bool headers, bool quoted)
        {
            string copy = string.Empty;
            if (headers)
            {
                if (quoted)
                    copy = "\"Række\"\t\"Længde\"\t\"Data\"\n";
                else
                    copy = "Række\tLængde\tData\n";
            }
            foreach (Post post in AnalysisErrorSamplesSelection)
            {
                if (quoted)
                    copy += $"\"{post.RowIndex + 1}\"\t\"{post.Data.Length}\"\t\"{post.Data}\"\n";
                else
                    copy += $"{post.RowIndex + 1}\t{post.Data.Length}\t{post.Data}\n";
            }
            Clipboard.SetText(copy);
        }

        public void ForeignKeyTestErrorSamplesSelectionChanged(SelectionChangedEventArgs eventArgs)
        {
            ForeignKeyTestErrorSamplesSelection.AddRange(eventArgs.AddedItems.Cast<System.Data.DataRowView>());
            foreach (var item in eventArgs.RemovedItems.Cast<System.Data.DataRowView>())
            {
                ForeignKeyTestErrorSamplesSelection.Remove(item);
            }
        }

        public void CopyForeignKeyTestErrorSamples(bool headers, bool quoted)
        {
            if (SelectedNotification.Type != NotificationType.ForeignKeyTestError)
                return;

            var foreignKey = SelectedNotification.ForeignKey;

            string copy = string.Empty;
            if (headers)
            {
                if (quoted)
                    copy = string.Join("\t", foreignKey.References.Select(refe => "\"" + refe.Column.ToString() + "\"")) + "\t\"Antal fejl\"\n";
                else
                    copy = string.Join("\t", foreignKey.References.Select(refe => refe.Column.ToString())) + "\tAntal fejl\n";
            }

            foreach (var rowView in ForeignKeyTestErrorSamplesSelection)
            {
                var items = new List<string>();
                int i;
                for (i = 0; i < foreignKey.References.Count; i++)
                {
                    var post = rowView[i] as Post;
                    if (quoted)
                        items.Add("\"" + post.Data + "\"");
                    else
                        items.Add(post.Data);
                }
                if (quoted)
                    items.Add("\"" + (rowView[i] as int?).Value.ToString() + "\"");
                else
                    items.Add((rowView[i] as int?).Value.ToString());
                copy += string.Join("\t", items) + "\n";
            }

            Clipboard.SetText(copy);
        }

        public void ExportTableIndex(bool onlySuggestionsWhereError)
        {
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.Filter = "Xml|*.xml|Alle filtyper|*.*";
                dialog.FileName = "tableIndex.xml";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SetStatus($"Eksporterer opdateret tabelindeks til '{dialog.FileName}'.");
                    using (var stream = dialog.OpenFile())
                    {
                        using (var xmlWriter = System.Xml.XmlWriter.Create(stream, new System.Xml.XmlWriterSettings() { Indent = true }))
                        {
                            TableIndex fromSuggestions = Analyzer.CreateTableIndexFromSuggestions(onlySuggestionsWhereError);
                            var xml = fromSuggestions.ToXml(false);
                            xml.WriteTo(xmlWriter);
                        }
                    }
                }
            }


        }

        public void Notifications_ExportHTML()
        {
            var now = DateTime.Now;
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.Filter = "Html|*.html|Alle filtyper|*.*";
                dialog.FileName = $"NEAAnalyzer_{ArchiveVersion.Id}_{now.ToString("yyyy-MM-dd_HH-mm-ss")}.html";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SetStatus($"Eksporterer {NotificationCount} ud af {Notifications.Count} notifikationer til '{dialog.FileName}'.");
                    using (var stream = dialog.OpenFile())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            var groupByTables = Notifications_SelectedGroupingIndex == 0;
                            IEnumerable<CollectionViewGroup> notificationGroups;
                            if (groupByTables)
                                notificationGroups = NotificationsView.Groups.Cast<CollectionViewGroup>();
                            else
                                notificationGroups = NotificationsCategoryView.Groups.Cast<CollectionViewGroup>();

                            var analysisSamples = new Dictionary<INotification, IEnumerable<Post>>();
                            foreach (var typedict in AnalysisNotificationsMap.Values)
                            {
                                foreach (var notificationvm in typedict.Values)
                                {
                                    analysisSamples.Add(notificationvm, notificationvm.Sample);
                                }
                            }

                            var fkeySamples = new Dictionary<ForeignKey, IEnumerable<Tuple<ForeignKeyValue, int>>>();
                            foreach (var notificationvm in ForeignKeyTestErrorNotificationsMap.Values)
                            {
                                fkeySamples.Add(notificationvm.ForeignKey,
                                    notificationvm.ErrorValues.ToList().Select(keyValue => new Tuple<ForeignKeyValue, int>(keyValue.Key, keyValue.Value)));
                            }
                            writer.Write(new Resources.NotificationLog(ArchiveVersion, analysisSamples, fkeySamples, notificationGroups, groupByTables).TransformText());
                        }
                    }
                }
            }
        }

        public void Exit()
        {
            Application.Current.Shutdown();
        }
        #endregion
    }
}
