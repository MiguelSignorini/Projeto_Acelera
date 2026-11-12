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
        private Usuario? _usuarioLogado;

        // Modos de layout conforme a largura da janela
        private enum ModoLayout { Completo, Compacto, Celular }
        private ModoLayout _modo = ModoLayout.Completo;
        private bool _menuAberto;

        public MainWindow()
        {
            InitializeComponent();

            // Abre no tamanho original (1280x780, centralizada), mas nunca maior que a área útil da tela
            Width = Math.Min(1280, SystemParameters.WorkArea.Width);
            Height = Math.Min(780, SystemParameters.WorkArea.Height);

            // Janela única: começa no login; ao entrar, o sistema aparece nesta mesma janela
            loginView.LoginRealizado += Entrar;

            SizeChanged += (s, e) => AplicarLayout(e.NewSize.Width);
            Loaded += (s, e) => AplicarLayout(ActualWidth);
        }

        private void Entrar(Usuario usuario)
        {
            _usuarioLogado = usuario;
            AtualizarCabecalho();

            loginView.Visibility = Visibility.Collapsed;
            shell.Visibility = Visibility.Visible;

            // Abre direto no Dashboard (força recarregar mesmo se já estava marcado)
            rbDashboard.IsChecked = false;
            rbDashboard.IsChecked = true;
            AplicarLayout(ActualWidth);
        }

        // Volta para a tela de login sem fechar a janela
        private void VoltarAoLogin()
        {
            _usuarioLogado = null;
            UsuarioService.Instancia.Sair();
            FecharMenu();
            AreaConteudo.Content = null;
            foreach (var rb in new[] { rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro })
                rb.IsChecked = false;

            shell.Visibility = Visibility.Collapsed;
            loginView.Reiniciar();
            loginView.Visibility = Visibility.Visible;
        }

        // ---------- Responsividade ----------
        // >= 1000: menu completo | 760..1000: menu só com ícones | < 760: menu vira gaveta (botão ☰)
        private void AplicarLayout(double largura)
        {
            if (largura <= 0) return;

            var modo = largura >= 1000 ? ModoLayout.Completo
                     : largura >= 760 ? ModoLayout.Compacto
                     : ModoLayout.Celular;
            _modo = modo;

            bool celular = modo == ModoLayout.Celular;
            bool compacto = modo == ModoLayout.Compacto;

            Helpers.Responsivo.SetCompacto(menuLateral, compacto);
            blocoLogo.Visibility = compacto ? Visibility.Collapsed : Visibility.Visible;

            if (celular)
            {
                colMenu.Width = new GridLength(0);
                Grid.SetColumnSpan(menuLateral, 2);
                menuLateral.HorizontalAlignment = HorizontalAlignment.Left;
                menuLateral.Width = 240;
                menuLateral.Visibility = _menuAberto ? Visibility.Visible : Visibility.Collapsed;
                cortina.Visibility = _menuAberto ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                _menuAberto = false;
                colMenu.Width = new GridLength(compacto ? 72 : 240);
                Grid.SetColumnSpan(menuLateral, 1);
                menuLateral.HorizontalAlignment = HorizontalAlignment.Stretch;
                menuLateral.ClearValue(WidthProperty);
                menuLateral.Visibility = Visibility.Visible;
                cortina.Visibility = Visibility.Collapsed;
            }

            btnMenu.Visibility = celular ? Visibility.Visible : Visibility.Collapsed;
            barraTopo.Margin = new Thickness(celular ? 12 : 28, 0, celular ? 12 : 28, 0);
            rolagemConteudo.Margin = new Thickness(celular ? 10 : 24);
            txtBemVindo.FontSize = celular ? 16 : 20;
            txtBemVindo.Visibility = largura < 520 ? Visibility.Collapsed : Visibility.Visible;
            caixaData.Visibility = largura < 820 ? Visibility.Collapsed : Visibility.Visible;
            rotuloMenu.Visibility = compacto ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AbrirMenu_Click(object sender, RoutedEventArgs e)
        {
            _menuAberto = true;
            AplicarLayout(ActualWidth);
        }

        private void Cortina_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => FecharMenu();

        private void FecharMenu()
        {
            if (!_menuAberto) return;
            _menuAberto = false;
            AplicarLayout(ActualWidth);
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            if (AreaConteudo == null) return;

            var item = sender as RadioButton;
            FecharMenu(); // no modo celular a gaveta fecha ao escolher uma tela

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
            if (_usuarioLogado == null) return;
            runNomeUsuario.Text = _usuarioLogado.NomeCompleto;

            // Ex.: "Quinta, 12 de novembro" (primeira letra maiúscula)
            string hoje = DateTime.Today.ToString("dddd, d 'de' MMMM", new System.Globalization.CultureInfo("pt-BR"));
            txtData.Text = char.ToUpper(hoje[0]) + hoje.Substring(1).Replace("-feira", "");

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
            if (_usuarioLogado == null) return;

            foreach (var rb in new[] { rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro })
                rb.IsChecked = false;

            var configuracoes = new ConfiguracoesView(_usuarioLogado);
            configuracoes.PerfilAlterado += AtualizarCabecalho;
            configuracoes.SairContaSolicitado += VoltarAoLogin;
            AreaConteudo.Content = configuracoes;
        }

        // Salvar: grava tudo nos JSON principais (pasta Json, não a Comparacao) e fecha o sistema
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Se a tela de perfil está aberta com foto/nome alterados e ainda não aplicados, aplica antes
            if (AreaConteudo.Content is ConfiguracoesView perfil && !perfil.AplicarPendencias())
                return; // perfil inválido: a mensagem já aparece na tela e o sistema continua aberto

            Persistencia.SalvarNoPrincipal = true;
            Application.Current.Shutdown(); // o App.OnExit chama Salvar() de cada service
        }

        // Sair fecha o programa (grava em Json\Comparacao)
        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}