using System.Windows.Controls;
using System.Windows.Input;
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

        // Ao digitar no campo Bairro, abre a lista de sugestões que vai afinando conforme as letras
        private void ComboBairro_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo) return;
            if (e.Key is Key.Down or Key.Up or Key.Enter or Key.Escape or Key.Tab or Key.Left or Key.Right) return;
            combo.IsDropDownOpen = true;
        }
    }
}