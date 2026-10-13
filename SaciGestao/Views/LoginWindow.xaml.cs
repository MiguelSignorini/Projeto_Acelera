using System.Windows;
using SaciGestao.Models;
using SaciGestao.ViewModels;

namespace SaciGestao.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            // Quando o login dá certo, abre a MainWindow e fecha esta janela
            _viewModel.LoginRealizado += UsuarioLogado =>
            {
                var mainWindow = new MainWindow(UsuarioLogado);
                mainWindow.Show();
                Close();
            };
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