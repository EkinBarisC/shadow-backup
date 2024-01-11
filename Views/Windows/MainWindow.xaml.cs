// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Windows;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Back_It_Up.Views.Windows
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }
        ISnackbarService snackbarService;
        INavigationService NavigationService;
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

            NavigationService = navigationService;

            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);
            NavigationView.SetServiceProvider(serviceProvider);
            this.snackbarService = snackbarService;

            Messenger.Default.Register<string>(this, BackupStatus.Complete, OnBackupCreated);
            Messenger.Default.Register<string>(this, BackupStatus.RestoreComplete, OnRestoreCreated);
            Messenger.Default.Register<string>(this, BackupStatus.Deleted, OnBackupDeleted);

        }

        private void OnBackupDeleted(string backupName)
        {
            //ShowSnackbarMessage("Backup Deleted");
            UpdateBackupList();
            while (NavigationView.CanGoBack)
                NavigationView.GoBack();
        }

        private void OnBackupCreated(string backupName)
        {
            //ShowSnackbarMessage("Backup Completed");
            UpdateBackupList();

        }
        private void OnRestoreCreated(string backupName)
        {
            ShowSnackbarMessage("Restore Completed");
        }

        private void UpdateBackupList()
        {
            ObservableCollection<object> backupList = ViewModel.LoadBackupLocations();

            // Create a new NavigationView instance
            var newNavigationView = new NavigationView
            {
                MenuItemsSource = backupList,
                // Copy other necessary properties from the old NavigationView to the new one
                Header = NavigationView.Header,
                IsPaneOpen = NavigationView.IsPaneOpen,
                PaneDisplayMode = NavigationView.PaneDisplayMode,
                // etc...
            };

            // Assign event handlers to the new NavigationView
            newNavigationView.SelectionChanged += NavigationView_SelectionChanged;

            // Replace the old NavigationView with the new one in the layout
            // Assuming NavigationView is placed directly in the layout (like in a Grid)
            var parent = NavigationView.Parent as Panel;
            if (parent != null)
            {
                int index = parent.Children.IndexOf(NavigationView);
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, newNavigationView);
            }

            // Reset the NavigationService to use the new NavigationView
            NavigationService.SetNavigationControl(newNavigationView);

            // Update the reference to the new NavigationView
            NavigationView = newNavigationView;
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
                    store.SelectedBackup = new Backup();
                    store.SelectedBackup.BackupName = selectedItem.Content.ToString();
                    store.SelectedBackup.LoadBackup();
                }
                else if (selectedItem.Content != null && selectedItem.Content.ToString() == "Add New Backup")
                {
                    store.SelectedBackup = new Backup();
                    store.SelectedBackup.LoadBackup();

                }

            }
        }


    }
}
