

using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<object> _topMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Backup",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.BackupPage)
            },
            new NavigationViewItem()
            {
                Content = "Restore",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.RestorePage)
            },
            new NavigationViewItem()
            {
                Content = "Logs",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.LogsPage)
            },
        };
    }
}
