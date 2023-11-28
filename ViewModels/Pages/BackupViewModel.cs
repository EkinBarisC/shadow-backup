﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.ViewModels.Windows;
using Back_It_Up.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class BackupViewModel : ObservableObject
    {

        public ICommand OpenSourceExplorerCommand { get; set; }

        private string[] _breadcrumbBarItems = new string[] { "Source & Destination", "Method & Cleaning", "Scheduling", "Encryption" };

        public BackupViewModel()
        {
            this.OpenSourceExplorerCommand = new RelayCommand(OpenSourceExplorer);
        }

        private void OpenSourceExplorer()
        {

        }
    }
}
