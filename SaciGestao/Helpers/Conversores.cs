using System;
using System.Globalization;
using System.Windows.Data;

namespace SaciGestao.Helpers
{
    // true quando os dois valores são iguais (usado para marcar o botão de período ativo)
    public class IguaisConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values.Length == 2 && Equals(values[0]?.ToString(), values[1]?.ToString());

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Texto vazio -> Collapsed; com texto -> Visible (mensagens de erro e resumos)
    public class TextoParaVisibilidadeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string)
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
