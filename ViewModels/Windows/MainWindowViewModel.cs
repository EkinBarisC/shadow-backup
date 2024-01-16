
using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Stores;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "Back It Up";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new ObservableCollection<object>();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };

        public MainWindowViewModel()
        {
            MenuItems = LoadBackupLocations();
        }

        public ObservableCollection<object> LoadBackupLocations()
        {

            BackupStore store = App.GetService<BackupStore>();
            string[] backupPathsArray = store.SelectedBackup.LoadBackupLocationsFromFile();
            List<string> backupNames = backupPathsArray.Select(path => Path.GetFileNameWithoutExtension(path)).ToList();
            List<string> backupLocations = backupNames.ToList();

            ObservableCollection<object> backupList = new ObservableCollection<object>();

            backupList.Add(new NavigationViewItem()
            {
                Content = "Add New Backup",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Add24 },
                Tag = "add_new_backup",
                TargetPageType = typeof(Views.Pages.DashboardPage)
            });

            foreach (var location in backupLocations)
            {
                backupList.Add(new NavigationViewItem()
                {
                    Content = location,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Document24 },
                    Tag = "backup_" + location,
                    TargetPageType = typeof(Views.Pages.DashboardPage)
                });
            }
            return backupList;

        }




    }
}
