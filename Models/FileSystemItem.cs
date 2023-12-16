using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeTypes;
using Back_It_Up.Stores;
using System.Windows.Controls;

namespace Back_It_Up.Models
{
    public partial class FileSystemItem : ObservableObject
    {

        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public string FileType { get; set; }


        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;

                    if (isExpanded && IsFolder && !contentsLoaded)
                    {
                        LoadContents();
                    }
                }
            }
        }

        private FileSystemItem parent;
        public FileSystemItem Parent
        {
            get { return parent; }
            set
            {
                parent = value;
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public bool contentsLoaded = false;
        private long fileSizeInBytes;
        public long FileSizeInBytes
        {
            get { return fileSizeInBytes; }
            set
            {
                if (fileSizeInBytes != value)
                {
                    fileSizeInBytes = value;
                }
            }
        }
        public string FileSize
        {
            get { return FormatFileSize(FileSizeInBytes); }
        }
        public bool isDummy = false;
        private static string FormatFileSize(long fileSize)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = fileSize;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }

        public void AddDummyChild()
        {
            if (IsFolder && Children.Count == 0)
            {
                FileSystemItem dummy = new FileSystemItem();
                dummy.isDummy = true;
                Children.Add(dummy);
            }
        }

        public ObservableCollection<FileSystemItem> Children { get; set; }

        public FileSystemItem()
        {
            Children = new ObservableCollection<FileSystemItem>();
        }

        public void CheckBox_Checked()
        {
            this.IsExpanded = true;

            if (this.Parent != null && this.Parent.IsSelected == false)
            {
                BackupStore store = App.GetService<BackupStore>();
                store.SelectedBackup.BackupItems.Add(this);
            }
        }

        public void CheckBox_Unchecked()
        {
            this.SetIsSelectedRecursively(false);

            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.BackupItems.Remove(this);
        }

        public void SetIsSelectedRecursively(bool isSelected)
        {
            this.IsSelected = isSelected;
            foreach (var child in Children)
            {
                child.SetIsSelectedRecursively(isSelected);
            }
        }

        public void LoadContents()
        {
            if (IsFolder && !contentsLoaded)
            {

                try
                {
                    var rootItems = Directory.GetFileSystemEntries(Path).Select(item =>
                    {
                        try
                        {
                            string mimeType = MimeTypeMap.GetMimeType(System.IO.Path.GetExtension(item));
                            return new FileSystemItem
                            {
                                Name = System.IO.Path.GetFileName(item),
                                Path = item,
                                IsFolder = Directory.Exists(item),
                                IsExpanded = false,
                                FileType = Directory.Exists(item) ? "file folder" : mimeType.Substring(0, mimeType.IndexOf('/')),
                                FileSizeInBytes = Directory.Exists(item) ? 0 : new FileInfo(item).Length,
                                Parent = this
                            };
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return null;
                        }
                    }).Where(item => item != null);

                    foreach (var item in rootItems)
                    {
                        item.Parent = this;
                        Children.Add(item);

                        // TODO: add dummy child only for lazy loading
                        if (item.IsFolder)
                        {
                            item.LoadContents();
                        }
                    }
                    Children.Remove(Children.Where(child => child.isDummy).SingleOrDefault());
                    contentsLoaded = true;
                }
                catch (UnauthorizedAccessException)
                {

                }
            }
        }

    }
}
