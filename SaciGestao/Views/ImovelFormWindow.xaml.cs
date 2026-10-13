using System;
using System.Globalization;
using System.Windows;
using SaciGestao.Models;

namespace SaciGestao.Views
{
    public partial class ImovelFormWindow : Window
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");
        private readonly Imovel _imovel;

        public ImovelFormWindow(Imovel imovel, string titulo)
        {
            InitializeComponent();

            Owner = Application.Current.MainWindow;
            Title = titulo;
            txtCabecalho.Text = titulo;
            _imovel = imovel;

            cbTipo.ItemsSource = Enum.GetValues(typeof(TipoImovel));
            cbFinalidade.ItemsSource = Enum.GetValues(typeof(FinalidadeImovel));
            cbStatus.ItemsSource = Enum.GetValues(typeof(StatusImovel));

            cbTipo.SelectedItem = imovel.Tipo;
            cbFinalidade.SelectedItem = imovel.Finalidade;
            cbStatus.SelectedItem = imovel.Status;

            txtTitulo.Text = imovel.Titulo;
            txtEndereco.Text = imovel.Endereco;
            txtBairro.Text = imovel.Bairro;
            txtValor.Text = imovel.Valor > 0 ? imovel.Valor.ToString("N2", PtBr) : string.Empty;
            txtQuartos.Text = imovel.Quartos.ToString();
            txtVagas.Text = imovel.Vagas.ToString();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text) ||
                string.IsNullOrWhiteSpace(txtEndereco.Text) ||
                string.IsNullOrWhiteSpace(txtBairro.Text))
            {
                MostrarErro("Preencha título, endereço e bairro.");
                return;
            }

            if (!decimal.TryParse(txtValor.Text, NumberStyles.Number, PtBr, out decimal valor) || valor <= 0)
            {
                MostrarErro("Informe um valor maior que zero. Exemplo: 450.000,00");
                return;
            }

            if (!int.TryParse(txtQuartos.Text, out int quartos) || quartos < 0 ||
                !int.TryParse(txtVagas.Text, out int vagas) || vagas < 0)
            {
                MostrarErro("Quartos e vagas devem ser números inteiros (0 ou mais).");
                return;
            }

            _imovel.Titulo = txtTitulo.Text.Trim();
            _imovel.Endereco = txtEndereco.Text.Trim();
            _imovel.Bairro = txtBairro.Text.Trim();
            _imovel.Tipo = (TipoImovel)(cbTipo.SelectedItem ?? _imovel.Tipo);
            _imovel.Finalidade = (FinalidadeImovel)(cbFinalidade.SelectedItem ?? _imovel.Finalidade);
            _imovel.Status = (StatusImovel)(cbStatus.SelectedItem ?? _imovel.Status);
            _imovel.Valor = valor;
            _imovel.Quartos = quartos;
            _imovel.Vagas = vagas;

            DialogResult = true;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void MostrarErro(string mensagem)
        {
            txtErro.Text = mensagem;
            txtErro.Visibility = Visibility.Visible;
        }
    }
}