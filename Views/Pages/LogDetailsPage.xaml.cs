using Back_It_Up.Models;
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

namespace Back_It_Up.Views.Pages
{

    public partial class LogDetailsPage : Page
    {
        public LogDetailsViewModel ViewModel { get; }
        public LogDetailsPage(LogDetailsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            Messenger.Default.Register<string>(this, BackupStatus.Log, OnLogSelected);
        }

        public void OnLogSelected(string entry)
        {
            ViewModel.LoadLogEntry();
        }


    }
}
