// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Vss;
using Back_It_Up.Models;
using Back_It_Up.ViewModels.Windows;
using Back_It_Up.Views.Pages;
using Back_It_Up.Views.UserControls;
using Back_It_Up.Views.Windows;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class SourceViewModel : ObservableObject
    {

        public ICommand PerformBackupCommand { get; set; }

              public SourceViewModel()
        {
            this.PerformBackupCommand = new RelayCommand(PerformBackup);
               }

      
        private void PerformBackup()
        {
            string source_file = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\texts\text 1.txt";
            string backup_root = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\backups";
            string backup_path = Path.Combine(backup_root, Path.GetFileName(source_file));

            // Initialize the shadow copy subsystem.
            using (VssBackup vss = new VssBackup())
            {
                vss.Setup(Path.GetPathRoot(source_file));
                string snap_path = vss.GetSnapshotPath(source_file);
                // Here we use the AlphaFS library to make the copy.
                //File.Copy(snap_path, backup_path);
                string xmlPath = backup_path + ".xml";
                //CreateXmlFile(xmlPath, source_file, snap_path);
                string[] filesToBeZipped = { xmlPath, backup_path };

                string backupName = "mock backup";
                string zipPath = Path.Combine(backup_root, backupName + ".zip");
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile(source_file, Path.GetFileName(source_file), CompressionLevel.Fastest);
                    archive.ExtractToDirectory(backup_path);
                }

            }


        }

        private void CreateXmlFile(string xmlFilePath, string originalFilePath, string snapshotPath)
        {
            // Create XML document
            XmlDocument xmlDoc = new XmlDocument();

            // Create root element
            XmlElement root = xmlDoc.CreateElement("BackupInfo");
            xmlDoc.AppendChild(root);

            // Add attributes to the XML document
            XmlElement fileNameElement = xmlDoc.CreateElement("FileName");
            fileNameElement.InnerText = Path.GetFileName(originalFilePath);
            root.AppendChild(fileNameElement);

            XmlElement originalPathElement = xmlDoc.CreateElement("OriginalPath");
            originalPathElement.InnerText = originalFilePath;
            root.AppendChild(originalPathElement);

            XmlElement snapshotPathElement = xmlDoc.CreateElement("SnapshotPath");
            snapshotPathElement.InnerText = snapshotPath;
            root.AppendChild(snapshotPathElement);

            // Add more attributes as needed (e.g., file size, timestamp, etc.)

            // Save the XML document
            xmlDoc.Save(xmlFilePath);
        }

        private void ZipFiles(string zipFilePath, string[] files)
        {
            // Create a zip file
            using (ZipArchive zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    // Add each file to the zip archive
                    zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }
        }

    }
}
