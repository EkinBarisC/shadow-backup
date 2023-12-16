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

namespace Back_It_Up.ViewModels.Pages
{
    public partial class RestoreViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<BackupVersion> _backupVersions;

        [ObservableProperty]
        private ObservableCollection<FileSystemItem> _fileSystemItems;

        public ICommand LoadContentsCommand { get; }

        public ICommand CheckBoxCheckedCommand { get; set; }
        public ICommand CheckBoxUncheckedCommand { get; set; }
        public ICommand RestoreCommand { get; }
        public ICommand OpenDestinationExplorerCommand { get; }

        private readonly INavigationService _navigationService;

        public RestoreViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            LoadContentsCommand = new RelayCommand<BackupVersion>(LoadContents);
            CheckBoxCheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxChecked);
            CheckBoxUncheckedCommand = new RelayCommand<FileSystemItem>(CheckBoxUnchecked);
            OpenDestinationExplorerCommand = new RelayCommand(OpenDestinationExplorer);
            RestoreCommand = new RelayCommand(Restore);
            readManifestFile();
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


        private async void LoadContents(BackupVersion backupVersion)
        {

            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.Version = backupVersion;
            string zipFilePath = backupVersion.BackupZipFilePath;
            string metadata = await ReadMetadataFromZip(zipFilePath);
            FileSystemItems = CreateFileSystemItemsFromJson(metadata);

        }
        private FileSystemItem FindItemByPath(ObservableCollection<FileSystemItem> items, string path)
        {
            foreach (var item in items)
            {
                if (item.Path == path)
                {
                    return item;
                }

                var foundItem = FindItemByPath(item.Children, path);
                if (foundItem != null)
                {
                    return foundItem;
                }
            }

            return null;
        }

        public ObservableCollection<FileSystemItem> CreateFileSystemItemsFromJson(string json)
        {
            var backupItems = JsonSerializer.Deserialize<List<BackupItem>>(json);
            var fileSystemItems = new ObservableCollection<FileSystemItem>();

            if (backupItems == null)
                return fileSystemItems;

            // Process the backup items
            foreach (var item in backupItems)
            {
                if (item.Path != null && Path.GetFileName(item.Path) != "metadata.json")
                {
                    var fileSystemItem = new FileSystemItem
                    {
                        Path = item.Path,
                        Name = System.IO.Path.GetFileName(item.Path),
                        IsFolder = item.Type == "folder",
                        // Other properties can be set as needed
                    };

                    // Add to the parent's children or directly to the fileSystemItems
                    var parentDir = System.IO.Path.GetDirectoryName(item.Path);
                    var parentItem = FindItemByPath(fileSystemItems, parentDir);
                    if (parentItem != null)
                    {
                        parentItem.Children.Add(fileSystemItem);
                    }
                    else
                    {
                        fileSystemItems.Add(fileSystemItem);
                    }
                }
            }

            return fileSystemItems;
        }

        public async Task<string> ReadMetadataFromZip(string zipFilePath)
        {
            // Ensure the file exists
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException("ZIP file not found.", zipFilePath);
            }

            // Open the ZIP file
            using (var zipArchive = ZipFile.OpenRead(zipFilePath))
            {
                // Find the metadata file
                var metadataEntry = zipArchive.GetEntry("metadata.json");
                if (metadataEntry == null)
                {
                    throw new FileNotFoundException("Metadata file not found in the ZIP archive.", "metadata.json");
                }

                // Read the metadata file
                using (var stream = metadataEntry.Open())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        return Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                }
            }
        }


        public void readManifestFile()
        {
            BackupStore store = App.GetService<BackupStore>();
            string backupName = store.SelectedBackup.BackupName;

            string manifestPath = Path.Combine(store.SelectedBackup.DestinationPath, backupName, "manifest.json");
            string manifestJson = File.ReadAllText(manifestPath);
            BackupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();
        }


    }

    public class BackupItem
    {
        public string Type { get; set; }
        public string Path { get; set; }
    }

}
