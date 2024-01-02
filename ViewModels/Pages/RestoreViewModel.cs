// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Wpf.Ui.Controls;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;
using System.Collections.ObjectModel;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using Back_It_Up.Views.Pages;
using GalaSoft.MvvmLight.Messaging;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class RestoreViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<BackupVersion> _backupVersions;

        [ObservableProperty]
        private BackupVersion _selectedVersion;

        [ObservableProperty]
        private ObservableCollection<FileSystemItem> _fileSystemItems;


        public ICommand CheckBoxCheckedCommand { get; set; }
        public ICommand CheckBoxUncheckedCommand { get; set; }
        public ICommand OpenDestinationExplorerCommand { get; }

        private readonly INavigationService _navigationService;

        public RestoreViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            CheckBoxCheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxChecked);
            CheckBoxUncheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxUnchecked);
            OpenDestinationExplorerCommand = new RelayCommand(OpenDestinationExplorer);
            // get backup store
            BackupStore store = App.GetService<BackupStore>();
            BackupVersions = store.SelectedBackup.BackupVersions;
        }



        private void OpenDestinationExplorer()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.CurrentContext = BackupStore.ExplorerContext.Restore;
            _navigationService.Navigate(typeof(DestinationExplorerPage));
        }

        [RelayCommand]
        private void OpenFindBackupExplorer()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.CurrentContext = BackupStore.ExplorerContext.Find;
            _navigationService.Navigate(typeof(DestinationExplorerPage));
        }

        [RelayCommand]
        private async void Restore()
        {
            BackupStore store = App.GetService<BackupStore>();
            await store.SelectedBackup.RestoreIncrementalBackup("restore", _selectedVersion);

        }

        private void CheckBoxChecked(FileSystemItem dataItem)
        {
            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.RestoreItems.Add(dataItem);
        }
        private void CheckBoxUnchecked(FileSystemItem dataItem)
        {
            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.RestoreItems.Remove(dataItem);
        }
        [RelayCommand]
        public void LoadContents(BackupVersion backupVersion)
        {
            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.LoadContents(_backupVersions[0]);
            _selectedVersion = backupVersion;
            FileSystemItems = store.SelectedBackup.BackupItems;
        }

    }



}
