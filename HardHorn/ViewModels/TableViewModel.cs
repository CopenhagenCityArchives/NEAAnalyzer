﻿using Caliburn.Micro;
using HardHorn.Analysis;
using HardHorn.Archiving;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardHorn.ViewModels
{
    class BrowseRow
    {
        public List<Post> Posts { get; private set; }

        public BrowseRow(Post[] posts)
        {
            Posts = new List<Post>(posts);
        }
    }

    class TableViewModel : PropertyChangedBase
    {
        public Table Table { get; set; }

        int _browseOffset;
        public int BrowseOffset { get { return _browseOffset; } set { _browseOffset = value; NotifyOfPropertyChange("BrowseOffset"); } }
        int _browseCount;
        public int BrowseCount { get { return _browseCount; } set { _browseCount = value; NotifyOfPropertyChange("BrowseCount"); } }

        public ObservableCollection<BrowseRow> BrowseRows { get; set; }
        BackgroundWorker browseRowsWorker;

        ColumnAnalysis _selectedColumnAnalysis;
        public ColumnAnalysis SelectedColumnAnalysis { get { return _selectedColumnAnalysis; } set { _selectedColumnAnalysis = value; NotifyOfPropertyChange("SelectedColumnAnalysis"); } }

        public bool Errors
        {
            get { return _errors; }
            set { _errors = value; NotifyOfPropertyChange("Errors"); }
        }

        public bool Done
        {
            get { return _done; }
            set { _done = value; NotifyOfPropertyChange("Done"); }
        }

        public bool Busy
        {
            get { return _busy; }

            set { _busy = value; NotifyOfPropertyChange("Busy"); }
        }

        bool _busy = false;
        bool _errors = false;
        private bool _done = false;

        int _browseReadProgress = 0;
        public int BrowseReadProgress {  get { return _browseReadProgress; } set { _browseReadProgress = value;  NotifyOfPropertyChange("BrowseReadProgress"); } }
        bool _BrowseReady = true;
        public bool BrowseReady { get { return _BrowseReady; } set { _BrowseReady = value; NotifyOfPropertyChange("BrowseReady"); } }

        public TableViewModel(Table table)
        {
            Table = table;
            BrowseRows = new ObservableCollection<BrowseRow>();
            BrowseOffset = 0;
            BrowseCount = 20;
            browseRowsWorker = new BackgroundWorker();
            browseRowsWorker.DoWork += BrowseRowsWorker_DoWork;
            browseRowsWorker.RunWorkerCompleted += BrowseRowsWorker_RunWorkerCompleted;
            browseRowsWorker.WorkerReportsProgress = true;
            browseRowsWorker.ProgressChanged += BrowseRowsWorker_ProgressChanged;
            UpdateBrowseRows();
        }

        private void BrowseRowsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BrowseReadProgress = e.ProgressPercentage;
        }

        private void BrowseRowsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BrowseReady = true;
            BrowseRows.Clear();
            foreach (var row in e.Result as List<BrowseRow>)
                BrowseRows.Add(row);
        }

        private void BrowseRowsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            if (Table == null || BrowseOffset < 0 || BrowseCount < 0)
                return;

            // Local copies
            int browseOffset = BrowseOffset;
            int browseCount = BrowseCount;
            var browseRows = new List<BrowseRow>();

            int currentOffset = 0;
            int rowsRead = 0;
            Post[,] posts;
            using (var reader = Table.GetReader())
            {
                if (browseOffset > 0)
                {
                    int chunkSize = 50000;
                    int chunks = browseOffset / chunkSize;
                    int chunkExtra = browseOffset % chunkSize;

                    for (int c = 0; c < chunks; c++)
                    {
                        rowsRead = reader.Read(out posts, chunkSize);
                        currentOffset += rowsRead;
                        worker.ReportProgress((currentOffset * 100) / browseOffset);
                    }
                    rowsRead = reader.Read(out posts, chunkExtra);
                    currentOffset += rowsRead;
                    worker.ReportProgress((currentOffset * 100) / browseOffset);
                }
                rowsRead = reader.Read(out posts, browseCount);
                currentOffset += rowsRead;
            }

            for (int i = 0; i < rowsRead; i++)
            {
                var rowPosts = new Post[Table.Columns.Count];
                for (int j = 0; j < Table.Columns.Count; j++)
                {
                    rowPosts[j] = posts[i, j];
                }
                browseRows.Add(new BrowseRow(rowPosts));
            }

            e.Result = browseRows;
        }

        public void UpdateBrowseRows()
        {
            if (!BrowseReady)
                return;

            BrowseReady = false;
            BrowseReadProgress = 0;
            browseRowsWorker.RunWorkerAsync();
        }
    }
}
