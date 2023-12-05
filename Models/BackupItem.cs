using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    class BackupItem
    {
        public ObservableCollection<FileSystemItem> BackupItems = new ObservableCollection<FileSystemItem>();
        public string DestinationPath;
        public BackupItemSetting BackupSetting = new BackupItemSetting();
        public string BackupName;
        public string RestorePath;
    }
}
