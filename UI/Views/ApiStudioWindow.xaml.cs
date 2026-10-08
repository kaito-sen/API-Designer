using System.Windows;
using API_Integarated.UI.ViewModels;

namespace API_Integarated.UI.Views
{
    public partial class ApiStudioWindow : Window
    {
        public ApiStudioViewModel ViewModel => (ApiStudioViewModel)StudioControl.DataContext;

        public ApiStudioWindow()
        {
            InitializeComponent();
        }

        public ApiStudioWindow(ApiStudioViewModel viewModel) : this()
        {
            StudioControl.DataContext = viewModel;
        }
    }
}
