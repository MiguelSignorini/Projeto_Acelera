using System;
using System.Globalization;
using System.Windows.Data;

namespace SaciGestao.Converters
{
    // Mostra nomes de enum com acento e espaço (Locacao -> Locação, EmAtendimento -> Em Atendimento)
    public class TextoAmigavelConverter : IValueConverter
    {
        public object Convert(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            return valor?.ToString() switch
            {
                "Locacao" => "Locação",
                "EmAtendimento" => "Em Atendimento",
                var texto => texto ?? string.Empty
            };
        }

        public object ConvertBack(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            return Binding.DoNothing;
        }
    }
}
