// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Back_It_Up.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Back_It_Up.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(
            DashboardViewModel viewModel,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            IContentDialogService contentDialogService
            )
        {

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            navigationService.SetNavigationControl(TopNavigationView);
            contentDialogService.SetContentPresenter(MainContentDialog);
            //TopNavigationView.SetServiceProvider(serviceProvider);

        }
    }
}
