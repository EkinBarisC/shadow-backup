﻿using Back_It_Up.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
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

    public partial class BackupPage : INavigableView<BackupViewModel>
    {
        public BackupViewModel ViewModel { get; }

        public BackupPage(
            BackupViewModel viewModel,
            IContentDialogService contentDialogService
            )
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            contentDialogService.SetContentPresenter(InnerContentDialog);
        }

        private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ViewModel.ChangeUserControl(args.Item.ToString());
        }
    }
}
