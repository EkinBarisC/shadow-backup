// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using Back_It_Up.ViewModels.Windows;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
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
            UpdateBackupList();
            while (NavigationView.CanGoBack)
                NavigationView.GoBack();
            ShowSnackbarMessage("Backup Deleted");
        }

        private void OnBackupCreated(string backupName)
        {
            UpdateBackupList();
            ShowSnackbarMessage("Backup Completed");

        }
        private void OnRestoreCreated(string backupName)
        {
            ShowSnackbarMessage("Restore Completed");
        }

        private void UpdateBackupList()
        {
            ObservableCollection<object> backupList = ViewModel.LoadBackupLocations();

            var newNavigationView = new NavigationView
            {
                MenuItemsSource = backupList,
                Header = NavigationView.Header,
                IsPaneOpen = NavigationView.IsPaneOpen,
                PaneDisplayMode = NavigationView.PaneDisplayMode,
                BreadcrumbBar = NavigationView.BreadcrumbBar,
                Padding = NavigationView.Padding,
                FooterMenuItemsSource = NavigationView.FooterMenuItemsSource,
                FrameMargin = NavigationView.FrameMargin,
                IsBackButtonVisible = NavigationView.IsBackButtonVisible,
                IsPaneToggleVisible = NavigationView.IsPaneToggleVisible,
                OpenPaneLength = NavigationView.OpenPaneLength,
                TitleBar = NavigationView.TitleBar
            };

            newNavigationView.SelectionChanged += NavigationView_SelectionChanged;
            var parent = NavigationView.Parent as Panel;
            if (parent != null)
            {
                int index = parent.Children.IndexOf(NavigationView);
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, newNavigationView);
            }

            NavigationService.SetNavigationControl(newNavigationView);

            NavigationView = newNavigationView;

            if (newNavigationView.MenuItemsSource is ObservableCollection<object> menuItems && menuItems.Count > 0)
            {
                if (menuItems[0] is NavigationViewItem firstItem && firstItem.Tag != null)
                {
                    NavigationService.Navigate(firstItem.Tag.ToString());
                }
            }
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
                    SourceExplorerViewModel sourceExplorer = App.GetService<SourceExplorerViewModel>();
                    sourceExplorer.ClearCheckedItems();
                }

            }
        }


    }
}
