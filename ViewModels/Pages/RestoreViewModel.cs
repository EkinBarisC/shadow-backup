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
        private ObservableCollection<FileSystemItem> _fileSystemItems;


        public ICommand CheckBoxCheckedCommand { get; set; }
        public ICommand CheckBoxUncheckedCommand { get; set; }
        public ICommand RestoreCommand { get; }
        public ICommand OpenDestinationExplorerCommand { get; }

        private readonly INavigationService _navigationService;

        public RestoreViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            CheckBoxCheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxChecked);
            CheckBoxUncheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxUnchecked);
            OpenDestinationExplorerCommand = new RelayCommand(OpenDestinationExplorer);
            RestoreCommand = new RelayCommand(Restore);
        }



        private void OpenDestinationExplorer()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.CurrentContext = BackupStore.ExplorerContext.Restore;
            _navigationService.Navigate(typeof(DestinationExplorerPage));
        }
        private async void Restore()
        {
            BackupStore store = App.GetService<BackupStore>();
            await store.SelectedBackup.PerformRestore();
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





    }



}
