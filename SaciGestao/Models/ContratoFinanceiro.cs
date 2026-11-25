using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SaciGestao.Models
{
    // Uma parcela do contrato (paga ou não). Não é gravada em arquivo: o FinanceiroService monta a lista
    // a partir do contrato (prazo, vencimento, valor) e dos pagamentos registrados.
    public class ParcelaContrato
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public int Numero { get; set; }
        public string Referencia { get; set; } = string.Empty;   // "11/2026" na locação, "Parcela 3/36" na venda
        public DateTime Competencia { get; set; }
        public DateTime Vencimento { get; set; }
        public decimal ValorPrevisto { get; set; }
        public decimal ValorPago { get; set; }
        public DateTime? DataPagamento { get; set; }
        public bool Pago { get; set; }
        public bool EhLocacao { get; set; }

        // Nomes iguais aos da MovimentacaoFinanceira para reaproveitar os estilos das etiquetas de pagamento
        public bool Atrasado => !Pago && Vencimento.Date < DateTime.Today;
        public string PagamentoTexto => Pago ? "Paga" : (Atrasado ? "Atrasada" : "A vencer");

        public string NumeroTexto => Numero.ToString("00");
        public string VencimentoTexto => Vencimento.ToString("dd/MM/yyyy");
        public string ValorPrevistoTexto => ValorPrevisto.ToString("C2", PtBr);
        public string ValorPagoTexto => Pago ? ValorPago.ToString("C2", PtBr) : "—";
        public string DataPagamentoTexto => Pago && DataPagamento.HasValue ? DataPagamento.Value.ToString("dd/MM/yyyy") : "—";
    }

    // Resumo financeiro de um contrato ativo: usado na lista "Contratos ativos" e no pop-up de detalhes.
    public class ContratoFinanceiro
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public Contrato Contrato { get; set; } = new Contrato();
        public List<ParcelaContrato> Parcelas { get; set; } = new List<ParcelaContrato>();
        public decimal PercentualEmpresa { get; set; }

        public int ContratoId => Contrato.Id;
        public string Codigo => Contrato.Codigo;
        public string ImovelTitulo => Contrato.ImovelTitulo;
        public string LocatarioNome => Contrato.LocatarioNome;
        public string LocadorNome => Contrato.LocadorNome;
        public bool EhLocacao => Contrato.Tipo == TipoContrato.Locacao;
        public string TipoTexto => Contrato.TipoTexto;

        // ---------- Valores ----------
        public decimal ValorTotal => Parcelas.Sum(p => p.ValorPrevisto);
        public decimal TotalPago => Parcelas.Where(p => p.Pago).Sum(p => p.ValorPago);
        public decimal Falta => Parcelas.Where(p => !p.Pago).Sum(p => p.ValorPrevisto);
        public decimal Atrasado => Parcelas.Where(p => p.Atrasado).Sum(p => p.ValorPrevisto);

        public int TotalParcelas => Parcelas.Count;
        public int ParcelasPagas => Parcelas.Count(p => p.Pago);
        public int ParcelasPendentes => TotalParcelas - ParcelasPagas;
        public int ParcelasAtrasadas => Parcelas.Count(p => p.Atrasado);

        public double Progresso => TotalParcelas == 0 ? 0 : ParcelasPagas * 100.0 / TotalParcelas;
        public string ProgressoTexto => Progresso.ToString("0", PtBr) + "%";
        public string ParcelasTexto => EhLocacao
            ? ParcelasPagas + " de " + TotalParcelas + (TotalParcelas == 1 ? " parcela paga" : " parcelas pagas")
            : (Quitado ? "Comissão recebida" : "Comissão ainda não recebida");

        public decimal GanhoEmpresa => Math.Round(TotalPago * PercentualEmpresa / 100m, 2);
        public decimal RepasseProprietario => TotalPago - GanhoEmpresa;

        // ---------- Próximo pagamento ----------
        public ParcelaContrato? Proxima => Parcelas.Where(p => !p.Pago).OrderBy(p => p.Vencimento).FirstOrDefault();
        public bool Quitado => TotalParcelas > 0 && ParcelasPagas == TotalParcelas;
        public bool ComAtraso => ParcelasAtrasadas > 0;

        public string StatusTexto => Quitado ? "Quitado" : (ComAtraso ? ParcelasAtrasadas + (ParcelasAtrasadas == 1 ? " atrasada" : " atrasadas") : "Em dia");

        public string ProximoVencimentoTexto => Proxima == null ? "—" : Proxima.Vencimento.ToString("dd/MM/yyyy");
        public string ProximoVencimentoDetalhe
        {
            get
            {
                var p = Proxima;
                if (p == null) return "Todas as parcelas pagas";
                int dias = (p.Vencimento.Date - DateTime.Today).Days;
                string referencia = EhLocacao ? "Ref. " + p.Competencia.ToString("MM/yyyy") : p.Referencia;
                if (dias < 0) return referencia + " • atrasada há " + (-dias) + (dias == -1 ? " dia" : " dias");
                if (dias == 0) return referencia + " • vence hoje";
                return referencia + " • vence em " + dias + (dias == 1 ? " dia" : " dias");
            }
        }

        // ---------- Modelo de pagamento ----------
        public string ModeloPagamentoTexto => EhLocacao
            ? "Aluguel mensal recebido pela imobiliária, com repasse ao proprietário"
            : "Comissão da imobiliária sobre a venda (pagamento único)";

        public string ValorParcelaTexto => (Parcelas.Count > 0 ? Parcelas[0].ValorPrevisto : Contrato.ValorMensal).ToString("C2", PtBr);

        // Locação vence no "Dia de vencimento" do contrato; a comissão da venda tem uma data prevista só
        public string VencimentoRegraTexto => EhLocacao
            ? "Todo dia " + Contrato.DiaVencimento + " de cada mês"
            : "Previsto para " + (Parcelas.Count > 0 ? Parcelas[0].Vencimento : Contrato.DataInicio).ToString("dd/MM/yyyy");

        public string ReajusteTexto => EhLocacao ? Contrato.IndiceReajuste + " (anual)" : "Não se aplica";
        public string MultaTexto => EhLocacao ? Contrato.PercentualMultaAtraso.ToString("0.##", PtBr) + "% sobre o valor em atraso" : "Não se aplica";

        // Rótulos que mudam entre locação e venda (na venda tudo aqui é só a comissão da imobiliária)
        public string RotuloValorContrato => EhLocacao ? "Valor do aluguel" : "Valor da venda";
        public string RotuloPago => EhLocacao ? "Já pago" : "Comissão recebida";
        public string RotuloFalta => EhLocacao ? "Ainda falta" : "Comissão a receber";
        public string ValorContratoTexto => EhLocacao
            ? Contrato.ValorMensal.ToString("C2", PtBr) + " /mês"
            : Contrato.ValorMensal.ToString("C2", PtBr);
        public string RotuloValorParcela => EhLocacao ? "Valor de cada parcela" : "Valor da comissão";
        public string RotuloParticipacao => EhLocacao ? "Taxa da imobiliária e repasse" : "Comissão da imobiliária";
        public string ValorContratoDetalhe => EhLocacao ? Contrato.PeriodoTexto
            : "Comissão da imobiliária: " + Contrato.PercentualImobiliariaTexto + " = " + Contrato.GanhoImobiliaria.ToString("C2", PtBr);
        public string FaltaDetalhe => EhLocacao
            ? ParcelasPendentes + " parcela(s) • atrasado: " + Atrasado.ToString("C2", PtBr)
            : (Quitado ? "Nada pendente" : (ComAtraso ? "Prevista e ainda não recebida" : "Registre quando cair na conta"));

        // Linha única do pop-up: o que fica com a imobiliária e (na locação) o que vai para o proprietário
        public string ParticipacaoTexto => EhLocacao
            ? Contrato.PercentualImobiliariaTexto + " de taxa • recebida: " + GanhoEmpresaTexto + " • repasse ao proprietário: " + RepasseTexto
            : Contrato.PercentualImobiliariaTexto + " • recebida: " + GanhoEmpresaTexto;
        public string PeriodoTexto => Contrato.PeriodoTexto;
        public string PercentualEmpresaTexto => PercentualEmpresa.ToString("0.##", PtBr) + "%";

        // Textos prontos para a tela
        public string ValorTotalTexto => ValorTotal.ToString("C2", PtBr);
        public string TotalPagoTexto => TotalPago.ToString("C2", PtBr);
        public string FaltaTexto => Falta.ToString("C2", PtBr);
        public string AtrasadoTexto => Atrasado.ToString("C2", PtBr);
        public string GanhoEmpresaTexto => GanhoEmpresa.ToString("C2", PtBr);
        public string RepasseTexto => RepasseProprietario.ToString("C2", PtBr);
    }
}
