﻿using Back_It_Up.ViewModels.Pages;
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
        }
    }
}
