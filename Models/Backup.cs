using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Views.Pages;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    class Backup
    {
        //TODO: remove defaults
        public ObservableCollection<FileSystemItem> BackupItems = new ObservableCollection<FileSystemItem>();
        public string DestinationPath = "C:\\Users\\User\\Documents";
        public BackupSetting BackupSetting = new BackupSetting();
        public string BackupName = "mock backup";
        //public string RestorePath;

        public void PrepareMetadata()
        {
            List<object> metadataList = new List<object>();

            foreach (FileSystemItem backupItem in BackupItems)
            {
                string type = backupItem.IsFolder ? "folder" : "file";
                string path = backupItem.Path;

                var metadataItem = new { Type = type, Path = path };
                metadataList.Add(metadataItem);

                if (backupItem.IsFolder)
                {
                    TraverseBackupItems(backupItem.Children, metadataList);
                }
            }

            string metadataJson = JsonSerializer.Serialize(metadataList, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            string metadataFilePath = Path.Combine(DestinationPath, BackupName + "_metadata.json");
            File.WriteAllText(metadataFilePath, metadataJson);
        }

        private void TraverseBackupItems(IEnumerable<FileSystemItem> items, List<object> metadataList)
        {
            foreach (FileSystemItem backupItem in items)
            {
                string type = backupItem.IsFolder ? "folder" : "file";
                string path = backupItem.Path;

                var metadataItem = new { Type = type, Path = path };
                metadataList.Add(metadataItem);

                if (backupItem.IsFolder)
                {
                    TraverseBackupItems(backupItem.Children, metadataList);
                }
            }
        }

        public void PerformFullBackup()
        {

            foreach (FileSystemItem backupItem in BackupItems)
            {
                string source = backupItem.Path;
                // Initialize the shadow copy subsystem.
                using (VssBackup vss = new VssBackup())
                {
                    vss.Setup(Path.GetPathRoot(source));
                    string snap_path = vss.GetSnapshotPath(source);
                    // Here we use the AlphaFS library to make the copy.
                    string destinationPath = Path.Combine(DestinationPath, Path.GetFileName(source));
                    if (backupItem.IsFolder)
                    {
                        Directory.Copy(snap_path, destinationPath);
                    }
                    else
                    {
                        File.Copy(snap_path, destinationPath);
                    }
                }

            }


        }



    }
}

