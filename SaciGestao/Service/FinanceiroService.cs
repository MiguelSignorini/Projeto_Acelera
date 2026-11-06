using System;
using System.Collections.Generic;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    // Regras do financeiro:
    //  * Cada contrato ativo gera parcelas "Pendente": uma por mês (locação, do início até o mês atual,
    //    no máximo 12 meses para trás) ou uma única (venda).
    //  * A parcela só deixa de ser pendente quando o admin registra o pagamento (vira um Pagamento gravado).
    //  * Locação: o valor só conta como "na conta" quando o check "caiu na conta" está marcado.
    //  * A parte da empresa vem do percentual cadastrado no imóvel (comissão na venda, taxa no aluguel).
    public class FinanceiroService
    {
        private static FinanceiroService? _instancia;
        public static FinanceiroService Instancia => _instancia ??= new FinanceiroService();

        private const string Arquivo = "pagamentos.json";
        private const int MesesParaTras = 12;

        private readonly List<Pagamento> _pagamentos = new();
        private int _proximoId = 1;

        private FinanceiroService()
        {
            var lista = Persistencia.Carregar<Pagamento>(Arquivo);
            if (lista == null) return;

            _pagamentos.AddRange(lista);
            _proximoId = _pagamentos.Max(p => p.Id) + 1;
        }

        public void Salvar() => Persistencia.Salvar(Arquivo, _pagamentos);

        // ---------- Consulta ----------
        // Todas as linhas: pagamentos registrados + parcelas pendentes
        public List<MovimentacaoFinanceira> ObterMovimentacoes()
        {
            var contratos = ContratoService.Instancia.ObterAtivos();
            var linhas = new List<MovimentacaoFinanceira>();

            foreach (var p in _pagamentos)
            {
                var contrato = contratos.FirstOrDefault(c => c.Id == p.ContratoId);
                linhas.Add(MontarPago(p, contrato));
            }

            foreach (var contrato in contratos.Where(c => c.Status == StatusContrato.Ativo))
                linhas.AddRange(GerarPendentes(contrato));

            return linhas.OrderByDescending(l => l.Data).ThenBy(l => l.ContratoCodigo).ToList();
        }

        public List<Contrato> ContratosParaPagamento()
        {
            return ContratoService.Instancia.ObterAtivos()
                .Where(c => c.Status == StatusContrato.Ativo)
                .OrderBy(c => c.Codigo)
                .ToList();
        }

        public List<MovimentacaoFinanceira> ParcelasPendentes(int contratoId)
        {
            var contrato = ContratoService.Instancia.ObterAtivos().FirstOrDefault(c => c.Id == contratoId);
            return contrato == null
                ? new List<MovimentacaoFinanceira>()
                : GerarPendentes(contrato).OrderBy(p => p.Competencia).ToList();
        }

        // ---------- Ações ----------
        public void Registrar(Pagamento pagamento)
        {
            pagamento.Id = _proximoId++;
            _pagamentos.Add(pagamento);
        }

        public void DefinirNaConta(int pagamentoId, bool caiu)
        {
            var p = _pagamentos.FirstOrDefault(x => x.Id == pagamentoId);
            if (p != null) p.CaiuNaConta = caiu;
        }

        // Desfaz o registro: a parcela volta a aparecer como Pendente
        public void Desfazer(int pagamentoId)
        {
            _pagamentos.RemoveAll(p => p.Id == pagamentoId);
        }

        public decimal PercentualDoImovel(int imovelId)
        {
            return ImovelService.Instancia.ObterPorId(imovelId)?.PercentualEmpresa ?? 0m;
        }

        // ---------- Montagem das linhas ----------
        private MovimentacaoFinanceira MontarPago(Pagamento p, Contrato? contrato)
        {
            bool locacao = contrato == null || contrato.Tipo == TipoContrato.Locacao;
            return new MovimentacaoFinanceira
            {
                PagamentoId = p.Id,
                ContratoId = p.ContratoId,
                ContratoCodigo = contrato?.Codigo ?? "—",
                Inquilino = contrato?.LocatarioNome ?? string.Empty,
                Descricao = Descricao(contrato, p.Competencia, locacao),
                EhLocacao = locacao,
                Competencia = p.Competencia,
                Vencimento = p.Competencia,
                DataPagamento = p.DataPagamento,
                Pago = true,
                CaiuNaConta = p.CaiuNaConta,
                ValorPrevisto = contrato?.ValorMensal ?? p.ValorRecebido,
                ValorRecebido = p.ValorRecebido,
                Percentual = p.PercentualEmpresa
            };
        }

        private IEnumerable<MovimentacaoFinanceira> GerarPendentes(Contrato contrato)
        {
            decimal percentual = PercentualDoImovel(contrato.ImovelId);
            var registrados = _pagamentos.Where(p => p.ContratoId == contrato.Id).Select(p => p.Competencia.Date).ToHashSet();

            if (contrato.Tipo == TipoContrato.Venda)
            {
                var data = contrato.DataInicio.Date;
                if (!registrados.Contains(data))
                    yield return Pendente(contrato, data, data, false, percentual);
                yield break;
            }

            var primeiro = new DateTime(contrato.DataInicio.Year, contrato.DataInicio.Month, 1);
            var atual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fim = new DateTime(contrato.DataFim.Year, contrato.DataFim.Month, 1);
            if (fim < atual) atual = fim;

            var inicio = atual.AddMonths(-(MesesParaTras - 1));
            if (primeiro > inicio) inicio = primeiro;

            for (var mes = inicio; mes <= atual; mes = mes.AddMonths(1))
            {
                if (registrados.Contains(mes)) continue;

                int dia = Math.Min(Math.Max(contrato.DiaVencimento, 1), DateTime.DaysInMonth(mes.Year, mes.Month));
                var vencimento = new DateTime(mes.Year, mes.Month, dia);
                if (vencimento < contrato.DataInicio.Date) vencimento = contrato.DataInicio.Date; // 1ª parcela não vence antes do início

                yield return Pendente(contrato, mes, vencimento, true, percentual);
            }
        }

        private static MovimentacaoFinanceira Pendente(Contrato c, DateTime competencia, DateTime vencimento, bool locacao, decimal percentual)
        {
            return new MovimentacaoFinanceira
            {
                ContratoId = c.Id,
                ContratoCodigo = c.Codigo,
                Inquilino = c.LocatarioNome,
                Descricao = Descricao(c, competencia, locacao),
                EhLocacao = locacao,
                Competencia = competencia,
                Vencimento = vencimento,
                Pago = false,
                ValorPrevisto = c.ValorMensal,
                Percentual = percentual
            };
        }

        private static string Descricao(Contrato? c, DateTime competencia, bool locacao)
        {
            if (c == null) return "Contrato removido";
            return $"{c.Codigo} • {c.ImovelTitulo} — {c.LocatarioNome}" + (locacao ? $" ({competencia:MM/yyyy})" : string.Empty);
        }
    }
}
