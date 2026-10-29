using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SaciGestao.Converters;
using SaciGestao.Models;
using SaciGestao.Service;
using SaciGestao.Views;

namespace SaciGestao
{
    public partial class MainWindow : Window
    {
        private readonly Usuario _usuarioLogado;
        private static readonly CaminhoParaImagemConverter _conversorFoto = new CaminhoParaImagemConverter();

        public MainWindow(Usuario usuarioLogado)
        {
            InitializeComponent();

            _usuarioLogado = usuarioLogado;

            // Mostra o nome de quem está logado na barra superior
            runNomeUsuario.Text = _usuarioLogado.NomeCompleto;
            AtualizarPerfil();

            // Abre direto no Dashboard
            Loaded += (s, e) => rbDashboard.IsChecked = true;
        }

        // Atualiza nome e foto do usuário em todos os lugares do cabeçalho
        private void AtualizarPerfil()
        {
            runNomeUsuario.Text = _usuarioLogado.NomeCompleto;

            ImageSource? foto = null;
            if (!string.IsNullOrEmpty(_usuarioLogado.FotoCaminho))
                foto = _conversorFoto.Convert(_usuarioLogado.FotoCaminho, typeof(ImageSource), "80", CultureInfo.InvariantCulture) as ImageSource;

            elipsePerfil.Fill = foto != null
                ? new ImageBrush(foto) { Stretch = Stretch.UniformToFill }
                : new SolidColorBrush(Color.FromRgb(0xE4, 0xE8, 0xEE));
            iconePerfil.Visibility = foto == null ? Visibility.Visible : Visibility.Collapsed;
        }

        // Clique na foto: troca a tela principal pelas configurações da conta
        private void BtnUsuario_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in new[] { rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro })
                item.IsChecked = false;

            var tela = new ConfiguracoesView(_usuarioLogado);
            tela.PerfilAlterado += AtualizarPerfil;
            tela.SairContaSolicitado += SairConta;
            AreaConteudo.Content = tela;
        }

        // Sair da conta: volta para a tela de login (diferente do botão Sair do menu lateral, que fecha o sistema)
        private void SairConta()
        {
            UsuarioService.Instancia.Sair();

            new LoginWindow().Show();
            Close();
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