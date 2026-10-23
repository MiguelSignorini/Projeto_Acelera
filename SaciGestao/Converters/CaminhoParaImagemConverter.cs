using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SaciGestao.Converters
{
    // Converte o caminho de um arquivo de foto em imagem. Carrega redimensionada
    // (ConverterParameter = largura em pixels) e sem travar o arquivo no disco.
    public class CaminhoParaImagemConverter : IValueConverter
    {
        public object Convert(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            if (valor is not string nome || !File.Exists(SaciGestao.Service.Persistencia.CaminhoDaFoto(nome)))
                return DependencyProperty.UnsetValue;

            try
            {
                int largura = int.TryParse(parametro?.ToString(), out int l) ? l : 900;

                var imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.CacheOption = BitmapCacheOption.OnLoad;
                imagem.DecodePixelWidth = largura;
                imagem.UriSource = new Uri(SaciGestao.Service.Persistencia.CaminhoDaFoto(nome));
                imagem.EndInit();
                imagem.Freeze();
                return imagem;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            return Binding.DoNothing;
        }
    }
}
