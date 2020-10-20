using System.Windows;
using Caliburn.Micro;
using System.Windows.Markup;
using System.Globalization;

namespace NEA.Analyzer
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            DisplayRootViewFor<ViewModels.SimpleViewModel>();
        }
    }
}
