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
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class BackupViewModel : ObservableObject
    {

        public ICommand OpenSourceExplorerCommand { get; set; }
        private readonly INavigationService _navigationService;


        public ICommand PerformBackupCommand { get; set; }
        public ObservableCollection<string> BreadcrumbBarItems { get; private set; }

            private OptionsControl _optionsControl;

        public OptionsControl OptionsControl
        {
            get
            {
                if (_optionsControl == null)
                    _optionsControl = new OptionsControl();
                return _optionsControl;
            }
        }
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }
        public BackupViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            this.OpenSourceExplorerCommand = new RelayCommand(OpenSourceExplorer);
            this.PerformBackupCommand = new RelayCommand(PerformBackup);
            BreadcrumbBarItems = new ObservableCollection<string>
    {
        "Source",
        "Options",
        "Schedule",
    };
        }

        public void ChangeUserControl(string breadcrumbSelection)
        {
            switch (breadcrumbSelection)
            {
                case "Source":
                    CurrentView = OptionsControl;
                    break;
                    // Add more cases as needed
            }
        }

        private void PerformBackup()
        {
            //string source_file = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\texts\text 1.txt";
            //string backup_root = @"C:\Users\ekin1\OneDrive\Documents\backup tool files\backups";
            //string backup_path = Path.Combine(backup_root, Path.GetFileName(source_file));

            //// Initialize the shadow copy subsystem.
            //using (VssBackup vss = new VssBackup())
            //{
            //    vss.Setup(Path.GetPathRoot(source_file));
            //    string snap_path = vss.GetSnapshotPath(source_file);

            //    // Here we use the AlphaFS library to make the copy.
            //    File.Copy(snap_path, backup_path);
            //}

            // enumerate snapshots in the system and show info about them
            IVssFactory vssImplementation = VssFactoryProvider.Default.GetVssFactory();
            using (IVssBackupComponents backup = vssImplementation.CreateVssBackupComponents())
            {
                backup.InitializeForBackup(null);

                backup.SetContext(VssSnapshotContext.All);

                foreach (VssSnapshotProperties prop in backup.QuerySnapshots())
                {

                    System.Diagnostics.Debug.WriteLine("Snapshot ID: {0:B}", prop.SnapshotId);
                    System.Diagnostics.Debug.WriteLine("Snapshot Set ID: {0:B}", prop.SnapshotSetId);
                    System.Diagnostics.Debug.WriteLine("Original Volume Name: {0}", prop.OriginalVolumeName);
                }
            }

        }

        private void OpenSourceExplorer()
        {
        _navigationService.Navigate(typeof(SourceExplorerPage));
        }

    }
}
