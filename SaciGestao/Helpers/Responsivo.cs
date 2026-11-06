using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SaciGestao.Helpers
{
    // Propriedade herdada usada pelo menu lateral: quando true, mostra só os ícones (menu compacto)
    public static class Responsivo
    {
        public static readonly DependencyProperty CompactoProperty =
            DependencyProperty.RegisterAttached("Compacto", typeof(bool), typeof(Responsivo),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetCompacto(DependencyObject o) => (bool)o.GetValue(CompactoProperty);
        public static void SetCompacto(DependencyObject o, bool v) => o.SetValue(CompactoProperty, v);
    }

    // Menu compacto: ícone centralizado; menu normal: alinhado à esquerda
    public class CompactoParaAlinhamentoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? HorizontalAlignment.Center : HorizontalAlignment.Left;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
