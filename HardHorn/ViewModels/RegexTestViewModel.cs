using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NEA.Archiving;
using NEA.Analysis;

namespace NEA.HardHorn.ViewModels
{
    class RegexTestViewModel
    {
        public Test.Pattern RegexTest { get; private set; }
        public Column Column { get; private set; }

        public RegexTestViewModel(Test.Pattern regexTest, Column column)
        {
            Column = column;
            RegexTest = regexTest;
        }
    }
}
