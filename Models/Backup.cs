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
using Task = System.Threading.Tasks.Task;
using Microsoft.Win32.TaskScheduler;
using Trigger = Microsoft.Win32.TaskScheduler.Trigger;
using Serilog;

namespace Back_It_Up.Models
{
    class Backup
    {
        public ObservableCollection<FileSystemItem> BackupItems = new ObservableCollection<FileSystemItem>();
        public ObservableCollection<FileSystemItem> RestoreItems = new ObservableCollection<FileSystemItem>();
        public string DestinationPath;
        public string RestorePath;
        public BackupSetting BackupSetting = new BackupSetting();
        public string BackupName;
        public List<BackupVersion> BackupVersions;
        public BackupVersion Version;

        public async Task DeleteBackup()
        {
            // 1. Delete backup contents from the destination path
            try
            {
                string backupFolderPath = Path.Combine(DestinationPath, BackupName);
                if (Directory.Exists(backupFolderPath))
                {
                    Directory.Delete(backupFolderPath, recursive: true);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them or inform the user)
            }

            // 2. Remove the backup entry from backup_locations.txt
            try
            {
                string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp");
                string backupLocationsFilePath = Path.Combine(appDataFolder, "backup_locations.txt");

                if (File.Exists(backupLocationsFilePath))
                {
                    var allLines = await System.IO.File.ReadAllLinesAsync(backupLocationsFilePath);
                    var updatedLines = allLines.Where(line => !line.Trim().Equals(Path.Combine(DestinationPath, BackupName), StringComparison.OrdinalIgnoreCase)).ToList();

                    await System.IO.File.WriteAllLinesAsync(backupLocationsFilePath, updatedLines);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
        }


        public async Task PerformScheduledBackup(string backupName)
        {
            BackupName = backupName;
            LoadBackup();
            await PerformBackup();
        }

        public void CreateBackupTask(string taskName, DateTime startTime, int frequency, string frequencyType)
        {
            using (TaskService ts = new TaskService())
            {
                string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                string arguments = $"-s \"{BackupName}\"";

                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Perform scheduled backup";
                td.Principal.RunLevel = TaskRunLevel.Highest;  // Run with the highest privileges

                Trigger trigger;

                switch (frequencyType)
                {
                    case "Minutes":
                    case "Hours":
                        // Use DailyTrigger with a repetition pattern
                        var dailyTrigger = new DailyTrigger { DaysInterval = 1 };
                        dailyTrigger.StartBoundary = startTime;
                        TimeSpan repeatInterval = frequencyType == "Minutes" ?
                                                  TimeSpan.FromMinutes(frequency) :
                                                  TimeSpan.FromHours(frequency);
                        dailyTrigger.Repetition.Interval = repeatInterval;
                        trigger = dailyTrigger;
                        break;
                    case "Days":
                        trigger = new DailyTrigger { DaysInterval = (short)frequency, StartBoundary = startTime };
                        break;
                    case "Weeks":
                        trigger = new WeeklyTrigger { WeeksInterval = (short)frequency, StartBoundary = startTime };
                        break;
                    case "Years":
                        // For yearly, consider using a MonthlyTrigger and adjust accordingly
                        trigger = new MonthlyTrigger(); // Adjust for yearly scheduling
                        trigger.StartBoundary = startTime;
                        break;
                    default:
                        throw new ArgumentException("Invalid frequency type.");
                }

                td.Triggers.Add(trigger);

                // Create an action that will launch the application
                td.Actions.Add(new ExecAction(executablePath, arguments, null));

                // Register the task in the root folder of the Task Scheduler
                ts.RootFolder.RegisterTaskDefinition(taskName, td);
            }
        }



        public async Task PerformBackup()
        {
            Log.Information($"Backup started for '{BackupName}'");

            try
            {
                LoadBackup();
                switch (BackupSetting.SelectedBackupMethod)
                {
                    case BackupMethod.Full:
                        if (BackupSetting.SelectedCleaningOption == CleaningOption.CleanUpOldBackups && BackupSetting.DaysToKeepBackups.HasValue)
                        {
                            await DeleteBackupsOlderThan(BackupSetting.DaysToKeepBackups.Value);
                        }
                        await PerformFullBackup();
                        break;

                    case BackupMethod.Incremental:
                        if (DoesPreviousBackupExist())
                        {
                            // Check if it's time for a periodic full backup
                            if (BackupSetting.SelectedBackupScheme == BackupScheme.PeriodicFullBackup &&
                                ShouldPerformPeriodicFullBackup())
                            {
                                // Perform a full backup
                                await CleanUpOldBackups();
                                await PerformFullBackup();
                            }
                            else
                            {
                                // Perform an incremental backup
                                await RestoreIncrementalBackup("backup", Version);
                                await PerformIncrementalBackup();
                            }
                        }
                        else
                        {
                            await PerformFullBackup();
                        }
                        break;

                    default:
                        Log.Warning($"Unknown backup method: {BackupSetting.SelectedBackupMethod}");
                        throw new InvalidOperationException("Unknown backup method.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An error occurred during PerformBackup: {ex.Message}");
                throw;
            }
            Log.Information($"Backup completed successfully for '{BackupName}'");
        }




        private async Task DeleteBackupsOlderThan(int days)
        {
            string backupFolderPath = Path.Combine(DestinationPath, BackupName);

            // Load the current manifest
            List<BackupVersion> currentVersions = await ReadManifestFileAsync();

            // Calculate the threshold date
            DateTime thresholdDate = DateTime.Now.AddDays(-days);

            // Identify backups older than the threshold
            var oldBackups = currentVersions.Where(version => version.DateCreated < thresholdDate);

            // Delete the old backup ZIP files and remove them from the manifest
            foreach (var backup in oldBackups.ToList())
            {
                string zipPath = Path.Combine(backupFolderPath, backup.BackupZipFilePath);
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                currentVersions.Remove(backup);
            }

            // Save the updated manifest
            string manifestJson = JsonSerializer.Serialize(currentVersions, new JsonSerializerOptions { WriteIndented = true });
            string manifestPath = Path.Combine(backupFolderPath, "manifest.json");
            await System.IO.File.WriteAllTextAsync(manifestPath, manifestJson);
        }


        private bool ShouldPerformPeriodicFullBackup()
        {
            // Calculate the number of incremental backups since the last full backup
            int incrementalCount = BackupVersions.Count();

            // Check if the number of incremental backups has reached the frequency limit
            return BackupSetting.FullBackupFrequency.HasValue &&
                   incrementalCount >= BackupSetting.FullBackupFrequency.Value;
        }

        private async Task CleanUpOldBackups()
        {
            string backupFolderPath = Path.Combine(DestinationPath, BackupName);


            foreach (var backup in BackupVersions.ToList())
            {
                string zipPath = Path.Combine(backupFolderPath, backup.BackupZipFilePath);
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                BackupVersions.Remove(backup);
            }

            string manifestPath = Path.Combine(backupFolderPath, "manifest.json");
            System.IO.File.Delete(manifestPath);
        }



        public async Task PerformIncrementalBackup()
        {
            List<MetadataItem> res = await LoadPreviousBackupMetadata("latest");
            List<FileSystemItem> changedFiles = await GetChangedFiles(res, Path.Combine(DestinationPath, BackupName, "Contents"));
            if (!changedFiles.Any())
            {
                Console.WriteLine("No changes detected. Incremental backup is not required.");
                string contentsDirectoryPath = Path.Combine(DestinationPath, BackupName, "Contents");
                if (Directory.Exists(contentsDirectoryPath))
                    Directory.Delete(Path.Combine(DestinationPath, BackupName, "Contents"), true);
                return;
            }

            List<BackupVersion> versions = await ReadManifestFileAsync();
            BackupVersion version = versions.Last();

            foreach (var file in changedFiles)
            {
                string restorePath = Path.Combine(DestinationPath, BackupName, "Contents");
                string tempFilePath = await GetHierarchicalPathAsync(restorePath, file.Name);

                string changedFilePath = file.Path;
                string patchFilePath = Path.Combine(DestinationPath, BackupName, "Contents", file.Name + ".octopatch"); // Path where the patch file will be saved

                await GeneratePatchUsingOctodiffAsync(tempFilePath, changedFilePath, patchFilePath);
            }

            await CreateIncrementalBackupZipAndCleanup(changedFiles);

        }

        public async Task RestoreBackup(string reason, BackupVersion selectedVersion)
        {
            try
            {
                Log.Information($"Started restore process for '{BackupName}'");

                // Determine the restore directory based on the reason
                string extractPath = Path.Combine(DestinationPath, BackupName, "Contents");

                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                // Check the backup method of the selected version
                switch (BackupVersions[0].BackupSetting.SelectedBackupMethod)
                {
                    case BackupMethod.Full:
                        // Handle full backup restore
                        string zipFilePath = selectedVersion.BackupZipFilePath;
                        if (reason == "restore")
                            await ExtractSelectedFilesFromZip(Version, extractPath, RestoreItems);
                        else
                            ExtractZipFileToDirectory(zipFilePath, extractPath);

                        break;

                    case BackupMethod.Incremental:
                        // Handle incremental backup restore
                        foreach (var version in BackupVersions)
                        {
                            zipFilePath = version.BackupZipFilePath;

                            if (version == BackupVersions.First())
                            {
                                if (reason == "restore")
                                    await ExtractSelectedFilesFromZip(version, extractPath, RestoreItems);
                                else
                                    ExtractZipFileToDirectory(zipFilePath, extractPath);
                            }
                            else
                            {
                                if (reason == "restore")
                                    await ApplySelectedPatchesFromIncrementalBackup(zipFilePath, extractPath, RestoreItems);
                                else
                                    await ApplyPatchesFromIncrementalBackup(zipFilePath, extractPath);
                            }

                            if (version.Version == selectedVersion.Version)
                            {
                                break;
                            }
                        }
                        break;

                    default:
                        Log.Warning($"Unknown backup method: {selectedVersion.BackupSetting.SelectedBackupMethod}");
                        throw new InvalidOperationException("Unknown backup method.");
                }

                if (!string.IsNullOrEmpty(RestorePath))
                {
                    // Move files to the specified restore path
                    MoveFilesToDirectory(extractPath, RestorePath);
                }
                else
                {
                    // Restore files to their original location
                    foreach (var item in RestoreItems)
                    {
                        string sourcePath = Path.Combine(extractPath, item.Name);
                        string destinationPath = item.Path;
                        MoveFileOrDirectory(sourcePath, destinationPath);
                    }
                }

                Log.Information($"Restore completed successfully for '{BackupName}'");

            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during RestoreBackup: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private void MoveFileOrDirectory(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                // Move file
                System.IO.File.Move(sourcePath, destinationPath, true);
            }
            else if (Directory.Exists(sourcePath))
            {
                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Move each file and subdirectory
                foreach (var file in Directory.GetFiles(sourcePath))
                {
                    var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                    System.IO.File.Move(file, destFile, true);
                }
                foreach (var dir in Directory.GetDirectories(sourcePath))
                {
                    var destDir = Path.Combine(destinationPath, Path.GetFileName(dir));
                    MoveFileOrDirectory(dir, destDir);
                }

                // Delete the source directory after moving its contents
                Directory.Delete(sourcePath, true);
            }
        }


        private void MoveFilesToDirectory(string sourceDir, string destDir)
        {
            // Create the destination directory if it doesn't exist
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Move files from source to destination
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFilePath = Path.Combine(destDir, Path.GetFileName(file));
                System.IO.File.Move(file, destFilePath, overwrite: true);
            }

            // Recursively move subdirectories
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string destSubDirPath = Path.Combine(destDir, Path.GetFileName(directory));
                if (!Directory.Exists(destSubDirPath))
                {
                    Directory.Move(directory, destSubDirPath);
                }
                else
                {
                    // If the subdirectory already exists at the destination, move its contents
                    MoveFilesToDirectory(directory, destSubDirPath);
                }
            }

            // Optionally, delete the source directory if it's now empty
            if (!Directory.EnumerateFileSystemEntries(sourceDir).Any())
            {
                Directory.Delete(sourceDir);
            }
        }


        public async Task RestoreIncrementalBackup(string reason, BackupVersion selectedVersion)
        {
            string restoreDirectory;
            if (reason == "restore")
                restoreDirectory = Path.Combine(RestorePath, BackupName);
            else
                restoreDirectory = Path.Combine(DestinationPath, BackupName, "Contents");

            if (!Directory.Exists(restoreDirectory))
            {
                Directory.CreateDirectory(restoreDirectory);
            }

            foreach (var version in BackupVersions)
            {
                string zipFilePath = version.BackupZipFilePath;

                if (version == BackupVersions.First())
                {
                    if (reason == "restore")
                        await ExtractSelectedFilesFromZip(version, restoreDirectory, RestoreItems);
                    else
                        ExtractZipFileToDirectory(zipFilePath, restoreDirectory);
                }
                else
                {
                    if (reason == "restore")
                        await ApplySelectedPatchesFromIncrementalBackup(zipFilePath, restoreDirectory, RestoreItems);
                    else
                        await ApplyPatchesFromIncrementalBackup(zipFilePath, restoreDirectory);
                }

                if (version.Version == selectedVersion.Version)
                {
                    break;
                }
            }
        }

        private async Task ExtractSelectedFilesFromZip(BackupVersion version, string extractPath, ObservableCollection<FileSystemItem> restoreItems)
        {
            using (ZipArchive archive = ZipFile.OpenRead(version.BackupZipFilePath))
            {
                // Create a dictionary to quickly find entries by their paths
                var entryDictionary = archive.Entries.ToDictionary(e => e.FullName.Replace("/", "\\"), e => e);

                foreach (var item in restoreItems)
                {
                    string relativePath = await GetRelativePathAsync(version, item.Name);
                    if (entryDictionary.TryGetValue(relativePath, out ZipArchiveEntry entry))
                    {
                        // Entry found, extract it
                        string completeFilePath = Path.Combine(extractPath, relativePath);
                        string directoryPath = Path.GetDirectoryName(completeFilePath);

                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        entry.ExtractToFile(completeFilePath, true);
                    }
                }
            }
        }



        private async Task ApplySelectedPatchesFromIncrementalBackup(string zipFilePath, string restoreDirectory, ObservableCollection<FileSystemItem> restoreItems)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry patchEntry in archive.Entries)
                {
                    if (patchEntry.FullName == "metadata.json")
                    {
                        // Skip metadata file
                        continue;
                    }

                    string originalFileName = Path.GetFileNameWithoutExtension(patchEntry.FullName);
                    // Check if the original file is in the restore items list
                    if (!restoreItems.Any(item => item.Name.Equals(originalFileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Skip if not in the restore list
                    }

                    string denemeFilePath = await GetHierarchicalPathAsync(restoreDirectory, originalFileName);
                    string patchFilePath = Path.Combine(restoreDirectory, patchEntry.FullName);

                    // Extract the patch file temporarily
                    patchEntry.ExtractToFile(patchFilePath, overwrite: true);

                    // Apply the patch
                    ApplyPatchToFile(denemeFilePath, patchFilePath);

                    // Delete the extracted patch file after applying
                    File.Delete(patchFilePath);
                }
            }
        }



        private void ExtractZipFileToDirectory(string zipFilePath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string completeFilePath = Path.Combine(extractPath, entry.FullName);
                    string directoryPath = Path.GetDirectoryName(completeFilePath);

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    entry.ExtractToFile(completeFilePath, true);
                }
            }
        }
        private async Task ApplyPatchesFromIncrementalBackup(string zipFilePath, string restoreDirectory)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry patchEntry in archive.Entries)
                {
                    if (patchEntry.FullName == "metadata.json")
                    {
                        // Skip metadata file
                        continue;
                    }
                    string originalFileName = Path.GetFileNameWithoutExtension(patchEntry.FullName);
                    string originalFilePath = Path.Combine(restoreDirectory, originalFileName);

                    string denemeFilePath = await GetHierarchicalPathAsync(restoreDirectory, originalFileName);

                    string patchFilePath = Path.Combine(restoreDirectory, patchEntry.FullName);

                    // Extract the patch file temporarily
                    patchEntry.ExtractToFile(patchFilePath, overwrite: true);

                    // Apply the patch
                    ApplyPatchToFile(denemeFilePath, patchFilePath);

                    // Delete the extracted patch file after applying
                    File.Delete(patchFilePath);
                }
            }
        }

        private async Task<string> GetRelativePathAsync(BackupVersion version, string fileName)
        {

            string metadata = await ReadMetadataFromZip(version.BackupZipFilePath);
            List<MetadataItem> fullMetadata = JsonSerializer.Deserialize<List<MetadataItem>>(metadata) ?? new List<MetadataItem>();

            var fileMetadata = fullMetadata.FirstOrDefault(m => Path.GetFileName(m.Path) == fileName && m.Type == "file");
            if (fileMetadata == null)
            {
                return Path.Combine(fileName);
            }

            string hierarchicalPath = BuildHierarchicalPath(fileMetadata.Path, fileMetadata.RootPath, fullMetadata);
            return hierarchicalPath;
        }

        private async Task<string> GetHierarchicalPathAsync(string restoreDirectory, string fileName)
        {
            // Load the metadata for all backups, or just the necessary ones.

            foreach (var version in BackupVersions) // Assuming each version has a Date property
            {

                string metadataJson = await ReadMetadataFromZip(version.BackupZipFilePath);
                List<MetadataItem> metadata = JsonSerializer.Deserialize<List<MetadataItem>>(metadataJson) ?? new List<MetadataItem>();

                //here
                var fileMetadata = metadata.FirstOrDefault(m => Path.GetFileName(m.Path) == fileName && m.Type == "file");

                if (fileMetadata != null)
                {
                    string hierarchicalPath = BuildHierarchicalPath(fileMetadata.Path, fileMetadata.RootPath, metadata);
                    return Path.Combine(restoreDirectory, hierarchicalPath);
                }
            }

            // If the file wasn't found in any backup, fallback to the default behavior.
            return Path.Combine(restoreDirectory, fileName);
        }


        //private async Task<string> GetHierarchicalPathAsync(string restoreDirectory, string fileName)
        //{
        //    List<MetadataItem> fullMetadata = await LoadPreviousBackupMetadata("first");
        //    var fileMetadata = fullMetadata.FirstOrDefault(m => Path.GetFileName(m.Path) == fileName && m.Type == "file");
        //    if (fileMetadata == null)
        //    {
        //        return Path.Combine(restoreDirectory, fileName);
        //    }

        //    string hierarchicalPath = BuildHierarchicalPath(fileMetadata.Path, fileMetadata.RootPath, fullMetadata);
        //    return Path.Combine(restoreDirectory, hierarchicalPath, fileName);
        //}

        //private string BuildHierarchicalPath(string currentPath, string rootPath, List<MetadataItem> metadata)
        //{
        //    if (currentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return "";
        //    }

        //    var parentItem = metadata
        //        .Where(m => currentPath.StartsWith(m.Path + "\\", StringComparison.OrdinalIgnoreCase) && m.Type == "folder")
        //        .OrderByDescending(m => m.Path.Length)
        //        .FirstOrDefault();

        //    if (parentItem == null || parentItem.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return Path.GetFileName(Path.GetDirectoryName(currentPath));
        //    }

        //    string parentPath = BuildHierarchicalPath(parentItem.Path, rootPath, metadata);
        //    string currentRelativePath = GetRelativePathFromRoot(parentItem.Path, parentPath);

        //    return currentRelativePath;
        //}

        private string BuildHierarchicalPath(string currentPath, string rootPath, List<MetadataItem> metadata)
        {
            var formattedRootPath = rootPath.TrimEnd('\\');
            var formattedCurrentPath = currentPath.TrimEnd('\\');

            if (formattedCurrentPath.Equals(formattedRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileName(formattedRootPath); // Return the last part of rootPath
            }

            if (formattedCurrentPath.StartsWith(formattedRootPath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = formattedCurrentPath.Substring(formattedRootPath.Length).TrimStart('\\');
                string trimmedRootPath = Path.GetFileName(formattedRootPath);
                string result = Path.Combine(trimmedRootPath, relativePath); // Include last part of rootPath
                return result;
            }

            var parentItem = metadata
                .Where(m => formattedCurrentPath.StartsWith(m.Path + "\\", StringComparison.OrdinalIgnoreCase) && m.Type == "folder")
                .OrderByDescending(m => m.Path.Length) // Ensure the closest parent is selected
                .FirstOrDefault();

            if (parentItem == null)
            {
                return Path.GetFileName(formattedCurrentPath); // Return just the file/folder name if no parent found
            }

            string parentPath = BuildHierarchicalPath(parentItem.Path, formattedRootPath, metadata);
            string currentRelativePath = parentItem.Path.Equals(formattedRootPath, StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(formattedCurrentPath)
                : Path.Combine(parentPath, Path.GetFileName(formattedCurrentPath));

            return currentRelativePath;
        }



        private string GetRelativePathFromRoot(string fullPath, string rootFolderName)
        {
            int rootIndex = fullPath.IndexOf(rootFolderName, StringComparison.OrdinalIgnoreCase);
            if (rootIndex == -1)
            {
                throw new ArgumentException("Root folder not found in the provided path.");
            }

            string relativePath = fullPath.Substring(rootIndex);

            return relativePath;
        }


        private void ApplyPatchToFile(string originalFilePath, string patchFilePath)
        {
            // Ensure the directory for the new file exists
            string directoryPath = Path.GetDirectoryName(originalFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // The file path for the new file generated after applying the patch
            string newFilePath = originalFilePath + ".new";

            using (FileStream basisStream = new FileStream(originalFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            using (FileStream deltaStream = new FileStream(patchFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream newFileStream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var deltaApplier = new DeltaApplier { SkipHashCheck = true };
                deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, new ConsoleProgressReporter()), newFileStream);
            }

            // Replace the original file with the new file
            File.Delete(originalFilePath);
            File.Move(newFilePath, originalFilePath);
        }




        public async Task CreateIncrementalBackupZipAndCleanup(List<FileSystemItem> changedFiles)
        {

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
            using (var basisStream = new FileStream(originalFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
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


        public async Task PerformFullBackup()
        {
            int version = await CreateManifest();
            await CreateMetadata();
            await FullBackup();

            await CreateZipArchive(version);
            await WriteBackupLocation();
        }

        public async Task<List<BackupVersion>> ReadManifestFileAsync()
        {
            string manifestPath = Path.Combine(DestinationPath, BackupName, "manifest.json");
            if (File.Exists(manifestPath))
            {
                string manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
                List<BackupVersion> BackupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();

                if (BackupVersions.Count > 0)
                {
                    return BackupVersions;
                }
            }

            return null;
        }

        public async Task<List<FileSystemItem>> GetChangedFiles(List<MetadataItem> previousMetadata, string restoredBackupDirectory)
        {
            var changedFiles = new List<FileSystemItem>();
            await CheckAndAddChangedFiles(BackupItems, previousMetadata, changedFiles, restoredBackupDirectory);
            return changedFiles;
        }

        private async Task CheckAndAddChangedFiles(IEnumerable<FileSystemItem> items, List<MetadataItem> previousMetadata, List<FileSystemItem> changedFiles, string restoredBackupDirectory)
        {
            foreach (var item in items)
            {
                var previousItem = previousMetadata.FirstOrDefault(pm => pm.Path == item.Path);
                if (previousItem == null || await HasFileChangedAsync(item, previousItem, restoredBackupDirectory))
                {
                    if (!item.IsFolder) changedFiles.Add(item);
                }

                if (item.IsFolder)
                {
                    await CheckAndAddChangedFiles(item.Children, previousMetadata, changedFiles, restoredBackupDirectory);
                }
            }
        }

        private async Task<bool> HasFileChangedAsync(FileSystemItem currentItem, MetadataItem previousItem, string restoredBackupDirectory)
        {
            if (currentItem.IsFolder)
            {
                return false;
            }

            string restoredFilePath = Path.Combine(restoredBackupDirectory, currentItem.Path);
            string currentChecksum = await CalculateFileChecksum(restoredFilePath);
            return currentChecksum != previousItem.Checksum;
        }


        public async Task<string> CalculateFileChecksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                // Open the file asynchronously with AlphaFS
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous))
                {
                    // Read and compute the hash asynchronously
                    var hash = await md5.ComputeHashAsync(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

        }

        public async Task<List<MetadataItem>> LoadPreviousBackupMetadata(string which)
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
            BackupVersion latestBackupVersion;
            if (which == "latest")
                latestBackupVersion = backupVersions.OrderByDescending(v => v.DateCreated).FirstOrDefault();
            else
                latestBackupVersion = backupVersions.OrderBy(v => v.DateCreated).FirstOrDefault();

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


        private string GetEntryNameFromMetadata(MetadataItem item)
        {
            string backupRoot = Path.GetDirectoryName(item.RootPath);

            if (!string.IsNullOrEmpty(backupRoot))
            {
                string relativePath = item.Path.Substring(backupRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                return relativePath.Replace("\\", "/");
            }

            return string.Empty;
        }


        public async Task<int> CreateManifest()
        {
            string destinationFolder = Path.Combine(DestinationPath, BackupName);
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string manifestPath = Path.Combine(destinationFolder, "manifest.json");
            List<BackupVersion> backupVersions;

            if (File.Exists(manifestPath))
            {
                string existingManifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
                backupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(existingManifestJson) ?? new List<BackupVersion>();
            }
            else
            {
                backupVersions = new List<BackupVersion>();
            }

            int newVersionNumber = backupVersions.Any() ? backupVersions.Max(v => v.Version) + 1 : 1;
            BackupVersion newVersion = new BackupVersion()
            {
                Version = newVersionNumber,
                DateCreated = DateTime.Now,
                BackupZipFilePath = Path.Combine(destinationFolder, $"v{newVersionNumber}.zip"),
                BackupSetting = BackupSetting
            };

            backupVersions.Add(newVersion);

            string manifestJson = JsonSerializer.Serialize(backupVersions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(manifestPath, manifestJson);

            return newVersionNumber;
        }

        public async Task<string> FindRootPathForChangedItem(string changedItemPath)
        {
            // Load metadata from the first backup
            List<MetadataItem> firstBackupMetadata = await LoadPreviousBackupMetadata("first");

            // Find the root path for the changed item
            foreach (var metadataItem in firstBackupMetadata)
            {
                if (changedItemPath.StartsWith(metadataItem.Path, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the root path if a matching path is found
                    return metadataItem.RootPath;
                }
            }

            // If no matching root path is found, return null or an appropriate default
            return null;
        }
        public async Task CreateIncrementalMetadata(List<FileSystemItem> changedFiles)
        {
            List<MetadataItem> metadataList = new List<MetadataItem>();

            foreach (FileSystemItem changedFile in changedFiles)
            {
                await AddItemAndChildrenToMetadata(changedFile, metadataList, await FindRootPathForChangedItem(changedFile.Path));
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

            string backupNameFolderPath = Path.Combine(DestinationPath, "Contents");
            string metadataFilePath = Path.Combine(backupNameFolderPath, "metadata.json");

            try
            {
                Directory.CreateDirectory(backupNameFolderPath);
                await System.IO.File.WriteAllTextAsync(metadataFilePath, metadataJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating metadata: {ex.Message}");
            }
        }



        private async Task AddItemAndChildrenToMetadata(FileSystemItem item, List<MetadataItem> metadataList, string rootPath)
        {
            var checksum = item.IsFolder ? "" : await CalculateFileChecksum(item.Path);
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
                using (VssBackup vss = new VssBackup())
                {
                    await vss.Setup(Path.GetPathRoot(source));
                    string snap_path = vss.GetSnapshotPath(source);
                    string backupNameFolderPath = Path.Combine(DestinationPath, "Contents");
                    string destinationPath = Path.Combine(backupNameFolderPath, Path.GetFileName(source));

                    try
                    {
                        if (backupItem.IsFolder)
                        {
                            await CopyDirectoryAsync(snap_path, destinationPath);
                        }
                        else
                        {
                            await CopyFileAsync(snap_path, destinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during backup of '{source}': {ex.Message}");
                    }
                }
            }
        }


        private async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 128 * 1024, useAsync: true))
            using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 128 * 1024, useAsync: true))
            {
                await sourceStream.CopyToAsync(destStream);
            }
        }
        private async Task CopyDirectoryAsync(string sourceDirPath, string destDirPath)
        {
            Directory.CreateDirectory(destDirPath);

            var files = Directory.GetFiles(sourceDirPath);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFilePath = Path.Combine(destDirPath, fileName);
                await CopyFileAsync(file, destFilePath);
            }

            var subdirectories = Directory.GetDirectories(sourceDirPath);
            foreach (var subdirectory in subdirectories)
            {
                string subdirectoryName = Path.GetFileName(subdirectory);
                string destSubdirectoryPath = Path.Combine(destDirPath, subdirectoryName);
                await CopyDirectoryAsync(subdirectory, destSubdirectoryPath);
            }
        }




        public async Task CreateZipArchive(int version)
        {

            string backupDestinationFolder = Path.Combine(DestinationPath, BackupName);

            string zipPath = Path.Combine(backupDestinationFolder, "v" + version + ".zip");
            string sourcePath = Path.Combine(DestinationPath, "Contents");
            string manifestPath = Path.Combine(DestinationPath, "manifest.json");
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(sourcePath, zipPath, CompressionLevel.Fastest, false);
            });
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
                        string zipPath = Path.Combine(DestinationPath, BackupName);
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

        public string[] LoadBackupLocationsFromFile()
        {

            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp");
            string backupLocationPath = Path.Combine(appDataFolder, "backup_locations.txt");

            using KernelTransaction kernelTransaction = new KernelTransaction();
            string backupPaths = File.ReadAllTextTransacted(kernelTransaction, backupLocationPath);
            string[] backupPathsArray = backupPaths.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            return backupPathsArray;
        }



        public ObservableCollection<FileSystemItem> CreateFileSystemItemsFromJson(string json)
        {
            var backupItems = JsonSerializer.Deserialize<List<BackupItem>>(json);
            var fileSystemItems = new ObservableCollection<FileSystemItem>();

            if (backupItems == null)
                return fileSystemItems;

            foreach (var item in backupItems)
            {
                if (item.Path != null && Path.GetFileName(item.Path) != "metadata.json")
                {
                    var fileSystemItem = new FileSystemItem
                    {
                        Path = item.Path,
                        Name = System.IO.Path.GetFileName(item.Path),
                        IsFolder = item.Type == "folder",
                    };

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

            //UpdateFolderChildrenRecursively(fileSystemItems);

            return fileSystemItems;
        }

        private void UpdateFolderChildrenRecursively(ObservableCollection<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                if (item.IsFolder)
                {
                    UpdateFolderChildren(item);
                    UpdateFolderChildrenRecursively(item.Children);
                }
            }
        }

        private void UpdateFolderChildren(FileSystemItem folderItem)
        {
            if (Directory.Exists(folderItem.Path))
            {
                var directoryInfo = new Alphaleonis.Win32.Filesystem.DirectoryInfo(folderItem.Path);
                foreach (var fileInfo in directoryInfo.GetFiles())
                {
                    if (!folderItem.Children.Any(child => child.Path == fileInfo.FullName))
                    {
                        folderItem.Children.Add(new FileSystemItem
                        {
                            Path = fileInfo.FullName,
                            Name = fileInfo.Name,
                            IsFolder = false
                        });
                    }
                }
                foreach (var subDir in directoryInfo.GetDirectories())
                {
                    if (!folderItem.Children.Any(child => child.Path == subDir.FullName))
                    {
                        var subFolderItem = new FileSystemItem
                        {
                            Path = subDir.FullName,
                            Name = subDir.Name,
                            IsFolder = true
                        };
                        folderItem.Children.Add(subFolderItem);
                    }
                }
            }
        }



        public void readManifestFile(string backupPath)
        {
            BackupStore store = App.GetService<BackupStore>();
            string backupName = store.SelectedBackup.BackupName;
            if (!string.IsNullOrEmpty(backupName))
            {
                string manifestPath = Path.Combine(store.SelectedBackup.DestinationPath, backupName, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    string manifestJson = File.ReadAllText(manifestPath);
                    BackupVersions = JsonSerializer.Deserialize<List<BackupVersion>>(manifestJson) ?? new List<BackupVersion>();
                    store.SelectedBackup.Version = BackupVersions.Last();
                    LoadContents(BackupVersions.First());
                }
            }
        }

        public async void LoadContents(BackupVersion backupVersion)
        {
            if (backupVersion == null)
            {
                return;
            }

            Version = backupVersion;

            // Create a new collection to hold the cumulative file system items
            ObservableCollection<FileSystemItem> cumulativeItems = new ObservableCollection<FileSystemItem>();

            foreach (var version in BackupVersions.Where(v => v.Version <= backupVersion.Version))
            {
                string zipFilePath = version.BackupZipFilePath;
                string metadata = await ReadMetadataFromZip(zipFilePath);
                ObservableCollection<FileSystemItem> versionItems = CreateFileSystemItemsFromJson(metadata);

                // Merge version items into cumulativeItems
                MergeFileSystemItems(cumulativeItems, versionItems);
            }

            BackupItems = cumulativeItems;
            BackupSetting = BackupVersions[0].BackupSetting; // Assuming this is still relevant
        }

        private void MergeFileSystemItems(ObservableCollection<FileSystemItem> cumulativeItems, ObservableCollection<FileSystemItem> versionItems)
        {
            foreach (var item in versionItems)
            {
                var existingItem = cumulativeItems.FirstOrDefault(i => i.Path == item.Path);

                if (existingItem == null)
                {
                    // If the item doesn't exist in cumulativeItems, add it
                    cumulativeItems.Add(item);
                }
                else
                {
                    // If the item already exists, update it
                    //existingItem.UpdateFrom(item);

                    // If the existing item is a folder, merge its children
                    if (existingItem.IsFolder)
                    {
                        MergeFileSystemItems(existingItem.Children, item.Children);
                    }
                }

                // If the current item is a folder, ensure its children are processed
                if (item.IsFolder && existingItem == null)
                {
                    // The item was newly added, so its Children are not yet merged
                    MergeFileSystemItems(item.Children, item.Children);
                }
            }
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

        public void LoadBackup()
        {
            string[] locations = LoadBackupLocationsFromFile();
            List<string> backupNames = locations.Select(path => Path.GetFileNameWithoutExtension(path)).ToList();
            List<string> backupLocations = backupNames.ToList();

            int index = backupLocations.FindIndex(x => x == BackupName);
            if (index != -1)
            {
                DestinationPath = Path.GetDirectoryName(locations[index]);
                readManifestFile(DestinationPath);
                Messenger.Default.Send<BackupVersion>(BackupVersions[0], BackupStatus.Loaded);
            }
            else
            {
                Messenger.Default.Send<BackupVersion>(null, BackupStatus.Loaded);
            }
        }

    }

}

