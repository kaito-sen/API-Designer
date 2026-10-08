using System.Windows;
using API_Integarated.UI.Views;

namespace API_Integarated.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = new ApiStudioWindow();
            mainWindow.Show();
        }
    }
}
