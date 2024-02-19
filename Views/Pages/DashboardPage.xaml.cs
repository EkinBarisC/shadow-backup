

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
        }
    }
}
