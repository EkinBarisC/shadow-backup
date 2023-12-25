// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Windows;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Back_It_Up.Views.Windows
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService
        )
        {
            Wpf.Ui.Appearance.Watcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;
            //Messenger.Default.Register<string>(this, OnBackupCreated);
            InitializeComponent();

            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);

            NavigationView.SetServiceProvider(serviceProvider);
        }

        private void OnBackupCreated(string backupName)
        {
            ViewModel.LoadBackupLocations();
        }


        private void NavigationView_SelectionChanged(NavigationView sender, RoutedEventArgs args)
        {
            // Now use this content to determine what to do next
            if (sender.SelectedItem is NavigationViewItem selectedItem)
            {
                BackupStore store = App.GetService<BackupStore>();
                if (selectedItem.Content != null)
                {
                    store.SelectedBackup.BackupName = selectedItem.Content.ToString();
                    store.SelectedBackup.LoadBackup();
                }
            }
        }
    }
}
