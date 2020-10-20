using Caliburn.Micro;
using NEA.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEA.HardHorn.ViewModels
{
    public class ColumnTextStatisticsViewModel : Screen
    {
        public ColumnAnalysis ColumnAnalysis { get; private set; }
        public ColumnTextStatisticsViewModel(ColumnAnalysis columnAnalysis)
        {
            ColumnAnalysis = columnAnalysis;
        }
    }
}
