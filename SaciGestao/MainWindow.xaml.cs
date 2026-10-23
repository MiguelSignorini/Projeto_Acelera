using System.Windows;
using System.Windows.Controls;
using SaciGestao.Models;
using SaciGestao.Views;

namespace SaciGestao
{
    public partial class MainWindow : Window
    {
        private readonly Usuario _usuarioLogado;

        public MainWindow(Usuario usuarioLogado)
        {
            InitializeComponent();

            _usuarioLogado = usuarioLogado;

            // Mostra o nome de quem está logado na barra superior
            runNomeUsuario.Text = _usuarioLogado.NomeCompleto;

            // Abre direto no Dashboard
            Loaded += (s, e) => rbDashboard.IsChecked = true;
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            if (AreaConteudo == null) return;

            var item = sender as RadioButton;

            switch (item?.Name)
            {
                case "rbDashboard":
                    AreaConteudo.Content = new DashboardView();
                    break;

                case "rbImoveis":
                    AreaConteudo.Content = new ImoveisView();
                    break;

                case "rbClientes":
                    AreaConteudo.Content = new ClientesView();
                    break;

                case "rbContratos":
                    AreaConteudo.Content = new ContratosView();
                    break;

                default:
                    AreaConteudo.Content = new TextBlock
                    {
                        Text = "Tela em construção",
                        FontSize = 18,
                        Foreground = System.Windows.Media.Brushes.Gray
                    };
                    break;
            }
        }

        // Troca de tela pelo nome do item do menu; opcionalmente já abre o formulário de cadastro novo
        public void Navegar(string nomeMenu, bool abrirNovo = false)
        {
            if (FindName(nomeMenu) is not RadioButton item) return;

            item.IsChecked = true;

            if (abrirNovo && AreaConteudo.Content is FrameworkElement tela &&
                tela.DataContext?.GetType().GetProperty("NovoCommand")?.GetValue(tela.DataContext) is System.Windows.Input.ICommand novo &&
                novo.CanExecute(null))
            {
                novo.Execute(null);
            }
        }

        // Sair fecha o programa
        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}