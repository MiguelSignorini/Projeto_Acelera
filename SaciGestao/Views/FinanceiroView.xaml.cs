using System.Windows.Controls;
using System.Windows.Input;
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

        // Clicar no fundo escurecido fecha a janela "Registrar pagamento"
        private void Fundo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is FinanceiroViewModel vm) vm.FecharDialogoCommand.Execute(null);
        }
    }
}
