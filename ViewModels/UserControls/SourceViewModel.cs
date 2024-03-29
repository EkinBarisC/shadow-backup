﻿

using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Vss;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Windows;
using Back_It_Up.Views.Pages;
using Back_It_Up.Views.UserControls;
using Back_It_Up.Views.Windows;
using GalaSoft.MvvmLight.Messaging;
using Serilog;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class SourceViewModel : ObservableObject
    {

        public ICommand OpenSourceExplorerCommand { get; set; }
        public ICommand OpenDestinationExplorerCommand { get; set; }
        private readonly INavigationService _navigationService;
        public ICommand PerformBackupCommand { get; set; }

        public SourceViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            this.OpenSourceExplorerCommand = new RelayCommand(OpenSourceExplorer);
            this.OpenDestinationExplorerCommand = new RelayCommand(OpenDestinationExplorer);
            this.PerformBackupCommand = new RelayCommand(PerformBackup);
        }

        private void OpenSourceExplorer()
        {
            _navigationService.Navigate(typeof(SourceExplorerPage));
        }
        private void OpenDestinationExplorer()
        {
            BackupStore store = App.GetService<BackupStore>();
            store.CurrentContext = BackupStore.ExplorerContext.Backup;
            _navigationService.Navigate(typeof(DestinationExplorerPage));
        }

        private async void PerformBackup()
        {
            BackupStore store = App.GetService<BackupStore>();
            await store.SelectedBackup.PerformBackup();
            Messenger.Default.Send<string>("Backup Complete", BackupStatus.Complete);

        }

        [RelayCommand]
        private async void ShowDeleteConfirmationDialog(object backupItem)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "WPF UI Message Box",
                Content = "Do you want to delete this backup?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
            };

            var result = await uiMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                DeleteBackup();
            }
        }

        private async void DeleteBackup()
        {
            BackupStore store = App.GetService<BackupStore>();
            await store.SelectedBackup.DeleteBackup();
            store.SelectedBackup = new Backup();
            Messenger.Default.Send<string>("Backup Complete", BackupStatus.Deleted);

        }

    }
}
