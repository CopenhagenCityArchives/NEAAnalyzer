using Caliburn.Micro;

using System;

namespace NEA.Analyzer.ViewModels
{
    public class RecentLocationViewModel : PropertyChangedBase
    {
        public string Location { get; private set; }
        Action<string> LoadLocationMethod;

        public RecentLocationViewModel(string location, Action<string> loadMethod)
        {
            Location = location;
            LoadLocationMethod = loadMethod;
        }

        public void Load()
        {
            LoadLocationMethod(Location);
        }
    }
}
