using System.Windows;
using SaciGestao.Service;
using SaciGestao.Views;

namespace SaciGestao
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Os services leem os arquivos da pasta Json na primeira vez que são usados.
            // Daqui em diante tudo fica só em memória até o sistema ser fechado.
            ImovelService.Instancia.ToString();
            ClienteService.Instancia.ToString();
            ContratoService.Instancia.ToString();
            UsuarioService.Instancia.ToString();

            // O sistema sempre começa pela tela de login
            var login = new LoginWindow();
            login.Show();
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
            ClienteService.Instancia.Salvar();
            ContratoService.Instancia.Salvar();
            UsuarioService.Instancia.Salvar();
        }
    }
}
