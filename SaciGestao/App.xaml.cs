using System;
using System.Windows;
using SaciGestao.Service;

namespace SaciGestao
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Erro inesperado: grava o detalhe em um arquivo e avisa, em vez de fechar o sistema sem explicação
            DispatcherUnhandledException += (s, ev) =>
            {
                string log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SaciGestao_erro.log");
                try { System.IO.File.AppendAllText(log, DateTime.Now + "\n" + ev.Exception + "\n\n"); } catch { }
                ev.Handled = true;
                try
                {
                    Views.Dialogo.Avisar("Ocorreu um erro inesperado", ev.Exception.Message + "\n\nO sistema continua aberto. Os detalhes técnicos foram gravados em:",
                                         Views.TipoDialogo.Erro, log);
                }
                catch
                {
                    // Se até a janela de aviso falhar, usa a caixa simples do Windows
                    MessageBox.Show("Ocorreu um erro inesperado:\n\n" + ev.Exception.Message, "SaciGestao", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Os services leem os arquivos da pasta Json na primeira vez que são usados.
            // Daqui em diante tudo fica só em memória até o sistema ser fechado.
            ImovelService.Instancia.ToString();
            ClienteService.Instancia.ToString();
            ContratoService.Instancia.ToString();
            UsuarioService.Instancia.ToString();

            // Janela única: começa na tela de login e, ao entrar, mostra o sistema na mesma janela
            new MainWindow().Show();
        }

        // Roda ao fechar o sistema por qualquer caminho (botão Sair, X da janela...)
        protected override void OnExit(ExitEventArgs e)
        {
            SalvarTudo();
            base.OnExit(e);
        }

        // Desligar / sair do Windows com o sistema aberto também salva
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            SalvarTudo();
            base.OnSessionEnding(e);
        }

        private static void SalvarTudo()
        {
            ImovelService.Instancia.Salvar();
            FinanceiroService.Instancia.Salvar();
            ClienteService.Instancia.Salvar();
            ContratoService.Instancia.Salvar();
            UsuarioService.Instancia.Salvar();
        }
    }
}
