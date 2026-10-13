using System.Windows;
using SaciGestao.Views;

namespace SaciGestao
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // O sistema sempre começa pela tela de login
            var login = new LoginWindow();
            login.Show();
        }
    }
}
