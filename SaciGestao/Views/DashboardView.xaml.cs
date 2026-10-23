using System.Windows;
using System.Windows.Controls;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }

        // Atalhos da faixa de boas-vindas: abre a tela e já começa um cadastro novo
        private void Atalho_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botao && botao.Tag is string tela &&
                Window.GetWindow(this) is MainWindow principal)
            {
                principal.Navegar(tela, abrirNovo: true);
            }
        }
    }
}
