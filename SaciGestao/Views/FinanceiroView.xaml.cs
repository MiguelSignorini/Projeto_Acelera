using System.Windows.Controls;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class FinanceiroView : UserControl
    {
        public FinanceiroView()
        {
            InitializeComponent();
            DataContext = new FinanceiroViewModel();
        }
    }
}