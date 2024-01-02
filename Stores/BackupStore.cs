using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Back_It_Up.Models;

namespace Back_It_Up.Stores
{
    class BackupStore
    {
        public Backup SelectedBackup = new Backup();
        public enum ExplorerContext
        {
            Backup, Restore, Find
        }
        public ExplorerContext CurrentContext { get; set; }
    }
}
