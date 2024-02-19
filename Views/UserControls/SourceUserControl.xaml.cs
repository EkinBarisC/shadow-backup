using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using Back_It_Up.Views.Pages;
using CommunityToolkit.Mvvm.Messaging;
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

namespace Back_It_Up.Views.UserControls
{

    public partial class SourceUserControl : UserControl
    {
        public SourceViewModel ViewModel { get; }
        public SourceUserControl(SourceViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BackupStore store = App.GetService<BackupStore>();
                store.SelectedBackup.BackupName = BackupNameTextBox.Text;


            }
        }

        private void BackupNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BackupStore store = App.GetService<BackupStore>();
            store.SelectedBackup.BackupName = BackupNameTextBox.Text;
        }
    }
}
