using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.Views
{
    // Tela de configurações do usuário logado (aberta ao clicar na foto do perfil)
    public partial class ConfiguracoesView : UserControl
    {
        private readonly Usuario _usuario;

        // Escolhas de foto ainda não gravadas: a cópia para a pasta Fotos só acontece ao salvar o perfil
        private string? _novaFotoOrigem;
        private bool _removerFoto;

        // Avisa a MainWindow para atualizar nome e foto do cabeçalho
        public event Action? PerfilAlterado;

        // Avisa a MainWindow para encerrar a sessão e voltar ao login
        public event Action? SairContaSolicitado;

        public ConfiguracoesView(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario;
            txtNome.Text = usuario.NomeCompleto;
            txtUsuario.Text = usuario.NomeUsuario;
            txtPergunta.Text = usuario.PerguntaSecreta;

            MostrarPrevia(usuario.FotoCaminho);

            rbSecaoPerfil.IsChecked = true;
        }

        // Mostra só a seção escolhida no menu lateral
        private void Secao_Checked(object sender, RoutedEventArgs e)
        {
            if (secaoPerfil == null) return;

            secaoPerfil.Visibility = rbSecaoPerfil.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            secaoSenha.Visibility = rbSecaoSenha.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            secaoRecuperacao.Visibility = rbSecaoRecuperacao.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            secaoSessao.Visibility = rbSecaoSessao.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // ============================================================
        // PERFIL
        // ============================================================
        private void AlterarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Selecione a foto do perfil",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
            };

            if (dialogo.ShowDialog(Window.GetWindow(this)) != true) return;

            _novaFotoOrigem = dialogo.FileName;
            _removerFoto = false;
            MostrarPrevia(dialogo.FileName);
        }

        private void RemoverFoto_Click(object sender, RoutedEventArgs e)
        {
            _novaFotoOrigem = null;
            _removerFoto = true;
            elipseFoto.Fill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE8, 0xEE));
        }

        private void SalvarPerfil_Click(object sender, RoutedEventArgs e)
        {
            string nome = txtNome.Text.Trim();
            string nomeUsuario = txtUsuario.Text.Trim();

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(nomeUsuario))
            {
                Mostrar(txtMsgPerfil, "Preencha o nome completo e o nome de usuário.", false);
                return;
            }

            // Não deixa dois usuários com o mesmo nome de login (ignorando maiúsculas)
            var outro = UsuarioService.Instancia.ObterPorNomeUsuario(nomeUsuario);
            if (outro != null && outro != _usuario)
            {
                Mostrar(txtMsgPerfil, "Esse nome de usuário já está em uso.", false);
                return;
            }

            try
            {
                if (_novaFotoOrigem != null)
                {
                    // Mesmo padrão das fotos dos imóveis: cópia com nome único dentro da pasta Fotos
                    Directory.CreateDirectory(Persistencia.PastaFotos);
                    string nomeArquivo = Guid.NewGuid().ToString("N") + Path.GetExtension(_novaFotoOrigem);
                    File.Copy(_novaFotoOrigem, Path.Combine(Persistencia.PastaFotos, nomeArquivo));
                    _usuario.FotoCaminho = nomeArquivo;
                }
                else if (_removerFoto)
                {
                    _usuario.FotoCaminho = null;
                }
            }
            catch (Exception ex)
            {
                Mostrar(txtMsgPerfil, "Não foi possível salvar a foto: " + ex.Message, false);
                return;
            }

            _usuario.NomeCompleto = nome;
            _usuario.NomeUsuario = nomeUsuario;
            _novaFotoOrigem = null;
            _removerFoto = false;

            PerfilAlterado?.Invoke();
            Mostrar(txtMsgPerfil, "Perfil salvo.", true);
        }

        // ============================================================
        // SENHA
        // ============================================================
        private void SalvarSenha_Click(object sender, RoutedEventArgs e)
        {
            if (pwSenhaAtual.Password != _usuario.Senha)
            {
                Mostrar(txtMsgSenha, "A senha atual está incorreta.", false);
                return;
            }

            if (pwSenhaNova.Password.Length < 3)
            {
                Mostrar(txtMsgSenha, "A nova senha deve ter pelo menos 3 caracteres.", false);
                return;
            }

            if (pwSenhaNova.Password != pwSenhaConfirmar.Password)
            {
                Mostrar(txtMsgSenha, "As senhas não coincidem.", false);
                return;
            }

            UsuarioService.Instancia.RedefinirSenha(_usuario.NomeUsuario, pwSenhaNova.Password);

            pwSenhaAtual.Clear();
            pwSenhaNova.Clear();
            pwSenhaConfirmar.Clear();
            Mostrar(txtMsgSenha, "Senha alterada com sucesso.", true);
        }

        // ============================================================
        // RECUPERAÇÃO DE SENHA
        // ============================================================
        private void SalvarRecuperacao_Click(object sender, RoutedEventArgs e)
        {
            string pergunta = txtPergunta.Text.Trim();
            string resposta = txtResposta.Text.Trim();

            if (string.IsNullOrWhiteSpace(pergunta) || string.IsNullOrWhiteSpace(resposta))
            {
                Mostrar(txtMsgRecuperacao, "Preencha a pergunta e a resposta secreta.", false);
                return;
            }

            _usuario.PerguntaSecreta = pergunta;
            _usuario.RespostaSecreta = resposta;
            txtResposta.Clear();

            Mostrar(txtMsgRecuperacao, "Recuperação de senha atualizada.", true);
        }

        // ============================================================
        // SESSÃO
        // ============================================================
        private void SairConta_Click(object sender, RoutedEventArgs e)
        {
            SairContaSolicitado?.Invoke();
        }

        // ============================================================
        // AUXILIARES
        // ============================================================
        private void MostrarPrevia(string? fotoNome)
        {
            if (string.IsNullOrEmpty(fotoNome) || !File.Exists(Persistencia.CaminhoDaFoto(fotoNome)))
            {
                elipseFoto.Fill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE8, 0xEE));
                return;
            }

            try
            {
                var imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.CacheOption = BitmapCacheOption.OnLoad;
                imagem.DecodePixelWidth = 192;
                imagem.UriSource = new Uri(Persistencia.CaminhoDaFoto(fotoNome));
                imagem.EndInit();
                imagem.Freeze();

                elipseFoto.Fill = new ImageBrush(imagem) { Stretch = Stretch.UniformToFill };
            }
            catch
            {
                elipseFoto.Fill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE8, 0xEE));
            }
        }

        private static void Mostrar(TextBlock campo, string texto, bool sucesso)
        {
            campo.Text = texto;
            campo.Foreground = sucesso
                ? (Brush)Application.Current.FindResource("Sucesso")
                : (Brush)Application.Current.FindResource("Perigo");
            campo.Visibility = Visibility.Visible;
        }
    }
}
