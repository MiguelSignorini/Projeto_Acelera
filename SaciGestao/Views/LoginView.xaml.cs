using System;
using System.Windows;
using System.Windows.Controls;
using SaciGestao.Models;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    // Tela de login. Fica dentro da MainWindow (mesma janela do sistema).
    public partial class LoginView : UserControl
    {
        private LoginViewModel _viewModel = null!;

        // Avisa a MainWindow que o login deu certo
        public event Action<Usuario>? LoginRealizado;

        public LoginView()
        {
            InitializeComponent();
            Reiniciar();

            // Responsivo: em telas estreitas (celular/janela pequena) o painel da marca some
            SizeChanged += (s, e) =>
            {
                bool estreito = e.NewSize.Width < 820;
                painelMarca.Visibility = estreito ? Visibility.Collapsed : Visibility.Visible;
                colMarca.Width = estreito ? new GridLength(0) : new GridLength(1.15, GridUnitType.Star);
            };
        }

        // Volta ao estado inicial (usado ao sair da conta)
        public void Reiniciar()
        {
            _viewModel = new LoginViewModel();
            _viewModel.LoginRealizado += usuario => LoginRealizado?.Invoke(usuario);
            DataContext = _viewModel;
            txtSenhaLogin.Clear();
            txtNovaSenha.Clear();
            txtConfirmarSenha.Clear();
        }

        // PasswordBox não tem Binding de Password por segurança do WPF,
        // então repassamos o valor pro ViewModel manualmente aqui.
        private void SenhaLogin_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SenhaLogin = txtSenhaLogin.Password;
        }

        private void NovaSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.RecNovaSenha = txtNovaSenha.Password;
        }

        private void ConfirmarSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.RecConfirmarSenha = txtConfirmarSenha.Password;
        }

        // Fecha o programa sem precisar entrar
        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
