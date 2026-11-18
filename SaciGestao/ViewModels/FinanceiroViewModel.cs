using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    // Uma barra do gráfico "Recebido por mês"
    public class BarraMes
    {
        public string Rotulo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public double Percentual { get; set; }
        public string ValorTexto => Valor.ToString("C2", new CultureInfo("pt-BR"));
    }

    public class FinanceiroViewModel : ViewModelBase
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");
        private const string Todos = "Todos";

        private List<MovimentacaoFinanceira> _todas = new();

        // ============================================================
        // TABELA E INDICADORES (sempre refletem os filtros aplicados)
        // ============================================================
        public ObservableCollection<MovimentacaoFinanceira> Movimentacoes { get; } = new();
        public ObservableCollection<BarraMes> Meses { get; } = new();

        private decimal _receitaNaConta;
        public decimal ReceitaNaConta { get => _receitaNaConta; private set => SetProperty(ref _receitaNaConta, value); }

        private decimal _aReceber;
        public decimal AReceber { get => _aReceber; private set => SetProperty(ref _aReceber, value); }

        private decimal _ganhoEmpresa;
        public decimal GanhoEmpresa { get => _ganhoEmpresa; private set => SetProperty(ref _ganhoEmpresa, value); }

        private decimal _repasse;
        public decimal Repasse { get => _repasse; private set => SetProperty(ref _repasse, value); }

        private decimal _totalLocacao;
        public decimal TotalLocacao { get => _totalLocacao; private set => SetProperty(ref _totalLocacao, value); }

        private decimal _totalVenda;
        public decimal TotalVenda { get => _totalVenda; private set => SetProperty(ref _totalVenda, value); }

        private int _qtdPendentes;
        public int QuantidadePendentes { get => _qtdPendentes; private set => SetProperty(ref _qtdPendentes, value); }

        private int _qtdAtrasados;
        public int QuantidadeAtrasados { get => _qtdAtrasados; private set => SetProperty(ref _qtdAtrasados, value); }

        public int QuantidadeMovimentacoes => Movimentacoes.Count;
        public bool SemMovimentacoes => Movimentacoes.Count == 0;
        public bool SemMeses => Meses.Count == 0;

        public string DetalhePendentes => QuantidadePendentes == 0
            ? "Nenhum pagamento pendente"
            : QuantidadePendentes + (QuantidadePendentes == 1 ? " pagamento pendente" : " pagamentos pendentes")
              + (QuantidadeAtrasados > 0 ? " • " + QuantidadeAtrasados + " atrasado(s)" : string.Empty);

        // ============================================================
        // CONTRATOS ATIVOS E POP-UP DE DETALHES
        // ============================================================
        public ObservableCollection<ContratoFinanceiro> ContratosAtivos { get; } = new();
        public int QuantidadeContratosAtivos => ContratosAtivos.Count;
        public bool SemContratosAtivos => ContratosAtivos.Count == 0;

        private bool _detalheAberto;
        public bool DetalheAberto { get => _detalheAberto; private set => SetProperty(ref _detalheAberto, value); }

        private ContratoFinanceiro? _detalheContrato;
        public ContratoFinanceiro? DetalheContrato { get => _detalheContrato; private set => SetProperty(ref _detalheContrato, value); }

        // ============================================================
        // FILTROS
        // ============================================================
        public string[] Tipos { get; } = { Todos, "Locação", "Venda" };
        public string[] SituacoesPagamento { get; } = { Todos, "Pendente", "Pago" };

        private DateTime? _dataInicial;
        public DateTime? DataInicial { get => _dataInicial; set { if (SetProperty(ref _dataInicial, value)) AoAlterarFiltro(); } }

        private DateTime? _dataFinal;
        public DateTime? DataFinal { get => _dataFinal; set { if (SetProperty(ref _dataFinal, value)) AoAlterarFiltro(); } }

        private string _tipoSelecionado = Todos;
        public string TipoSelecionado { get => _tipoSelecionado; set { if (value != null && SetProperty(ref _tipoSelecionado, value)) AoAlterarFiltro(); } }

        private string _situacaoSelecionada = Todos;
        public string SituacaoSelecionada { get => _situacaoSelecionada; set { if (value != null && SetProperty(ref _situacaoSelecionada, value)) AoAlterarFiltro(); } }

        private string _busca = string.Empty;
        public string Busca { get => _busca; set { if (SetProperty(ref _busca, value ?? string.Empty)) AoAlterarFiltro(); } }

        private string _periodoAtivo = "tudo";
        public string PeriodoAtivo { get => _periodoAtivo; private set => SetProperty(ref _periodoAtivo, value); }

        private bool _aplicandoPeriodo;

        // Mexer nas datas à mão desmarca o atalho de período ("Este mês" etc.)
        private void AoAlterarFiltro()
        {
            if (!_aplicandoPeriodo && PeriodoAtivo != "personalizado" && (_dataInicial != null || _dataFinal != null))
                PeriodoAtivo = "personalizado";
            Filtrar();
        }

        // ============================================================
        // DIÁLOGO "REGISTRAR PAGAMENTO"
        // ============================================================
        private bool _dialogoAberto;
        public bool DialogoAberto { get => _dialogoAberto; private set => SetProperty(ref _dialogoAberto, value); }

        public ObservableCollection<Contrato> ContratosDisponiveis { get; } = new();
        public ObservableCollection<MovimentacaoFinanceira> ParcelasDisponiveis { get; } = new();

        private Contrato? _contratoSelecionado;
        public Contrato? ContratoSelecionado
        {
            get => _contratoSelecionado;
            set
            {
                if (!SetProperty(ref _contratoSelecionado, value)) return;
                CarregarParcelas();
                OnPropertyChanged(nameof(ContratoEhLocacao));
            }
        }

        private MovimentacaoFinanceira? _parcelaSelecionada;
        public MovimentacaoFinanceira? ParcelaSelecionada
        {
            get => _parcelaSelecionada;
            set
            {
                if (!SetProperty(ref _parcelaSelecionada, value)) return;
                if (value != null) ValorForm = value.ValorPrevisto.ToString("N2", PtBr);
                OnPropertyChanged(nameof(ResumoEmpresa));
            }
        }

        private string _valorForm = string.Empty;
        public string ValorForm { get => _valorForm; set { if (SetProperty(ref _valorForm, value)) OnPropertyChanged(nameof(ResumoEmpresa)); } }

        private DateTime _dataPagamentoForm = DateTime.Today;
        public DateTime DataPagamentoForm { get => _dataPagamentoForm; set => SetProperty(ref _dataPagamentoForm, value); }

        private bool _confirmadoForm;
        public bool ConfirmadoForm { get => _confirmadoForm; set => SetProperty(ref _confirmadoForm, value); }

        private string _observacaoForm = string.Empty;
        public string ObservacaoForm { get => _observacaoForm; set => SetProperty(ref _observacaoForm, value); }

        private string _erroForm = string.Empty;
        public string ErroForm { get => _erroForm; private set => SetProperty(ref _erroForm, value); }

        public bool ContratoEhLocacao => ContratoSelecionado?.Tipo == TipoContrato.Locacao;

        // Quanto fica para a empresa e quanto é repassado ao proprietário com o valor digitado
        public string ResumoEmpresa
        {
            get
            {
                if (ContratoSelecionado == null) return string.Empty;
                if (!decimal.TryParse(ValorForm, NumberStyles.Number, PtBr, out decimal valor) || valor <= 0) return string.Empty;

                decimal pct = FinanceiroService.Instancia.PercentualDoImovel(ContratoSelecionado.ImovelId);
                decimal empresa = Math.Round(valor * pct / 100m, 2);
                return $"Empresa ({pct.ToString("0.##", PtBr)}%): {empresa.ToString("C2", PtBr)}   •   Repasse ao proprietário: {(valor - empresa).ToString("C2", PtBr)}";
            }
        }

        // ============================================================
        // COMANDOS
        // ============================================================
        public ICommand AbrirRegistrarCommand { get; }
        public ICommand FecharDialogoCommand { get; }
        public ICommand ConfirmarRegistroCommand { get; }
        public ICommand DesfazerCommand { get; }
        public ICommand LimparFiltrosCommand { get; }
        public ICommand PeriodoCommand { get; }
        public ICommand AbrirDetalheCommand { get; }
        public ICommand FecharDetalheCommand { get; }
        public ICommand RegistrarDoDetalheCommand { get; }

        public FinanceiroViewModel()
        {
            AbrirRegistrarCommand = new RelayCommand(p => AbrirDialogo(p as MovimentacaoFinanceira));
            FecharDialogoCommand = new RelayCommand(_ => DialogoAberto = false);
            ConfirmarRegistroCommand = new RelayCommand(_ => ConfirmarRegistro());
            DesfazerCommand = new RelayCommand(p => Desfazer(p as MovimentacaoFinanceira));
            LimparFiltrosCommand = new RelayCommand(_ => LimparFiltros());
            PeriodoCommand = new RelayCommand(p => AplicarPeriodo(p as string ?? "tudo"));
            AbrirDetalheCommand = new RelayCommand(p => AbrirDetalhe(p as ContratoFinanceiro));
            FecharDetalheCommand = new RelayCommand(_ => DetalheAberto = false);
            RegistrarDoDetalheCommand = new RelayCommand(_ => RegistrarDoDetalhe());

            Recarregar();
        }

        // ============================================================
        // CARGA E FILTRO
        // ============================================================
        public void Recarregar()
        {
            _todas = FinanceiroService.Instancia.ObterMovimentacoes();
            Filtrar();
            CarregarContratosAtivos();
        }

        // Lista de contratos ativos; se o pop-up está aberto, atualiza também o contrato que ele mostra
        private void CarregarContratosAtivos()
        {
            ContratosAtivos.Clear();
            foreach (var c in FinanceiroService.Instancia.ObterContratosFinanceiro())
                ContratosAtivos.Add(c);

            OnPropertyChanged(nameof(QuantidadeContratosAtivos));
            OnPropertyChanged(nameof(SemContratosAtivos));

            if (DetalheAberto && DetalheContrato != null)
            {
                DetalheContrato = ContratosAtivos.FirstOrDefault(c => c.ContratoId == DetalheContrato.ContratoId);
                if (DetalheContrato == null) DetalheAberto = false;
            }
        }

        private void AbrirDetalhe(ContratoFinanceiro? contrato)
        {
            if (contrato == null) return;
            DetalheContrato = contrato;
            DetalheAberto = true;
        }

        // Botão do pop-up: abre o "Registrar pagamento" já na próxima parcela pendente do contrato
        private void RegistrarDoDetalhe()
        {
            var contrato = DetalheContrato;
            if (contrato == null) return;

            var proxima = FinanceiroService.Instancia.ParcelasPendentes(contrato.ContratoId).FirstOrDefault();
            DetalheAberto = false;
            AbrirDialogo(proxima ?? new MovimentacaoFinanceira { ContratoId = contrato.ContratoId });
        }

        private void Filtrar()
        {
            IEnumerable<MovimentacaoFinanceira> q = _todas;

            if (DataInicial.HasValue) q = q.Where(m => m.Data >= DataInicial.Value.Date);
            if (DataFinal.HasValue) q = q.Where(m => m.Data <= DataFinal.Value.Date);

            if (TipoSelecionado == "Locação") q = q.Where(m => m.EhLocacao);
            else if (TipoSelecionado == "Venda") q = q.Where(m => !m.EhLocacao);

            q = SituacaoSelecionada switch
            {
                "Pendente" => q.Where(m => !m.Pago),
                "Pago" => q.Where(m => m.Pago),
                _ => q
            };

            string termo = (Busca ?? string.Empty).Trim();
            if (termo.Length > 0)
                q = q.Where(m => m.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                                 m.ContratoCodigo.Contains(termo, StringComparison.OrdinalIgnoreCase));

            var lista = q.ToList();

            Movimentacoes.Clear();
            foreach (var m in lista) Movimentacoes.Add(m);

            // Indicadores: sempre do que está filtrado na tela
            var naConta = lista.Where(m => m.NaConta).ToList();
            ReceitaNaConta = naConta.Sum(m => m.Valor);
            GanhoEmpresa = naConta.Sum(m => m.ValorEmpresa);
            Repasse = naConta.Sum(m => m.ValorRepasse);
            TotalLocacao = naConta.Where(m => m.EhLocacao).Sum(m => m.Valor);
            TotalVenda = naConta.Where(m => !m.EhLocacao).Sum(m => m.Valor);
            AReceber = lista.Where(m => !m.Pago).Sum(m => m.ValorPrevisto);
            QuantidadePendentes = lista.Count(m => !m.Pago);
            QuantidadeAtrasados = lista.Count(m => m.Atrasado);

            MontarGrafico(naConta);

            OnPropertyChanged(nameof(QuantidadeMovimentacoes));
            OnPropertyChanged(nameof(SemMovimentacoes));
            OnPropertyChanged(nameof(DetalhePendentes));
        }

        // Barras com o que entrou na conta nos últimos 6 meses que têm movimento
        private void MontarGrafico(List<MovimentacaoFinanceira> naConta)
        {
            var grupos = naConta
                .GroupBy(m => new DateTime(m.Data.Year, m.Data.Month, 1))
                .OrderByDescending(g => g.Key)
                .Take(6)
                .OrderBy(g => g.Key)
                .Select(g => (Mes: g.Key, Total: g.Sum(m => m.Valor)))
                .ToList();

            decimal maior = grupos.Count == 0 ? 0 : grupos.Max(g => g.Total);

            Meses.Clear();
            foreach (var g in grupos)
                Meses.Add(new BarraMes
                {
                    Rotulo = g.Mes.ToString("MMM/yyyy", PtBr),
                    Valor = g.Total,
                    Percentual = maior > 0 ? (double)(g.Total / maior) * 100 : 0
                });

            OnPropertyChanged(nameof(SemMeses));
        }

        // ---------- Atalhos de período ----------
        private void AplicarPeriodo(string periodo)
        {
            var hoje = DateTime.Today;
            DateTime? ini = null, fim = null;

            switch (periodo)
            {
                case "mes":
                    ini = new DateTime(hoje.Year, hoje.Month, 1);
                    fim = ini.Value.AddMonths(1).AddDays(-1);
                    break;
                case "mesAnterior":
                    ini = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-1);
                    fim = ini.Value.AddMonths(1).AddDays(-1);
                    break;
                case "30dias":
                    ini = hoje.AddDays(-29);
                    fim = hoje;
                    break;
                case "ano":
                    ini = new DateTime(hoje.Year, 1, 1);
                    fim = new DateTime(hoje.Year, 12, 31);
                    break;
            }

            _aplicandoPeriodo = true;
            PeriodoAtivo = periodo;
            _dataInicial = ini; OnPropertyChanged(nameof(DataInicial));
            _dataFinal = fim; OnPropertyChanged(nameof(DataFinal));
            _aplicandoPeriodo = false;

            Filtrar();
        }

        private void LimparFiltros()
        {
            _tipoSelecionado = Todos; OnPropertyChanged(nameof(TipoSelecionado));
            _situacaoSelecionada = Todos; OnPropertyChanged(nameof(SituacaoSelecionada));
            _busca = string.Empty; OnPropertyChanged(nameof(Busca));
            AplicarPeriodo("tudo");
        }

        // ============================================================
        // REGISTRAR / DESFAZER
        // ============================================================
        private void AbrirDialogo(MovimentacaoFinanceira? linha)
        {
            ContratosDisponiveis.Clear();
            foreach (var c in FinanceiroService.Instancia.ContratosParaPagamento())
                ContratosDisponiveis.Add(c);

            ErroForm = string.Empty;
            ConfirmadoForm = false;
            ObservacaoForm = string.Empty;
            DataPagamentoForm = DateTime.Today;
            ValorForm = string.Empty;

            // Vindo do botão "Registrar" de uma linha: já abre no contrato e na parcela certos
            _contratoSelecionado = linha == null
                ? ContratosDisponiveis.FirstOrDefault()
                : ContratosDisponiveis.FirstOrDefault(c => c.Id == linha.ContratoId);
            OnPropertyChanged(nameof(ContratoSelecionado));
            OnPropertyChanged(nameof(ContratoEhLocacao));
            CarregarParcelas(linha);

            DialogoAberto = true;
        }

        private void CarregarParcelas(MovimentacaoFinanceira? preferida = null)
        {
            ParcelasDisponiveis.Clear();
            _parcelaSelecionada = null;

            if (ContratoSelecionado != null)
            {
                foreach (var p in FinanceiroService.Instancia.ParcelasPendentes(ContratoSelecionado.Id))
                    ParcelasDisponiveis.Add(p);

                // A parcela pedida (botão "Registrar" da linha); se não houver, a pendente mais antiga
                _parcelaSelecionada = (preferida == null ? null : ParcelasDisponiveis.FirstOrDefault(p => p.Competencia == preferida.Competencia))
                                      ?? ParcelasDisponiveis.FirstOrDefault();
            }

            OnPropertyChanged(nameof(ParcelaSelecionada));
            ValorForm = _parcelaSelecionada?.ValorPrevisto.ToString("N2", PtBr) ?? string.Empty;
            OnPropertyChanged(nameof(ResumoEmpresa));
        }

        private void ConfirmarRegistro()
        {
            if (ContratoSelecionado == null) { ErroForm = "Selecione o contrato."; return; }
            if (ParcelaSelecionada == null) { ErroForm = "Este contrato não tem pagamento pendente."; return; }

            if (!decimal.TryParse(ValorForm, NumberStyles.Number, PtBr, out decimal valor) || valor <= 0)
            {
                ErroForm = "Informe o valor recebido. Exemplo: 3.200,00";
                return;
            }

            // Aceita pagamento parcial e com juros/multa, mas barra valor fora de proporção (erro de digitação)
            if (valor > ParcelaSelecionada.ValorPrevisto * 2)
            {
                ErroForm = $"O valor recebido ({valor.ToString("C2", PtBr)}) é mais que o dobro da parcela " +
                           $"({ParcelaSelecionada.ValorPrevisto.ToString("C2", PtBr)}). Confira o valor digitado.";
                return;
            }

            if (DataPagamentoForm.Date > DateTime.Today)
            {
                ErroForm = "A data do pagamento não pode ser no futuro.";
                return;
            }

            if (!ConfirmadoForm)
            {
                ErroForm = "Marque a confirmação de que o pagamento foi recebido. Sem ela o pagamento continua pendente.";
                return;
            }

            FinanceiroService.Instancia.Registrar(new Pagamento
            {
                ContratoId = ContratoSelecionado.Id,
                Competencia = ParcelaSelecionada.Competencia,
                ValorRecebido = valor,
                DataPagamento = DataPagamentoForm.Date,
                // Registrar = valor recebido na conta (locação e venda entram nos totais na hora)
                PercentualEmpresa = FinanceiroService.Instancia.PercentualDoImovel(ContratoSelecionado.ImovelId),
                Observacao = ObservacaoForm.Trim()
            });

            DialogoAberto = false;
            Recarregar();
        }

        private void Desfazer(MovimentacaoFinanceira? linha)
        {
            if (linha == null || !linha.Pago) return;

            if (!Views.Dialogo.Confirmar("Desfazer pagamento",
                    $"{linha.Descricao}\n{linha.ValorTexto}\n\nO pagamento volta a ficar como Pendente.",
                    "Desfazer", Views.TipoDialogo.Atencao))
                return;

            FinanceiroService.Instancia.Desfazer(linha.PagamentoId);
            Recarregar();
        }
    }
}
