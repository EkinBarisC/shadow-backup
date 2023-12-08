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
using System.Transactions;

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

        public void PerformBackup()
        {
            //CreateMetadata();
            //PerformFullBackup();
            CreateZipArchive();
        }

        public async void CreateMetadata()
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

            using KernelTransaction kernelTransaction = new KernelTransaction();
            {
                try
                {
                    await Task.Run(() =>
                    {
                        string backupNameFolderPath = Path.Combine(DestinationPath, BackupName);
                        string metadataFilePath = Path.Combine(backupNameFolderPath, BackupName + "_metadata.json");
                        Directory.CreateDirectoryTransacted(kernelTransaction, backupNameFolderPath);
                        File.WriteAllTextTransacted(kernelTransaction, metadataFilePath, metadataJson);

                        kernelTransaction.Commit();
                    });
                }
                catch (Exception)
                {
                    kernelTransaction.Rollback();
                }
            }
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

        public async void FullBackup()
        {

            foreach (FileSystemItem backupItem in BackupItems)
            {
                string source = backupItem.Path;
                // Initialize the shadow copy subsystem.
                using (VssBackup vss = new VssBackup())
                {
                    vss.Setup(Path.GetPathRoot(source));
                    string snap_path = vss.GetSnapshotPath(source);
                    string backupNameFolderPath = Path.Combine(DestinationPath, BackupName);
                    string destinationPath = Path.Combine(backupNameFolderPath, Path.GetFileName(source));

                    using KernelTransaction kernelTransaction = new KernelTransaction();
                    {
                        try
                        {
                            await Task.Run(() =>
                            {
                                Directory.CreateDirectoryTransacted(kernelTransaction, backupNameFolderPath);

                                if (backupItem.IsFolder)
                                {
                                    //Directory.Copy(snap_path, destinationPath);
                                    Directory.CopyTransacted(kernelTransaction, snap_path, destinationPath);
                                }
                                else
                                {
                                    //File.Copy(snap_path, destinationPath);
                                    File.CopyTransacted(kernelTransaction, snap_path, destinationPath);
                                }


                                kernelTransaction.Commit();
                            });
                        }
                        catch (Exception)
                        {
                            kernelTransaction.Rollback();
                        }
                    }

                }

            }
        }

        public void CreateZipArchive()
        {
            string zipPath = Path.Combine(DestinationPath, BackupName + ".zip");
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                foreach (FileSystemItem backupItem in BackupItems)
                {
                    string source = Path.Combine(DestinationPath, backupItem.Name);
                    archive.CreateEntryFromFile(source, zipPath, CompressionLevel.Fastest);
                    ZipFile.CreateFromDirectory
                }

            }
        }


    }
}

