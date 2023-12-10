// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using System.Text.Json;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class RestoreViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<BackupVersion> _backupVersions;
        public RestoreViewModel()
        {
            readManifestFile();
        }

        public void readManifestFile()
        {
            BackupStore store = App.GetService<BackupStore>();
            string backupName = store.selectedBackup.BackupName;
            System.Diagnostics.Debug.WriteLine(backupName);

            string manifestPath = Path.Combine(store.selectedBackup.DestinationPath, backupName, "manifest.json");

            string manifestJson = File.ReadAllText(manifestPath);
            BackupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();

        }


    }
}
