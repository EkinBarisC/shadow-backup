﻿
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

        [ObservableProperty]
        private string currentPath;

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

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            LoadFileSystemItems(path);
        }

        public void ClearCheckedItems()
        {
            foreach (var item in fileSystemItems)
            {
                UncheckItemAndChildren(item);
            }
        }

        private void UncheckItemAndChildren(FileSystemItem item)
        {
            item.IsSelected = false;
            foreach (var child in item.Children)
            {
                UncheckItemAndChildren(child);
            }
        }

        [RelayCommand]
        private void NavigateToParentDirectory()
        {
            if (string.IsNullOrEmpty(CurrentPath))
            {
                return;
            }

            var parentDir = Directory.GetParent(CurrentPath);
            if (parentDir != null)
            {
                LoadFileSystemItems(parentDir.FullName);
                CurrentPath = parentDir.FullName;
            }
        }
        private void LoadFileSystemItems(string path)
        {
            try
            {
                CurrentPath = path;
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
                            FileSizeInBytes = Directory.Exists(item) ? 0 : new FileInfo(item).Length,
                            Parent = null
                        };
                    }
                    catch (Exception)
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


        public async void CheckBox_Checked(FileSystemItem dataItem)
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
            if (store.CurrentContext == BackupStore.ExplorerContext.Restore)
            {
                store.SelectedBackup.RestorePath = dataItem.Path;
            }
            else if (store.CurrentContext == BackupStore.ExplorerContext.Backup)
            {
                store.SelectedBackup.DestinationPath = dataItem.Path;
            }
            else if (store.CurrentContext == BackupStore.ExplorerContext.Find)
            {
                store.SelectedBackup.DestinationPath = Path.GetDirectoryName(dataItem.Path);
                store.SelectedBackup.BackupName = Path.GetFileNameWithoutExtension(dataItem.Path);
                await store.SelectedBackup.WriteBackupLocation();
            }

        }

        public void CheckBox_Unchecked(FileSystemItem dataItem)
        {
            dataItem.SetIsSelectedRecursively(false);

            BackupStore store = App.GetService<BackupStore>();

            if (store.CurrentContext == BackupStore.ExplorerContext.Restore)
            {
                if (store.SelectedBackup.RestorePath == dataItem.Path)
                {
                    store.SelectedBackup.RestorePath = null;
                }
            }

            else if (store.CurrentContext == BackupStore.ExplorerContext.Backup)
            {
                if (store.SelectedBackup.DestinationPath == dataItem.Path)
                {
                    store.SelectedBackup.DestinationPath = null;
                }
            }

        }

        private void ReturnToSourcePage()
        {
            _navigationService.GoBack();
        }
    }
}
