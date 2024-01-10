// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Windows;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.Views.Windows
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }
        ISnackbarService snackbarService;
        private ControlAppearance snackbarAppearance = ControlAppearance.Secondary;

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
            InitializeComponent();


            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);
            NavigationView.SetServiceProvider(serviceProvider);
            this.snackbarService = snackbarService;

            Messenger.Default.Register<string>(this, BackupStatus.Complete, OnBackupCreated);
            Messenger.Default.Register<string>(this, BackupStatus.RestoreComplete, OnRestoreCreated);

        }

        private void OnBackupCreated(string backupName)
        {
            ShowSnackbarMessage("Backup Completed");
            ViewModel.LoadBackupLocations();

            for (int i = 0; i < ViewModel.MenuItems.Count; i++)
            {
                if (ViewModel.MenuItems[i] is NavigationViewItem navigationViewItem && NavigationView.ItemTemplate != null && navigationViewItem.Template != NavigationView.ItemTemplate)
                {
                    navigationViewItem.Template = NavigationView.ItemTemplate;
                }
            }

        }
        private void OnRestoreCreated(string backupName)
        {
            ShowSnackbarMessage("Restore Completed");
        }

        public void ShowSnackbarMessage(string message)
        {
            snackbarService.Show(
           message, "",
           snackbarAppearance,
           new SymbolIcon(SymbolRegular.Save24),
           TimeSpan.FromSeconds(5)
       );
        }


        private void NavigationView_SelectionChanged(NavigationView sender, RoutedEventArgs args)
        {
            if (sender.SelectedItem is NavigationViewItem selectedItem)
            {
                BackupStore store = App.GetService<BackupStore>();
                if (selectedItem.Content != null && selectedItem.Content.ToString() != "Add New Backup")
                {
                    store.SelectedBackup.BackupName = selectedItem.Content.ToString();
                    store.SelectedBackup.LoadBackup();
                }
                else if (selectedItem.Content != null && selectedItem.Content.ToString() == "Add New Backup")
                {
                    store.SelectedBackup.LoadBackup();

                }

            }
        }
    }
}
