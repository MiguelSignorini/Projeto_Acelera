using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SaciGestao.Models;
using SaciGestao.Service;
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

            // Mostra o nome e a foto de quem está logado na barra superior
            AtualizarCabecalho();

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

                case "rbFinanceiro":
                    AreaConteudo.Content = new FinanceiroView();
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

        // Nome e foto do usuário logado na barra superior
        private void AtualizarCabecalho()
        {
            runNomeUsuario.Text = _usuarioLogado.NomeCompleto;

            string? foto = _usuarioLogado.FotoCaminho;
            fotoPerfil.Fill = null;
            iconePerfil.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(foto) || !File.Exists(Persistencia.CaminhoDaFoto(foto))) return;

            try
            {
                var imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.CacheOption = BitmapCacheOption.OnLoad;
                imagem.DecodePixelWidth = 120;
                imagem.UriSource = new Uri(Persistencia.CaminhoDaFoto(foto));
                imagem.EndInit();
                imagem.Freeze();

                fotoPerfil.Fill = new ImageBrush(imagem) { Stretch = Stretch.UniformToFill };
                iconePerfil.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        // Abre as configurações do perfil (foto, nome, senha, recuperação, sair da conta)
        private void Perfil_Click(object sender, RoutedEventArgs e)
        {
            foreach (var rb in new[] { rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro })
                rb.IsChecked = false;

            var configuracoes = new ConfiguracoesView(_usuarioLogado);
            configuracoes.PerfilAlterado += AtualizarCabecalho;
            configuracoes.SairContaSolicitado += () =>
            {
                new LoginWindow().Show();
                Close();
            };
            AreaConteudo.Content = configuracoes;
        }

        // Sair fecha o programa
        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}