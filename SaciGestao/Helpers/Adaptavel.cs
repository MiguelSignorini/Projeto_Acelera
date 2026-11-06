using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace SaciGestao.Helpers
{
    // ============================================================
    // Layout adaptável: nada de larguras fixas. O número de colunas é calculado a partir da largura
    // que está disponível no momento, então o conteúdo se reorganiza ao redimensionar a janela.
    // ============================================================

    // Painel que divide a largura em colunas iguais e quebra linha sozinho (substitui o UniformGrid).
    // Quantas colunas cabem = largura disponível / LarguraMinima de cada item.
    public class PainelAdaptavel : Panel
    {
        public static readonly DependencyProperty LarguraMinimaProperty =
            DependencyProperty.Register(nameof(LarguraMinima), typeof(double), typeof(PainelAdaptavel),
                new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty EspacamentoVerticalProperty =
            DependencyProperty.Register(nameof(EspacamentoVertical), typeof(double), typeof(PainelAdaptavel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty MaxColunasProperty =
            DependencyProperty.Register(nameof(MaxColunas), typeof(int), typeof(PainelAdaptavel),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double LarguraMinima { get => (double)GetValue(LarguraMinimaProperty); set => SetValue(LarguraMinimaProperty, value); }
        public double EspacamentoVertical { get => (double)GetValue(EspacamentoVerticalProperty); set => SetValue(EspacamentoVerticalProperty, value); }
        // 0 = sem limite (usa quantos itens houver)
        public int MaxColunas { get => (int)GetValue(MaxColunasProperty); set => SetValue(MaxColunasProperty, value); }

        private int Colunas(double largura)
        {
            int itens = Math.Max(1, InternalChildren.Count);
            int max = MaxColunas > 0 ? Math.Min(MaxColunas, itens) : itens;
            if (double.IsInfinity(largura)) return max;
            int c = (int)Math.Floor(largura / Math.Max(1, LarguraMinima));
            return Math.Max(1, Math.Min(max, c));
        }

        protected override Size MeasureOverride(Size disponivel)
        {
            int cols = Colunas(disponivel.Width);
            double larguraCol = double.IsInfinity(disponivel.Width) ? double.PositiveInfinity : disponivel.Width / cols;
            double total = 0;
            double alturaLinha = 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var filho = InternalChildren[i];
                filho.Measure(new Size(larguraCol, double.PositiveInfinity));
                alturaLinha = Math.Max(alturaLinha, filho.DesiredSize.Height);
                bool ultimo = i == InternalChildren.Count - 1;
                if (i % cols == cols - 1 || ultimo)
                {
                    total += alturaLinha + (ultimo ? 0 : EspacamentoVertical);
                    alturaLinha = 0;
                }
            }

            return new Size(double.IsInfinity(disponivel.Width) ? 0 : disponivel.Width, total);
        }

        protected override Size ArrangeOverride(Size final)
        {
            int cols = Colunas(final.Width);
            double larguraCol = final.Width / cols;
            double y = 0;

            for (int inicio = 0; inicio < InternalChildren.Count; inicio += cols)
            {
                int fim = Math.Min(inicio + cols, InternalChildren.Count);
                double altura = 0;
                for (int i = inicio; i < fim; i++) altura = Math.Max(altura, InternalChildren[i].DesiredSize.Height);
                for (int i = inicio; i < fim; i++)
                    InternalChildren[i].Arrange(new Rect((i - inicio) * larguraCol, y, larguraCol, altura));
                y += altura + EspacamentoVertical;
            }
            return final;
        }
    }

    public static class Adaptavel
    {
        // ---------- Grid que reorganiza as colunas ----------
        // Em um Grid com várias colunas lado a lado, LarguraMinima diz quanto cada bloco precisa.
        // Se a largura não comporta todos, os blocos quebram para a linha de baixo (até empilhar em 1 coluna);
        // ao alargar a janela volta ao layout original.
        public static readonly DependencyProperty LarguraMinimaProperty =
            DependencyProperty.RegisterAttached("LarguraMinima", typeof(double), typeof(Adaptavel),
                new PropertyMetadata(0.0, AoMudarLarguraMinima));

        public static double GetLarguraMinima(DependencyObject o) => (double)o.GetValue(LarguraMinimaProperty);
        public static void SetLarguraMinima(DependencyObject o, double v) => o.SetValue(LarguraMinimaProperty, v);

        private class Estado
        {
            public List<(GridLength Largura, double Min, double Max)> Colunas = new();
            public List<(UIElement El, int Linha, int Coluna)> Filhos = new();
            public double Espaco = 12;
            public int ColunasAtuais = -1;
            public bool Elegivel;
        }

        private static readonly ConditionalWeakTable<Grid, Estado> _estados = new();

        private static void AoMudarLarguraMinima(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Grid grid) return;
            grid.SizeChanged -= AoRedimensionar;
            grid.Loaded -= AoCarregar;
            if ((double)e.NewValue > 0)
            {
                grid.SizeChanged += AoRedimensionar;
                grid.Loaded += AoCarregar;
            }
        }

        private static void AoCarregar(object sender, RoutedEventArgs e) => Atualizar((Grid)sender);

        private static void AoRedimensionar(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged) Atualizar((Grid)sender);
        }

        private static Estado ObterEstado(Grid grid)
        {
            if (_estados.TryGetValue(grid, out var existente)) return existente;

            var st = new Estado();
            foreach (var c in grid.ColumnDefinitions) st.Colunas.Add((c.Width, c.MinWidth, c.MaxWidth));

            // Só mexe em grades "simples": sem linhas definidas e com filhos de 1 célula
            st.Elegivel = grid.ColumnDefinitions.Count > 1 && grid.RowDefinitions.Count == 0 &&
                          grid.Children.OfType<UIElement>().All(f => Grid.GetRow(f) == 0 &&
                              Grid.GetColumnSpan(f) == 1 && Grid.GetRowSpan(f) == 1);

            foreach (UIElement f in grid.Children) st.Filhos.Add((f, Grid.GetRow(f), Grid.GetColumn(f)));
            st.Filhos = st.Filhos.OrderBy(f => f.Coluna).ToList();

            var espaco = st.Colunas.Where(c => c.Largura.IsAbsolute && c.Largura.Value <= 40)
                                   .Select(c => c.Largura.Value).ToList();
            if (espaco.Count > 0) st.Espaco = espaco[0];

            _estados.Add(grid, st);
            return st;
        }

        private static void Atualizar(Grid grid)
        {
            var st = ObterEstado(grid);
            if (!st.Elegivel) return;

            double largura = grid.ActualWidth;
            if (largura <= 0) return;

            double minimo = GetLarguraMinima(grid);
            int k = st.Filhos.Count;
            int c = (int)Math.Floor((largura + st.Espaco) / (minimo + st.Espaco));
            c = Math.Max(1, Math.Min(k, c));
            if (c == st.ColunasAtuais) return;
            st.ColunasAtuais = c;

            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            if (c >= k)
            {
                // Layout original
                foreach (var col in st.Colunas)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = col.Largura, MinWidth = col.Min, MaxWidth = col.Max });
                foreach (var f in st.Filhos) { Grid.SetRow(f.El, f.Linha); Grid.SetColumn(f.El, f.Coluna); }
                return;
            }

            // Reorganiza em "c" colunas iguais (com espaço entre elas) e quantas linhas forem precisas
            for (int j = 0; j < c; j++)
            {
                if (j > 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(st.Espaco) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            int linhas = (int)Math.Ceiling(k / (double)c);
            for (int r = 0; r < linhas; r++)
            {
                if (r > 0) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(st.Espaco) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            for (int i = 0; i < k; i++)
            {
                Grid.SetColumn(st.Filhos[i].El, (i % c) * 2);
                Grid.SetRow(st.Filhos[i].El, (i / c) * 2);
            }
        }

        // ---------- Colunas de tabela que somem quando a tabela fica estreita ----------
        // Na tabela: h:Adaptavel.Tabela="True". Em cada DataGridColumn: h:Adaptavel.OcultarAbaixo="900"
        // esconde a coluna quando a tabela tem menos de 900 de largura.
        public static readonly DependencyProperty OcultarAbaixoProperty =
            DependencyProperty.RegisterAttached("OcultarAbaixo", typeof(double), typeof(Adaptavel),
                new PropertyMetadata(0.0));

        public static double GetOcultarAbaixo(DependencyObject o) => (double)o.GetValue(OcultarAbaixoProperty);
        public static void SetOcultarAbaixo(DependencyObject o, double v) => o.SetValue(OcultarAbaixoProperty, v);

        public static readonly DependencyProperty TabelaProperty =
            DependencyProperty.RegisterAttached("Tabela", typeof(bool), typeof(Adaptavel),
                new PropertyMetadata(false, AoMudarTabela));

        public static bool GetTabela(DependencyObject o) => (bool)o.GetValue(TabelaProperty);
        public static void SetTabela(DependencyObject o, bool v) => o.SetValue(TabelaProperty, v);

        private static void AoMudarTabela(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid tabela) return;
            tabela.SizeChanged -= AoRedimensionarTabela;
            tabela.Loaded -= AoCarregarTabela;
            if (e.NewValue is true)
            {
                tabela.SizeChanged += AoRedimensionarTabela;
                tabela.Loaded += AoCarregarTabela;
            }
        }

        private static void AoCarregarTabela(object sender, RoutedEventArgs e) => AjustarTabela((DataGrid)sender);
        private static void AoRedimensionarTabela(object sender, SizeChangedEventArgs e) => AjustarTabela((DataGrid)sender);

        private static readonly ConditionalWeakTable<DataGridColumn, object> _larguras = new();

        private static void AjustarTabela(DataGrid tabela)
        {
            bool mudou = false;
            foreach (var coluna in tabela.Columns)
            {
                double limite = GetOcultarAbaixo(coluna);
                if (limite <= 0) continue;
                var novo = tabela.ActualWidth < limite ? Visibility.Collapsed : Visibility.Visible;
                if (coluna.Visibility != novo) { coluna.Visibility = novo; mudou = true; }
            }

            if (!mudou) return;

            // O DataGrid não recalcula as larguras sozinho quando uma coluna some/aparece: força o recálculo
            tabela.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var coluna in tabela.Columns)
                {
                    if (coluna.Visibility != Visibility.Visible) continue;
                    var original = coluna.Width;
                    coluna.Width = DataGridLength.Auto;
                    coluna.Width = original;
                }
                tabela.UpdateLayout();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
