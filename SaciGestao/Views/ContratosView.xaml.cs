using System.Windows.Controls;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class ContratosView : UserControl
    {
        public ContratosView()
        {
            InitializeComponent();
            DataContext = new ContratosViewModel();
        }
    }
}