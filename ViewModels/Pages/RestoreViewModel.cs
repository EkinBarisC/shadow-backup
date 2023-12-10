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

namespace Back_It_Up.ViewModels.Pages
{
    public partial class RestoreViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<BackupVersion> _backupVersions;

        [ObservableProperty]
        private ObservableCollection<FileSystemItem> _fileSystemItems;

        public ICommand LoadContentsCommand { get; }
        public RestoreViewModel()
        {
            LoadContentsCommand = new RelayCommand<BackupVersion>(LoadContents);
            readManifestFile();
        }

        private async void LoadContents(BackupVersion backupVersion)
        {

            string zipFilePath = backupVersion.BackupZipFilePath;
            string metadata = await ReadMetadataFromZip(zipFilePath);
            FileSystemItems = CreateFileSystemItemsFromJson(metadata);

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
                var fileSystemItem = new FileSystemItem
                {
                    Path = item.Path,
                    Name = System.IO.Path.GetFileName(item.Path),
                    IsFolder = item.Type == "folder",
                    // Other properties can be set as needed
                };

                // Add to the parent's children or directly to the fileSystemItems
                var parentDir = System.IO.Path.GetDirectoryName(item.Path);
                var parentItem = fileSystemItems.FirstOrDefault(fi => fi.Path == parentDir);
                if (parentItem != null)
                {
                    parentItem.Children.Add(fileSystemItem);
                }
                else
                {
                    fileSystemItems.Add(fileSystemItem);
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
            string backupName = store.selectedBackup.BackupName;

            string manifestPath = Path.Combine(store.selectedBackup.DestinationPath, backupName, "manifest.json");
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
