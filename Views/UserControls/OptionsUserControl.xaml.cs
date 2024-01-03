using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.UserControls;
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

namespace Back_It_Up.Views.UserControls
{

    public partial class OptionsUserControl : UserControl
    {
        public OptionsViewModel ViewModel { get; }

        public OptionsUserControl(OptionsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            Messenger.Default.Register<BackupVersion>(this, BackupStatus.Loaded, OnBackupLoaded);

        }

        private void OnBackupLoaded(BackupVersion version)
        {
            BackupStore store = App.GetService<BackupStore>();
            ViewModel.BackupSetting = store.SelectedBackup.BackupSetting;
        }

        private void NumberBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox numberBox)
            {
                ViewModel.BackupSetting.FullBackupFrequency = (int?)numberBox.Value;
            }
        }

        private void NumberBox_ValueChanged_1(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox numberBox)
            {
                ViewModel.BackupSetting.DaysToKeepBackups = (int?)numberBox.Value;
            }
        }
    }
}
