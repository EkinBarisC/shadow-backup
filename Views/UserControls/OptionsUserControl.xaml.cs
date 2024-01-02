using Back_It_Up.ViewModels.UserControls;
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
        }

        //private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if (sender is TextBox textBox)
        //    {
        //        ViewModel.BackupSetting.FullBackupFrequency = textBox.Text;
        //    }
        //}

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
