// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
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
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Add New Backup",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Add24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            }
        };

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
            LoadBackupLocations();
        }


        private void LoadBackupLocations()
        {
            // Load backup locations from the file or any other storage mechanism
            List<string> backupLocations = LoadBackupLocationsFromFile();

            foreach (var location in backupLocations)
            {
                MenuItems.Add(new NavigationViewItem()
                {
                    Content = location,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Document24 }, // Adjust the icon as needed
                    Tag = location, // You can use Tag to store additional information, like the backup location
                    TargetPageType = typeof(Views.Pages.DashboardPage)
                });
            }
        }

        private List<string> LoadBackupLocationsFromFile()
        {

            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp");
            string backupLocationPath = Path.Combine(appDataFolder, "backup_locations.txt");

            using KernelTransaction kernelTransaction = new KernelTransaction();
            string backupPaths = File.ReadAllTextTransacted(kernelTransaction, backupLocationPath);
            string[] backupPathsArray = backupPaths.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<string> backupNames = backupPathsArray.Select(path => Path.GetFileNameWithoutExtension(path)).ToList();

            return backupNames.ToList();

        }

    }
}
