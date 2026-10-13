using System.Windows.Controls;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class ClientesView : UserControl
    {
        public ClientesView()
        {
            InitializeComponent();
            DataContext = new ClientesViewModel();
        }
    }
}
