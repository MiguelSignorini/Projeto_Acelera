using System;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly UsuarioService _service = UsuarioService.Instancia;

        // Avisa a LoginWindow (no code-behind) que pode abrir a MainWindow
        public event Action<Usuario>? LoginRealizado;

        // ============================================================
        // TELA DE LOGIN
        // ============================================================
        private string _nomeUsuarioLogin = string.Empty;
        public string NomeUsuarioLogin { get => _nomeUsuarioLogin; set => SetProperty(ref _nomeUsuarioLogin, value); }

        private string _senhaLogin = string.Empty;
        public string SenhaLogin { get => _senhaLogin; set => SetProperty(ref _senhaLogin, value); }

        private string _erroLogin = string.Empty;
        public string ErroLogin { get => _erroLogin; private set => SetProperty(ref _erroLogin, value); }

        // ============================================================
        // CONTROLE DE TELA (login / recuperar senha)
        // ============================================================
        private bool _mostrandoRecuperar;
        public bool MostrandoRecuperar { get => _mostrandoRecuperar; private set => SetProperty(ref _mostrandoRecuperar, value); }

        // ============================================================
        // RECUPERAR SENHA
        // ============================================================
        private string _recNomeUsuario = string.Empty;
        public string RecNomeUsuario { get => _recNomeUsuario; set => SetProperty(ref _recNomeUsuario, value); }

        private bool _perguntaRevelada;
        public bool PerguntaRevelada { get => _perguntaRevelada; private set => SetProperty(ref _perguntaRevelada, value); }

        private string _perguntaTexto = string.Empty;
        public string PerguntaTexto { get => _perguntaTexto; private set => SetProperty(ref _perguntaTexto, value); }

        private string _recResposta = string.Empty;
        public string RecResposta { get => _recResposta; set => SetProperty(ref _recResposta, value); }

        private string _recNovaSenha = string.Empty;
        public string RecNovaSenha { get => _recNovaSenha; set => SetProperty(ref _recNovaSenha, value); }

        private string _recConfirmarSenha = string.Empty;
        public string RecConfirmarSenha { get => _recConfirmarSenha; set => SetProperty(ref _recConfirmarSenha, value); }

        private string _erroRecuperar = string.Empty;
        public string ErroRecuperar { get => _erroRecuperar; private set => SetProperty(ref _erroRecuperar, value); }

        private string _sucessoRecuperar = string.Empty;
        public string SucessoRecuperar { get => _sucessoRecuperar; private set => SetProperty(ref _sucessoRecuperar, value); }

        // ============================================================
        // COMANDOS
        // ============================================================
        public ICommand EntrarCommand { get; }
        public ICommand AbrirRecuperarCommand { get; }
        public ICommand VoltarLoginCommand { get; }
        public ICommand ContinuarRecuperarCommand { get; }
        public ICommand RedefinirSenhaCommand { get; }

        public LoginViewModel()
        {
            EntrarCommand = new RelayCommand(_ => Entrar());
            AbrirRecuperarCommand = new RelayCommand(_ => AbrirRecuperar());
            VoltarLoginCommand = new RelayCommand(_ => VoltarLogin());
            ContinuarRecuperarCommand = new RelayCommand(_ => ContinuarRecuperar());
            RedefinirSenhaCommand = new RelayCommand(_ => RedefinirSenha());
        }

        private void Entrar()
        {
            if (string.IsNullOrWhiteSpace(NomeUsuarioLogin) || string.IsNullOrWhiteSpace(SenhaLogin))
            {
                ErroLogin = "Preencha usuário e senha.";
                return;
            }

            var usuario = _service.Autenticar(NomeUsuarioLogin.Trim(), SenhaLogin);

            if (usuario == null)
            {
                ErroLogin = "Usuário ou senha incorretos.";
                return;
            }

            ErroLogin = string.Empty;
            LoginRealizado?.Invoke(usuario);
        }

        private void AbrirRecuperar()
        {
            RecNomeUsuario = NomeUsuarioLogin; // facilita, já vem preenchido se a pessoa já tinha digitado
            PerguntaRevelada = false;
            RecResposta = string.Empty;
            RecNovaSenha = string.Empty;
            RecConfirmarSenha = string.Empty;
            ErroRecuperar = string.Empty;
            SucessoRecuperar = string.Empty;

            MostrandoRecuperar = true;
        }

        private void VoltarLogin()
        {
            MostrandoRecuperar = false;
            ErroLogin = string.Empty;
        }

        // Passo 1: encontra o usuário e revela a pergunta secreta dele
        private void ContinuarRecuperar()
        {
            if (string.IsNullOrWhiteSpace(RecNomeUsuario))
            {
                ErroRecuperar = "Informe o nome de usuário.";
                return;
            }

            var usuario = _service.ObterPorNomeUsuario(RecNomeUsuario.Trim());
            if (usuario == null)
            {
                ErroRecuperar = "Usuário não encontrado.";
                PerguntaRevelada = false;
                return;
            }

            PerguntaTexto = usuario.PerguntaSecreta;
            ErroRecuperar = string.Empty;
            PerguntaRevelada = true;
        }

        // Passo 2: confere a resposta e troca a senha
        private void RedefinirSenha()
        {
            if (!_service.ValidarResposta(RecNomeUsuario.Trim(), RecResposta))
            {
                ErroRecuperar = "Resposta incorreta.";
                return;
            }

            if (string.IsNullOrWhiteSpace(RecNovaSenha) || RecNovaSenha.Length < 3)
            {
                ErroRecuperar = "A nova senha deve ter pelo menos 3 caracteres.";
                return;
            }

            if (RecNovaSenha != RecConfirmarSenha)
            {
                ErroRecuperar = "As senhas não coincidem.";
                return;
            }

            _service.RedefinirSenha(RecNomeUsuario.Trim(), RecNovaSenha);

            ErroRecuperar = string.Empty;
            SucessoRecuperar = "Senha redefinida! Você já pode entrar com a nova senha.";
            NomeUsuarioLogin = RecNomeUsuario;
            SenhaLogin = string.Empty;
        }
    }
}
