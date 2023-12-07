﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.Views.Pages;
using Back_It_Up.Views.UserControls;
using MimeTypes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class DestinationExplorerViewModel : ObservableObject
    {
        public ObservableCollection<FileSystemItem> fileSystemItems { get; set; }

        public ICommand ReturnToSourcePageCommand { get; set; }
        public ICommand CheckBoxCheckedCommand { get; set; }
        public ICommand CheckBoxUncheckedCommand { get; set; }

        private readonly INavigationService _navigationService;

        public DestinationExplorerViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            this.ReturnToSourcePageCommand = new RelayCommand(ReturnToSourcePage);
            this.CheckBoxCheckedCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<FileSystemItem>(CheckBox_Checked);
            this.CheckBoxUncheckedCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<FileSystemItem>(CheckBox_Unchecked);

            fileSystemItems = new ObservableCollection<FileSystemItem>();
            LoadFileSystemItems("C:\\Users\\User");
        }

        private void LoadFileSystemItems(string path)
        {
            try
            {
                var rootItems = Directory.GetFileSystemEntries(path).Select(item =>
                {
                    try
                    {
                        string mimeType = MimeTypeMap.GetMimeType(System.IO.Path.GetExtension(item));
                        return new FileSystemItem
                        {
                            Name = Path.GetFileName(item),
                            Path = item,
                            IsFolder = Directory.Exists(item),
                            IsExpanded = false,
                            FileType = Directory.Exists(item) ? "file folder" : mimeType.Substring(0, mimeType.IndexOf('/')),
                            FileSizeInBytes = Directory.Exists(item) ? 0 : new FileInfo(item).Length
                        };
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return null;
                    }
                }).Where(item => item != null);

                fileSystemItems.Clear();
                foreach (var item in rootItems)
                {

                    if (item.IsFolder)
                    {
                        fileSystemItems.Add(item);
                        item.AddDummyChild();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }


        public void CheckBox_Checked(FileSystemItem dataItem)
        {
            dataItem.IsExpanded = true;

            foreach (var item in fileSystemItems)
            {
                if (item != dataItem)
                {
                    item.IsSelected = false;
                }
            }
            BackupStore store = App.GetService<BackupStore>();

            store.selectedBackup.DestinationPath = dataItem.Path;
        }

        public void CheckBox_Unchecked(FileSystemItem dataItem)
        {
            dataItem.SetIsSelectedRecursively(false);

            BackupStore store = App.GetService<BackupStore>();
            if (store.selectedBackup.DestinationPath == dataItem.Path)
            {
                store.selectedBackup.DestinationPath = null;
            }
        }

        private void ReturnToSourcePage()
        {

            BackupStore store = App.GetService<BackupStore>();
            _navigationService.Navigate(typeof(BackupPage));
        }
    }
}