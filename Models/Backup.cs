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
        public string DestinationPath = "C:\\Users\\ekin1\\OneDrive\\Documents\\backup tool files\\backups";
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
                if (backupItem.IsFolder)
                {
                    // Perform backup for folder
                    BackupFolder(backupItem.Path, DestinationPath);
                }
                else
                {
                    // Perform backup for file
                    BackupFile(backupItem.Path, DestinationPath);
                }
            }

          
        }

        private void BackupFolder(string sourceFolder, string destinationFolder)
        {
            try
            {
                // Initialize the shadow copy subsystem.
                using (VssBackup vss = new VssBackup())
                {
                    vss.Setup(Path.GetPathRoot(sourceFolder));
                    string snap_path = vss.GetSnapshotPath(sourceFolder);
                    // Here we use the AlphaFS library to make the copy.
                    string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(sourceFolder));
                    Directory.Copy(snap_path, destinationPath);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void BackupFile(string sourceFile, string destinationFolder)
        {
                // Initialize the shadow copy subsystem.
                using (VssBackup vss = new VssBackup())
                {
                    vss.Setup(Path.GetPathRoot(sourceFile));
                    string snap_path = vss.GetSnapshotPath(sourceFile);
                    // Here we use the AlphaFS library to make the copy.
                    string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));
                    File.Copy(snap_path, destinationPath);
                }
        }



    }
}

            //string source_file = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\texts\text 1.txt";
            //string backup_root = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\backups";
            //string backup_path = Path.Combine(backup_root, Path.GetFileName(source_file));

//string[] filesToBeZipped = { xmlPath, backup_path };

//string zipPath = Path.Combine(backup_root, backupName + ".zip");
//using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
//{
//    archive.CreateEntryFromFile(source_file, Path.GetFileName(source_file), CompressionLevel.Fastest);
//    archive.ExtractToDirectory(backup_path, true);
//}
