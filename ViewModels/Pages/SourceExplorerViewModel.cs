// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Alphaleonis.Win32.Filesystem;
using Back_It_Up.Models;
using MimeTypes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class SourceExplorerViewModel : ObservableObject
    {
        public ObservableCollection<FileSystemItem> fileSystemItems {  get; set; }

        public SourceExplorerViewModel()
        {

            // Initialize the file system items (you should customize this part)
            fileSystemItems = new ObservableCollection<FileSystemItem>();
            LoadFileSystemItems("C:\\Users\\ekin1\\OneDrive\\Documents"); // Load the root directory
            //TreeViewSourceExplorer.ItemsSource = fileSystemItems;

        }

        private void LoadFileSystemItems(string path)
        {
            try
            {
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
                            FileSizeInBytes = Directory.Exists(item) ? 0 : new FileInfo(item).Length
                        };
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return null; // Exclude folders with admin access issues
                    }
                }).Where(item => item != null);

                fileSystemItems.Clear();
                foreach (var item in rootItems)
                {
                    fileSystemItems.Add(item);

                    // If it's a folder, load its immediate subitems
                    if (item.IsFolder)
                    {
                        item.AddDummyChild();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Handle access issues at the root directory level
            }
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Get the data item associated with this CheckBox
            var checkBox = (CheckBox)sender;
            var dataItem = (FileSystemItem)checkBox.DataContext; // Ensure your DataContext is correctly set
            dataItem.IsExpanded = true;

            //if (dataItem.Parent != null && dataItem.Parent.IsSelected == false)
                
                //App.store.selectedBackup.BackupItems.Add(dataItem);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Get the data item associated with this CheckBox
            var checkBox = (CheckBox)sender;
            var dataItem = (FileSystemItem)checkBox.DataContext; // Ensure your DataContext is correctly set

            // Update the IsSelected property
            dataItem.SetIsSelectedRecursively(false);

            //App.store.selectedBackup.BackupItems.Remove(dataItem);
        }

        
        //public ICommand OpenSourceExplorerCommand { get; set; }

        //private string[] _breadcrumbBarItems = new string[] { "Source & Destination", "Method & Cleaning", "Scheduling", "Encryption" };

        //public SourceExplorerViewModel()
        //{
        //    this.OpenSourceExplorerCommand = new RelayCommand(OpenSourceExplorer);
        //}

        //private void OpenSourceExplorer()
        //{
        //    Console.WriteLine("ekin");
        //}
    }
}
