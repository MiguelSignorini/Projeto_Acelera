using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SaciGestao.Views
{
    // Perigo = confirmar ação destrutiva (botão vermelho); Erro = algo deu errado (só avisa)
    public enum TipoDialogo { Informacao, Sucesso, Atencao, Perigo, Erro }

    // Janela de aviso/confirmação no visual do sistema (substitui a caixa padrão do Windows).
    //   Dialogo.Avisar("PDF gerado", "Contrato salvo.", TipoDialogo.Sucesso);
    //   if (Dialogo.Confirmar("Excluir imóvel", "Essa ação não pode ser desfeita.", "Excluir")) ...
    public partial class Dialogo : Window
    {
        // Índice do botão clicado (0 = ação principal); -1 = fechou com Esc / sem escolher
        public int Resposta { get; private set; } = -1;

        private Dialogo()
        {
            InitializeComponent();
        }

        // ---------- Atalhos ----------

        // Aviso com um botão só ("Entendi")
        public static void Avisar(string titulo, string mensagem, TipoDialogo tipo = TipoDialogo.Informacao, string? detalhe = null)
            => Mostrar(titulo, mensagem, tipo, detalhe, "Entendi");

        // Pergunta Sim/Não: devolve true só se o usuário clicar no botão de confirmar
        public static bool Confirmar(string titulo, string mensagem, string textoConfirmar,
                                     TipoDialogo tipo = TipoDialogo.Perigo, string textoCancelar = "Cancelar")
            => Mostrar(titulo, mensagem, tipo, null, textoConfirmar, textoCancelar) == 0;

        // botoes[0] = ação principal (fica à direita, em destaque); os demais ficam à esquerda dele, com contorno
        public static int Mostrar(string titulo, string mensagem, TipoDialogo tipo, string? detalhe, params string[] botoes)
        {
            var janela = new Dialogo();
            janela.Montar(titulo, mensagem, tipo, detalhe, botoes.Length > 0 ? botoes : new[] { "OK" });

            var dono = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != janela)
                       ?? Application.Current?.MainWindow;
            if (dono != null && dono.IsVisible)
                janela.Owner = dono;
            else
                janela.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            janela.ShowDialog();
            return janela.Resposta;
        }

        private void Montar(string titulo, string mensagem, TipoDialogo tipo, string? detalhe, string[] botoes)
        {
            Title = titulo;
            txtTitulo.Text = titulo;
            txtMensagem.Text = mensagem;
            txtMensagem.Visibility = string.IsNullOrWhiteSpace(mensagem) ? Visibility.Collapsed : Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(detalhe))
            {
                txtDetalhe.Text = detalhe;
                caixaDetalhe.Visibility = Visibility.Visible;
            }

            // Ícone e cores de cada tipo (mesmas cores de estado do Tema.xaml)
            (string icone, string cor, string fundo) = tipo switch
            {
                TipoDialogo.Sucesso => ("", "Sucesso", "SucessoClaro"),
                TipoDialogo.Atencao => ("", "Atencao", "AtencaoClara"),
                TipoDialogo.Perigo => ("", "Perigo", "PerigoClaro"),
                TipoDialogo.Erro => ("", "Perigo", "PerigoClaro"),
                _ => ("", "Primaria", "PrimariaClara")
            };
            txtIcone.Text = icone;
            txtIcone.Foreground = (Brush)FindResource(cor);
            fundoIcone.Background = (Brush)FindResource(fundo);

            // Monta da esquerda para a direita: secundários primeiro, principal por último
            for (int i = botoes.Length - 1; i >= 0; i--)
            {
                int indice = i;
                bool principal = i == 0;
                var botao = new Button
                {
                    Content = botoes[i],
                    MinWidth = 110,
                    Margin = new Thickness(i == botoes.Length - 1 ? 0 : 10, 0, 0, 0),
                    Style = (Style)FindResource(principal
                        ? (tipo == TipoDialogo.Perigo ? "BotaoPerigo" : "BotaoPrimario")
                        : "BotaoContorno"),
                    IsDefault = principal // Enter = ação principal; Esc fecha sem escolher (Janela_PreviewKeyDown)
                };
                botao.Click += (s, e) => { Resposta = indice; Close(); };
                painelBotoes.Children.Add(botao);
            }
        }

        private void Janela_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Resposta = -1;
                Close();
                e.Handled = true;
            }
        }

        // Arrastar a janela pelo cartão
        private void Cartao_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not TextBox)
            {
                try { DragMove(); } catch (InvalidOperationException) { }
            }
        }
    }
}
