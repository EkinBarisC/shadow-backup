using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Back_It_Up.Views.Pages
{
    /// <summary>
    /// Interaction logic for RestorePage.xaml
    /// </summary>
    public partial class RestorePage : INavigableView<RestoreViewModel>
    {
        public RestoreViewModel ViewModel { get; }
        public RestorePage(RestoreViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            Messenger.Default.Register<BackupVersion>(this, BackupStatus.Loaded, OnBackupLoaded);

        }

        private void OnBackupLoaded(BackupVersion version)
        {
            BackupStore store = App.GetService<BackupStore>();
            if (store.SelectedBackup.BackupVersions != null && store.SelectedBackup.BackupVersions.Count > 0)
            {
                ViewModel.LoadContents(store.SelectedBackup.BackupVersions[0]);
            }
            else
            {
                ViewModel.LoadContents();
            }
        }

    }
}
