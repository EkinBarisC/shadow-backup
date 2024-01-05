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

namespace Back_It_Up.Views.UserControls
{
    /// <summary>
    /// Interaction logic for EncryptUserControl.xaml
    /// </summary>
    public partial class EncryptUserControl : UserControl
    {
        public EncryptViewModel ViewModel { get; }

        public EncryptUserControl(EncryptViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
