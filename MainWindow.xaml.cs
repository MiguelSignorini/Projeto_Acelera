using System.Windows;
using System.Windows.Controls;
using SaciGestao.Views;

namespace SaciGestao
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Abre direto em Imóveis  enquanto o Dashboard não fica pronto
            Loaded += (s, e) => rbImoveis.IsChecked = true;
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            if (AreaConteudo == null) return;

            var item = sender as RadioButton;

            switch (item?.Name)
            {
                case "rbImoveis":
                    AreaConteudo.Content = new ImoveisView();
                    break;

                //colegas termine as telas , é só trocar aqui:
                // case "rbClientes":  AreaConteudo.Content = new ClientesView();  break;
                // case "rbContratos": AreaConteudo.Content = new ContratosView(); break;

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

        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
