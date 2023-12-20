using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Stores;
using Back_It_Up.Views.Pages;
using CommunityToolkit.Mvvm.Messaging;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.VisualBasic;
using Octodiff.Core;
using Octodiff.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Media.Animation;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Back_It_Up.Models
{
    class Backup
    {
        //TODO: remove defaults
        public ObservableCollection<FileSystemItem> BackupItems = new ObservableCollection<FileSystemItem>();
        public ObservableCollection<FileSystemItem> RestoreItems = new ObservableCollection<FileSystemItem>();
        public string DestinationPath = "C:\\Users\\User\\Documents\\backups";
        public string RestorePath = "C:\\Users\\User\\Documents\\restores";
        public BackupSetting BackupSetting = new BackupSetting();
        public string BackupName;
        public BackupVersion Version;

        public async Task PerformBackup()
        {
            if (DoesPreviousBackupExist())
            {
                await PerformIncrementalBackup();
            }
            else
            {
                await PerformFullBackup();
            }
        }


        public async Task PerformIncrementalBackup()
        {
            List<MetadataItem> res = await LoadPreviousBackupMetadata();
            List<FileSystemItem> changedFiles = await GetChangedFiles(res);
            if (!changedFiles.Any())
            {
                Console.WriteLine("No changes detected. Incremental backup is not required.");
                return;
            }

            BackupVersion version = await ReadManifestFileAsync();

            foreach (var file in changedFiles)
            {
                // extract backup file to temp and get it's path
                string tempFilePath = await ExtractBackupFileAndGetPath(file, version);
                string changedFilePath = file.Path;
                string patchFilePath = Path.Combine(DestinationPath, BackupName, "Contents", file.Name + ".octopatch"); // Path where the patch file will be saved

                await GeneratePatchUsingOctodiffAsync(tempFilePath, changedFilePath, patchFilePath);
            }

            await CreateIncrementalBackupZipAndCleanup(changedFiles);

        }

        public async Task CreateIncrementalBackupZipAndCleanup(List<FileSystemItem> changedFiles)
        {
            //here
            int version = await UpdateManifestFileAsync();
            await CreateIncrementalMetadata(changedFiles);
            await CreateIncrementalBackupZip(changedFiles, version);
            await CleanupAfterBackupAsync();
        }

        public async Task CreateIncrementalBackupZip(List<FileSystemItem> changedFiles, int version)
        {
            await Task.Run(() =>
            {
                string backupDestinationFolder = Path.Combine(DestinationPath, BackupName);
                string zipPath = Path.Combine(backupDestinationFolder, "v" + version + ".zip");

                using (ZipArchive zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    // Add patch files to the ZIP archive
                    foreach (var file in changedFiles)
                    {
                        string patchFilePath = Path.Combine(DestinationPath, BackupName, "Contents", file.Name + ".octopatch");
                        if (File.Exists(patchFilePath))
                        {
                            zipArchive.CreateEntryFromFile(patchFilePath, Path.GetFileName(patchFilePath));
                        }
                    }

                    // Add metadata file to the ZIP archive
                    string metadataFilePath = Path.Combine(DestinationPath, BackupName, "Contents", "metadata.json");
                    if (File.Exists(metadataFilePath))
                    {
                        zipArchive.CreateEntryFromFile(metadataFilePath, "metadata.json");
                    }
                }


            });
        }

        private async Task CleanupAfterBackupAsync()
        {
            string backupRootPath = Path.Combine(DestinationPath, BackupName);
            string contentsFolderPath = Path.Combine(backupRootPath, "Contents");
            string tempFolderPath = Path.Combine(backupRootPath, "temp");

            await Task.Run(() =>
            {
                if (Directory.Exists(contentsFolderPath))
                {
                    Directory.Delete(contentsFolderPath, true);
                }

                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
            });
        }


        public async Task<int> UpdateManifestFileAsync()
        {
            string manifestPath = Path.Combine(DestinationPath, BackupName, "manifest.json");
            List<BackupVersion> backupVersions;

            // Check if the manifest file exists and read it
            if (File.Exists(manifestPath))
            {
                string manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
                backupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();
            }
            else
            {
                backupVersions = new List<BackupVersion>();
            }

            // Calculate the next version number
            int nextVersionNumber = backupVersions.Any() ? backupVersions.Max(v => v.Version) + 1 : 1;

            // Add a new entry for the current backup version
            BackupVersion newVersion = new BackupVersion
            {
                Version = nextVersionNumber,
                DateCreated = DateTime.Now,
                BackupZipFilePath = Path.Combine(DestinationPath, BackupName, $"v{nextVersionNumber}.zip")
            };

            backupVersions.Add(newVersion);

            // Serialize and write the updated manifest back to the file
            string updatedManifestJson = JsonSerializer.Serialize(backupVersions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(manifestPath, updatedManifestJson);
            return nextVersionNumber;
        }


        public async Task GeneratePatchUsingOctodiffAsync(string originalFilePath, string modifiedFilePath, string patchFilePath)
        {
            // Ensure the output directory for the patch file exists
            var patchFileDirectory = Path.GetDirectoryName(patchFilePath);
            if (!Directory.Exists(patchFileDirectory))
            {
                Directory.CreateDirectory(patchFileDirectory);
            }

            // Create signature of the original file
            var signatureBuilder = new SignatureBuilder();
            using (var basisStream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureStream = new FileStream(patchFilePath + ".sig", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
            }

            // Generate patch
            var deltaBuilder = new DeltaBuilder();
            using (var newFileStream = new FileStream(modifiedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureFileStream = new FileStream(patchFilePath + ".sig", FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var deltaStream = new FileStream(patchFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureFileStream, new ConsoleProgressReporter()), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
            }

            // Clean up: delete the signature file
            File.Delete(patchFilePath + ".sig");
        }

        private async Task<string> ExtractBackupFileAndGetPath(FileSystemItem file, BackupVersion version)
        {
            string zipFilePath = version.BackupZipFilePath;
            string extractedFilePath = "";

            // Define a temporary directory within the backup directory
            string tempDirectoryPath = Path.Combine(DestinationPath, BackupName, "temp");
            if (!Directory.Exists(tempDirectoryPath))
            {
                Directory.CreateDirectory(tempDirectoryPath);
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                ZipArchiveEntry metadataEntry = archive.GetEntry("metadata.json");
                using (var reader = new StreamReader(metadataEntry.Open()))
                {
                    string metadataJson = await reader.ReadToEndAsync();
                    var metadataItems = JsonSerializer.Deserialize<List<MetadataItem>>(metadataJson);

                    var item = metadataItems.FirstOrDefault(m => m.Path == file.Path && m.Type == "file");
                    if (item != null)
                    {
                        string entryName = GetEntryNameFromMetadata(item);
                        var zipEntry = archive.GetEntry(entryName);

                        if (zipEntry != null)
                        {
                            string fullExtractPath = Path.Combine(tempDirectoryPath, Path.GetFileName(file.Path));

                            zipEntry.ExtractToFile(fullExtractPath, overwrite: true); // Extract the file
                            extractedFilePath = fullExtractPath; // Set the extracted file path
                        }
                    }
                }
            }

            return extractedFilePath; // Return the path of the extracted file
        }

        public async Task PerformFullBackup()
        {
            int version = CreateManifest();
            await CreateMetadata();
            await FullBackup();
            await CreateZipArchive(version);
            await WriteBackupLocation();

            Messenger.Default.Send(BackupName);
        }

        public async Task<BackupVersion> ReadManifestFileAsync()
        {
            string manifestPath = Path.Combine(DestinationPath, BackupName, "manifest.json");
            if (File.Exists(manifestPath))
            {
                string manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
                List<BackupVersion> BackupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();

                if (BackupVersions.Count > 0)
                {
                    return BackupVersions.Last();
                }
            }

            return null;
        }

        public async Task<List<FileSystemItem>> GetChangedFiles(List<MetadataItem> previousMetadata)
        {
            var changedFiles = new List<FileSystemItem>();
            await CheckAndAddChangedFiles(BackupItems, previousMetadata, changedFiles);
            return changedFiles;
        }

        private async Task CheckAndAddChangedFiles(IEnumerable<FileSystemItem> items, List<MetadataItem> previousMetadata, List<FileSystemItem> changedFiles)
        {
            foreach (var item in items)
            {
                var previousItem = previousMetadata.FirstOrDefault(pm => pm.Path == item.Path);
                if (previousItem == null || (await HasFileChangedAsync(item, previousItem)))
                {
                    changedFiles.Add(item);
                }

                // Recursively check children if it's a folder
                if (item.IsFolder)
                {
                    await CheckAndAddChangedFiles(item.Children, previousMetadata, changedFiles);
                }
            }
        }

        private async Task<bool> HasFileChangedAsync(FileSystemItem currentItem, MetadataItem previousItem)
        {
            if (currentItem.IsFolder)
            {
                // Folders are handled recursively; skip processing here.
                return false;
            }

            // Compare checksums for files
            string currentChecksum = await CalculateFileChecksum(currentItem.Path);
            return currentChecksum != previousItem.Checksum;
        }

        public async Task<string> CalculateFileChecksum(string filePath)
        {
            using (VssBackup vss = new VssBackup())
            {
                vss.Setup(Path.GetPathRoot(filePath));
                string snap_path = vss.GetSnapshotPath(filePath);
                using (var md5 = MD5.Create())
                {
                    // Open the file asynchronously with AlphaFS
                    using (var stream = File.Open(snap_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                    {
                        // Read and compute the hash asynchronously
                        var hash = await md5.ComputeHashAsync(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
        }

        public async Task<List<MetadataItem>> LoadPreviousBackupMetadata()
        {

            if (string.IsNullOrEmpty(BackupName))
            {
                return new List<MetadataItem>();
            }

            string manifestPath = Path.Combine(DestinationPath, BackupName, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return new List<MetadataItem>();
            }

            string manifestJson = File.ReadAllText(manifestPath);
            var backupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();
            var latestBackupVersion = backupVersions.OrderByDescending(v => v.DateCreated).FirstOrDefault();

            if (latestBackupVersion == null)
            {
                return new List<MetadataItem>();
            }

            string metadataJson = await ReadMetadataFromZip(latestBackupVersion.BackupZipFilePath);
            return JsonSerializer.Deserialize<List<MetadataItem>>(metadataJson) ?? new List<MetadataItem>();
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


        public bool DoesPreviousBackupExist()
        {
            string metadataFilePath = Path.Combine(DestinationPath, BackupName, "manifest.json");
            return File.Exists(metadataFilePath);
        }

        public async Task PerformRestore()
        {
            string zipFilePath = Version.BackupZipFilePath; // Assuming Version is defined and holds the correct data

            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                var metadataEntry = archive.GetEntry("metadata.json");

                using (var reader = new StreamReader(metadataEntry.Open()))
                {
                    string metadataJson = await reader.ReadToEndAsync();
                    var metadataItems = JsonSerializer.Deserialize<List<MetadataItem>>(metadataJson);

                    foreach (var item in metadataItems)
                    {
                        if (item.Type == "file")
                        {
                            string entryName = GetEntryNameFromMetadata(item);
                            var zipEntry = archive.GetEntry(entryName);

                            if (zipEntry != null)
                            {
                                string fullRestorePath = Path.Combine(RestorePath, entryName);
                                string directoryPath = Path.GetDirectoryName(fullRestorePath);

                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }

                                zipEntry.ExtractToFile(fullRestorePath, true); // Extract the file
                            }
                        }
                    }
                }
            }
        }
        private string GetEntryNameFromMetadata(MetadataItem item)
        {
            // Assuming the RootPath is the path to the root of the backup
            string backupRoot = Path.GetDirectoryName(item.RootPath);

            if (!string.IsNullOrEmpty(backupRoot))
            {
                // Remove the backup root part from the item's path
                string relativePath = item.Path.Substring(backupRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                return relativePath.Replace("\\", "/"); // Convert to ZIP format (forward slashes)
            }

            return string.Empty;
        }


        public int CreateManifest()
        {
            string destinationFolder = Path.Combine(DestinationPath, BackupName);
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string manifestPath = Path.Combine(destinationFolder, "manifest.json");
            int version = 1;

            List<BackupVersion> backupVersions = new List<BackupVersion>();

            // Check if the manifest file already exists. If it does, read and deserialize its content.
            if (!File.Exists(manifestPath))
            {
                backupVersions.Add(new BackupVersion()
                {
                    Version = 1,
                    DateCreated = DateTime.Now,
                    BackupZipFilePath = Path.Combine(destinationFolder, "v1.zip")
                });

                string manifestJson = JsonSerializer.Serialize(backupVersions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(manifestPath, manifestJson);
            }
            else if (File.Exists(manifestPath))
            {
                string existingManifestJson = File.ReadAllText(manifestPath);
                backupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(existingManifestJson) ?? new List<BackupVersion>();
                version = backupVersions.Last().Version + 1;

                backupVersions.Add(new BackupVersion()
                {
                    Version = version,
                    DateCreated = DateTime.Now,
                    BackupZipFilePath = Path.Combine(destinationFolder, "v" + version + ".zip")
                });

                string manifestJson = JsonSerializer.Serialize(backupVersions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(manifestPath, manifestJson);


            }

            return version;
        }

        public async Task CreateIncrementalMetadata(List<FileSystemItem> changedFiles)
        {
            List<MetadataItem> metadataList = new List<MetadataItem>();

            foreach (FileSystemItem changedFile in changedFiles)
            {
                await AddItemAndChildrenToMetadata(changedFile, metadataList, changedFile.Path);
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
                        string backupNameFolderPath = Path.Combine(DestinationPath, BackupName, "Contents");
                        string metadataFilePath = Path.Combine(backupNameFolderPath, "metadata.json");
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


        public async Task CreateMetadata()
        {
            List<MetadataItem> metadataList = new List<MetadataItem>();

            foreach (FileSystemItem backupItem in BackupItems)
            {
                await AddItemAndChildrenToMetadata(backupItem, metadataList, backupItem.Path);
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
                        string backupNameFolderPath = Path.Combine(DestinationPath, "Contents");
                        string metadataFilePath = Path.Combine(backupNameFolderPath, "metadata.json");
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


        private async Task AddItemAndChildrenToMetadata(FileSystemItem item, List<MetadataItem> metadataList, string rootPath)
        {
            var checksum = item.IsFolder ? "" : await CalculateFileChecksum(item.Path); // Calculate checksum for files
            string type = item.IsFolder ? "folder" : "file";
            metadataList.Add(new MetadataItem
            {
                Type = type,
                Path = item.Path,
                RootPath = rootPath,
                Checksum = checksum
            });

            if (item.IsFolder)
            {
                foreach (var child in item.Children)
                {
                    await AddItemAndChildrenToMetadata(child, metadataList, rootPath);
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

        public async Task FullBackup()
        {

            foreach (FileSystemItem backupItem in BackupItems)
            {
                string source = backupItem.Path;
                // Initialize the shadow copy subsystem.
                using (VssBackup vss = new VssBackup())
                {
                    vss.Setup(Path.GetPathRoot(source));
                    string snap_path = vss.GetSnapshotPath(source);
                    string backupNameFolderPath = Path.Combine(DestinationPath, "Contents");
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

        public async Task CreateZipArchive(int version)
        {

            string backupDestinationFolder = Path.Combine(DestinationPath, BackupName);

            string zipPath = Path.Combine(backupDestinationFolder, "v" + version + ".zip");
            string sourcePath = Path.Combine(DestinationPath, "Contents");
            string manifestPath = Path.Combine(DestinationPath, "manifest.json");
            ZipFile.CreateFromDirectory(sourcePath, zipPath, CompressionLevel.Fastest, false);
            using KernelTransaction kernelTransaction = new KernelTransaction();
            {
                try
                {
                    await Task.Run(() =>
                    {
                        if (Directory.ExistsTransacted(kernelTransaction, sourcePath))
                        {
                            Directory.DeleteTransacted(kernelTransaction, sourcePath, true);
                            kernelTransaction.Commit();
                        }
                    });
                }
                catch (Exception)
                {
                    kernelTransaction.Rollback();
                }
            }
        }


        public async Task WriteBackupLocation()
        {
            using KernelTransaction kernelTransaction = new KernelTransaction();
            {
                try
                {
                    await Task.Run(() =>
                    {
                        string zipPath = Path.Combine(DestinationPath, BackupName + ".zip");
                        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp");
                        string destinationFilePath = Path.Combine(appDataFolder, "backup_locations.txt");

                        Directory.CreateDirectoryTransacted(kernelTransaction, appDataFolder);
                        Directory.CreateDirectoryTransacted(kernelTransaction, Path.GetDirectoryName(destinationFilePath));

                        if (!File.ExistsTransacted(kernelTransaction, destinationFilePath))
                        {
                            File.WriteAllTextTransacted(kernelTransaction, destinationFilePath, zipPath);
                        }
                        else
                        {
                            string existingContent = File.ReadAllTextTransacted(kernelTransaction, destinationFilePath);
                            if (!existingContent.Contains(zipPath))
                                File.AppendAllTextTransacted(kernelTransaction, destinationFilePath, Environment.NewLine + zipPath);
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

