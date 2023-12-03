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
            this.CurrentView = new SourceUserControl(new SourceViewModel(navigationService));
            BreadcrumbBarItems = new ObservableCollection<string>
    {
        "Source",
        "Options",
        "Schedule",
        ""
    };
        }

        public void ChangeUserControl(string breadcrumbSelection)
        {
            switch (breadcrumbSelection)
            {
                case "Source":
                    CurrentView = new SourceUserControl(new SourceViewModel(_navigationService));
                    break;
                case "Options":
                    CurrentView = new OptionsUserControl();
                    break;
                case "Schedule":
                    CurrentView = new ScheduleUserControl();
                    break;
                    // Add more cases as needed
            }
        }

      
        private void OpenSourceExplorer()
        {
        _navigationService.Navigate(typeof(SourceExplorerPage));
        }

    }
}
