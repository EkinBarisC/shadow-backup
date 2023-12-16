// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Vss;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Windows;
using Back_It_Up.Views.Pages;
using Back_It_Up.Views.UserControls;
using Back_It_Up.Views.Windows;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class SourceViewModel : ObservableObject
    {

        public ICommand OpenSourceExplorerCommand { get; set; }
        public ICommand OpenDestinationExplorerCommand { get; set; }
        private readonly INavigationService _navigationService;
        public ICommand PerformBackupCommand { get; set; }

        public SourceViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            this.OpenSourceExplorerCommand = new RelayCommand(OpenSourceExplorer);
            this.OpenDestinationExplorerCommand = new RelayCommand(OpenDestinationExplorer);
            this.PerformBackupCommand = new RelayCommand(PerformBackup);
        }

        private void OpenSourceExplorer()
        {
            _navigationService.Navigate(typeof(SourceExplorerPage));
        }
        private void OpenDestinationExplorer()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.CurrentContext = BackupStore.ExplorerContext.Backup;
            _navigationService.Navigate(typeof(DestinationExplorerPage));
        }

        private void PerformBackup()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.PerformBackup();
        }



    }
}
