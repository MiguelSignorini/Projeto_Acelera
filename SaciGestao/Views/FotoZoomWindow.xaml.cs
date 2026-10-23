using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SaciGestao.Service;

namespace SaciGestao.Views
{
    // Popup para ver a foto do imóvel em tamanho grande, com zoom e movimentação
    public partial class FotoZoomWindow : Window
    {
        private const double ZoomMinimo = 1.0;
        private const double ZoomMaximo = 8.0;
        private const double ZoomDuploClique = 2.5;

        private readonly IList<string> _fotos;
        private double _zoom = 1.0;

        // Arrastar para mover a foto ampliada
        private bool _arrastando;
        private Point _origemMouse;
        private double _origemHorizontal;
        private double _origemVertical;

        // Foto em que o usuário parou (a galeria da tela de detalhes continua nela)
        public int IndiceAtual { get; private set; }

        public FotoZoomWindow(IList<string> fotos, int indiceInicial)
        {
            InitializeComponent();

            _fotos = fotos;
            IndiceAtual = Math.Clamp(indiceInicial, 0, Math.Max(0, fotos.Count - 1));

            // Tamanho de popup: moderado e sempre dentro da área útil da tela, centralizado nela
            var area = SystemParameters.WorkArea;
            Width = Math.Min(980, area.Width * 0.7);
            Height = Math.Min(680, area.Height * 0.78);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Loaded += (s, e) => CarregarFoto();
        }

        // Faz o popup cobrir exatamente a área de conteúdo do sistema (à direita do menu, abaixo da barra superior)
        public void AjustarA(FrameworkElement area)
        {
            var fonte = PresentationSource.FromVisual(area);
            if (fonte?.CompositionTarget == null || area.ActualWidth <= 0) return;

            var topoEsquerdo = fonte.CompositionTarget.TransformFromDevice.Transform(area.PointToScreen(new Point(0, 0)));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = topoEsquerdo.X;
            Top = topoEsquerdo.Y;
            Width = area.ActualWidth;
            Height = area.ActualHeight;
        }

        // ---------- Foto atual ----------
        private void CarregarFoto()
        {
            string caminho = Persistencia.CaminhoDaFoto(_fotos[IndiceAtual]);

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad; // não trava o arquivo no disco
                bmp.DecodePixelWidth = 3000;                // boa qualidade para o zoom, sem estourar a memória
                bmp.UriSource = new Uri(caminho);
                bmp.EndInit();
                bmp.Freeze();
                imagem.Source = File.Exists(caminho) ? bmp : null;
            }
            catch
            {
                imagem.Source = null;
            }

            txtContador.Text = "Foto " + (IndiceAtual + 1) + " de " + _fotos.Count;
            btnAnterior.Visibility = btnProxima.Visibility = _fotos.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            DefinirZoom(1.0, centralizar: true);
        }

        private void IrParaFoto(int passo)
        {
            if (_fotos.Count < 2) return;
            IndiceAtual = (IndiceAtual + passo + _fotos.Count) % _fotos.Count;
            CarregarFoto();
        }

        // ---------- Zoom ----------
        // A imagem é desenhada no tamanho da área visível x zoom (Stretch = Uniform, nunca distorce).
        private void AplicarTamanho()
        {
            imagem.Width = Math.Max(1, rolagem.ViewportWidth * _zoom);
            imagem.Height = Math.Max(1, rolagem.ViewportHeight * _zoom);
            txtZoom.Text = Math.Round(_zoom * 100) + "%";
            rolagem.Cursor = _zoom > 1.0 ? Cursors.SizeAll : Cursors.Arrow;
        }

        // Muda o zoom mantendo o ponto indicado (coordenadas da área visível) parado na tela
        private void DefinirZoom(double novoZoom, bool centralizar = false, Point? ponto = null)
        {
            novoZoom = Math.Clamp(novoZoom, ZoomMinimo, ZoomMaximo);

            var p = ponto ?? new Point(rolagem.ViewportWidth / 2, rolagem.ViewportHeight / 2);
            double zoomAntes = _zoom;
            double conteudoX = (rolagem.HorizontalOffset + p.X) / zoomAntes;
            double conteudoY = (rolagem.VerticalOffset + p.Y) / zoomAntes;

            _zoom = novoZoom;
            AplicarTamanho();
            rolagem.UpdateLayout();

            if (centralizar || _zoom <= 1.0)
            {
                rolagem.ScrollToHorizontalOffset(0);
                rolagem.ScrollToVerticalOffset(0);
            }
            else
            {
                rolagem.ScrollToHorizontalOffset(conteudoX * _zoom - p.X);
                rolagem.ScrollToVerticalOffset(conteudoY * _zoom - p.Y);
            }
        }

        private void Rolagem_SizeChanged(object sender, SizeChangedEventArgs e) => AplicarTamanho();

        private void Rolagem_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double fator = e.Delta > 0 ? 1.2 : 1 / 1.2;
            DefinirZoom(_zoom * fator, ponto: e.GetPosition(rolagem));
            e.Handled = true;
        }

        private void ZoomMais_Click(object sender, RoutedEventArgs e) => DefinirZoom(_zoom * 1.4);
        private void ZoomMenos_Click(object sender, RoutedEventArgs e) => DefinirZoom(_zoom / 1.4);
        private void Ajustar_Click(object sender, RoutedEventArgs e) => DefinirZoom(1.0, centralizar: true);

        // ---------- Arrastar e duplo clique ----------
        private void Rolagem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (_zoom > 1.0) DefinirZoom(1.0, centralizar: true);
                else DefinirZoom(ZoomDuploClique, ponto: e.GetPosition(rolagem));
                e.Handled = true;
                return;
            }

            if (_zoom <= 1.0) return;

            _arrastando = true;
            _origemMouse = e.GetPosition(rolagem);
            _origemHorizontal = rolagem.HorizontalOffset;
            _origemVertical = rolagem.VerticalOffset;
            rolagem.CaptureMouse();
        }

        private void Rolagem_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_arrastando) return;

            var atual = e.GetPosition(rolagem);
            rolagem.ScrollToHorizontalOffset(_origemHorizontal - (atual.X - _origemMouse.X));
            rolagem.ScrollToVerticalOffset(_origemVertical - (atual.Y - _origemMouse.Y));
        }

        private void Rolagem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _arrastando = false;
            rolagem.ReleaseMouseCapture();
        }

        // ---------- Navegação e teclado ----------
        private void Anterior_Click(object sender, RoutedEventArgs e) => IrParaFoto(-1);
        private void Proxima_Click(object sender, RoutedEventArgs e) => IrParaFoto(1);
        private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: Close(); break;
                case Key.Left: IrParaFoto(-1); break;
                case Key.Right: IrParaFoto(1); break;
                case Key.Add:
                case Key.OemPlus: DefinirZoom(_zoom * 1.4); break;
                case Key.Subtract:
                case Key.OemMinus: DefinirZoom(_zoom / 1.4); break;
                case Key.D0:
                case Key.NumPad0: DefinirZoom(1.0, centralizar: true); break;
                default: return;
            }
            e.Handled = true;
        }
    }
}
