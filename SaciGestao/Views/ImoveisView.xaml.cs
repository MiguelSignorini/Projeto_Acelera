using System.Windows.Controls;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class ImoveisView : UserControl
    {
        public ImoveisView()
        {
            InitializeComponent();
            DataContext = new ImoveisViewModel();
        }
    }
}