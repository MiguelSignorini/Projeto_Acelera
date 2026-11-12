using System;
using System.Globalization;

namespace SaciGestao.Models
{
    // Uma linha da tabela do Financeiro: ou um pagamento já registrado (Pago) ou uma parcela esperada (Pendente).
    // Não é gravada em arquivo; é montada pelo FinanceiroService a partir dos contratos e dos pagamentos.
    public class MovimentacaoFinanceira
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public int PagamentoId { get; set; }          // 0 = ainda pendente (nada registrado)
        public int ContratoId { get; set; }
        public string ContratoCodigo { get; set; } = string.Empty;
        public string Inquilino { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public bool EhLocacao { get; set; }

        public DateTime Competencia { get; set; }
        public DateTime Vencimento { get; set; }
        public DateTime? DataPagamento { get; set; }

        public bool Pago { get; set; }
        public bool CaiuNaConta { get; set; }

        public decimal ValorPrevisto { get; set; }
        public decimal ValorRecebido { get; set; }
        public decimal Percentual { get; set; }

        // Venda parcelada: número desta parcela e total de parcelas (locação e venda à vista = 1/1)
        public int Parcela { get; set; } = 1;
        public int TotalParcelas { get; set; } = 1;

        // ---------- Cálculos ----------
        // Data usada nos filtros e na ordenação: dia do pagamento, se já foi pago; senão o vencimento
        public DateTime Data => Pago && DataPagamento.HasValue ? DataPagamento.Value.Date : Vencimento.Date;

        // Valor que vale na linha: o recebido (se pago) ou o previsto (se pendente)
        public decimal Valor => Pago ? ValorRecebido : ValorPrevisto;

        public decimal ValorEmpresa => Math.Round(Valor * Percentual / 100m, 2);
        public decimal ValorRepasse => Valor - ValorEmpresa;

        // Entra nos totais "na conta": venda paga, ou locação paga com o check "caiu na conta"
        public bool NaConta => Pago && (!EhLocacao || CaiuNaConta);
        public bool AguardandoConta => Pago && EhLocacao && !CaiuNaConta;
        public bool Atrasado => !Pago && Vencimento.Date < DateTime.Today;

        // ---------- Textos para a tela ----------
        public string TipoTexto => EhLocacao ? "Locação" : "Venda";
        public string DataTexto => Data.ToString("dd/MM/yyyy");
        public string ValorTexto => Valor.ToString("C2", PtBr);
        public string EmpresaTexto => ValorEmpresa.ToString("C2", PtBr);
        public string PercentualTexto => Percentual.ToString("0.##", PtBr) + "%";

        public string PagamentoTexto => Pago ? "Pago" : (Atrasado ? "Pendente • atrasado" : "Pendente");
        public string PagamentoDetalhe => Pago
            ? "Recebido em " + (DataPagamento ?? Vencimento).ToString("dd/MM/yyyy")
            : "Vence em " + Vencimento.ToString("dd/MM/yyyy");

        public string ReferenciaTexto => EhLocacao
            ? "Ref. " + Competencia.ToString("MM/yyyy")
            : (TotalParcelas > 1 ? "Parcela " + Parcela + "/" + TotalParcelas : "Venda");

        // Texto usado na lista de parcelas do diálogo "Registrar pagamento"
        public string ParcelaTexto => (EhLocacao ? Competencia.ToString("MM/yyyy")
                : TotalParcelas > 1 ? "Parcela " + Parcela + "/" + TotalParcelas : "Valor da venda")
            + " • " + ValorPrevisto.ToString("C2", PtBr) + " • vence " + Vencimento.ToString("dd/MM/yyyy")
            + (Atrasado ? " (atrasado)" : "");

        // Visibilidade de controles na linha
        public bool MostrarRegistrar => !Pago;
        public bool MostrarDesfazer => Pago;
        public bool MostrarCheckNaConta => Pago && EhLocacao;
    }
}
