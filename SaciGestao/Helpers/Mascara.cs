using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SaciGestao.Helpers
{
    public enum TipoMascara { Nenhuma, Cpf, Rg, Telefone, Cep, Inteiro, Decimal }

    // Propriedade anexada para TextBox: limita o que pode ser digitado e coloca pontos, traços
    // e parênteses automaticamente enquanto a pessoa digita.
    //
    //   <TextBox h:Mascara.Tipo="Cpf"/>      -> 123.123.123-12
    //   <TextBox h:Mascara.Tipo="Rg"/>       -> 12.123.123-2
    //   <TextBox h:Mascara.Tipo="Telefone"/> -> (12) 91234-5678
    //   <TextBox h:Mascara.Tipo="Cep"/>      -> 12630-000
    //   <TextBox h:Mascara.Tipo="Inteiro" MaxLength="3"/> -> só dígitos
    //   <TextBox h:Mascara.Tipo="Decimal"/>  -> dígitos e uma vírgula (até 2 casas)
    public static class Mascara
    {
        public static readonly DependencyProperty TipoProperty =
            DependencyProperty.RegisterAttached("Tipo", typeof(TipoMascara), typeof(Mascara),
                new PropertyMetadata(TipoMascara.Nenhuma, AoMudarTipo));

        public static TipoMascara GetTipo(DependencyObject o) => (TipoMascara)o.GetValue(TipoProperty);
        public static void SetTipo(DependencyObject o, TipoMascara v) => o.SetValue(TipoProperty, v);

        private static readonly Regex _decimal = new(@"^[\d.]*(,\d{0,2})?$", RegexOptions.Compiled);
        private static bool _formatando;

        private static void AoMudarTipo(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox caixa) return;

            caixa.PreviewTextInput -= AoDigitar;
            caixa.TextChanged -= AoMudarTexto;
            DataObject.RemovePastingHandler(caixa, AoColar);

            var tipo = (TipoMascara)e.NewValue;
            if (tipo == TipoMascara.Nenhuma) return;

            caixa.PreviewTextInput += AoDigitar;
            DataObject.AddPastingHandler(caixa, AoColar);

            int tamanho = TamanhoFormatado(tipo);
            if (tamanho > 0) caixa.MaxLength = tamanho;

            if (EhMascaraFixa(tipo))
            {
                caixa.TextChanged += AoMudarTexto;
                Formatar(caixa);
            }
        }

        private static bool EhMascaraFixa(TipoMascara t) =>
            t is TipoMascara.Cpf or TipoMascara.Rg or TipoMascara.Telefone or TipoMascara.Cep;

        // Tamanho máximo do texto já com pontos e traços
        private static int TamanhoFormatado(TipoMascara t) => t switch
        {
            TipoMascara.Cpf => 14,       // 123.123.123-12
            TipoMascara.Rg => 12,        // 12.123.123-2
            TipoMascara.Telefone => 15,  // (12) 91234-5678
            TipoMascara.Cep => 9,        // 12630-000
            _ => 0
        };

        private static void AoDigitar(object sender, TextCompositionEventArgs e)
        {
            var caixa = (TextBox)sender;
            var tipo = GetTipo(caixa);

            if (tipo == TipoMascara.Decimal)
            {
                string novo = caixa.Text.Remove(caixa.SelectionStart, caixa.SelectionLength)
                                        .Insert(caixa.SelectionStart, e.Text);
                e.Handled = !_decimal.IsMatch(novo);
                return;
            }

            // Cpf, Rg, Telefone, Cep e Inteiro: só dígitos
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private static void AoColar(object sender, DataObjectPastingEventArgs e)
        {
            var caixa = (TextBox)sender;
            if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText)) { e.CancelCommand(); return; }

            string texto = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText);
            var tipo = GetTipo(caixa);

            string limpo = tipo == TipoMascara.Decimal
                ? new string(texto.Where(c => char.IsDigit(c) || c == ',').ToArray())
                : new string(texto.Where(char.IsDigit).ToArray());

            e.DataObject = new DataObject(DataFormats.UnicodeText, limpo);
        }

        private static void AoMudarTexto(object sender, TextChangedEventArgs e)
        {
            Formatar((TextBox)sender);
        }

        private static void Formatar(TextBox caixa)
        {
            if (_formatando) return;

            var tipo = GetTipo(caixa);
            string digitos = new string(caixa.Text.Where(char.IsDigit).ToArray());
            string formatado = Aplicar(digitos, tipo);
            if (formatado == caixa.Text) return;

            _formatando = true;
            try
            {
                caixa.Text = formatado;
                caixa.CaretIndex = formatado.Length;
            }
            finally { _formatando = false; }
        }

        // Os separadores só aparecem quando há mais um dígito depois deles
        private static string Aplicar(string digitos, TipoMascara tipo)
        {
            string modelo = tipo switch
            {
                TipoMascara.Cpf => "###.###.###-##",
                TipoMascara.Rg => "##.###.###-#",
                TipoMascara.Cep => "#####-###",
                TipoMascara.Telefone => digitos.Length > 10 ? "(##) #####-####" : "(##) ####-####",
                _ => ""
            };

            int max = modelo.Count(c => c == '#');
            if (digitos.Length > max) digitos = digitos.Substring(0, max);

            var saida = new StringBuilder();
            int posicao = 0;
            foreach (char c in modelo)
            {
                if (posicao >= digitos.Length) break;
                saida.Append(c == '#' ? digitos[posicao++] : c);
            }
            return saida.ToString();
        }
    }
}
