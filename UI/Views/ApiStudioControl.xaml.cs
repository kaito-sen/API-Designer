using System.Windows;
using System.Windows.Controls;
using API_Integarated.Core.Models;
using API_Integarated.UI.ViewModels;

namespace API_Integarated.UI.Views
{
    public partial class ApiStudioControl : UserControl
    {
        public ApiStudioControl()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                DataContext = new ApiStudioViewModel();
            }
        }

        private EndpointItemViewModel? GetEndpoint(object sender) =>
            ((sender as FrameworkElement)?.DataContext as EndpointItemViewModel) ??
            (DataContext as ApiStudioViewModel)?.SelectedEndpoint;

        private void OnBodyTypeNoneChecked(object sender, RoutedEventArgs e)
        {
            var ep = GetEndpoint(sender);
            if (ep != null) ep.BodyType = ApiBodyType.None;
        }

        private void OnBodyTypeJsonChecked(object sender, RoutedEventArgs e)
        {
            var ep = GetEndpoint(sender);
            if (ep != null) ep.BodyType = ApiBodyType.Json;
        }

        private void OnBodyTypeFormUrlChecked(object sender, RoutedEventArgs e)
        {
            var ep = GetEndpoint(sender);
            if (ep != null) ep.BodyType = ApiBodyType.FormUrlEncoded;
        }

        private void OnBodyTypeFormDataChecked(object sender, RoutedEventArgs e)
        {
            var ep = GetEndpoint(sender);
            if (ep != null) ep.BodyType = ApiBodyType.FormData;
        }

        private void OnBodyTypeRawChecked(object sender, RoutedEventArgs e)
        {
            var ep = GetEndpoint(sender);
            if (ep != null) ep.BodyType = ApiBodyType.RawText;
        }
    }
}
