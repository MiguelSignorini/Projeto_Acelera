using System;
using System.Collections.Generic;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    // Uma parcela prevista no contrato (paga ou não). Montada pelo Cronograma a partir do contrato.
    //   Competencia = chave que liga a parcela ao pagamento gravado (1º dia do mês na locação;
    //                 data de início + N meses na venda — mantido assim para os pagamentos já gravados)
    //   Vencimento  = data em que a parcela vence (usa o "Dia de vencimento" do contrato nos dois tipos)
    public record ParcelaPrevista(int Numero, int Total, DateTime Competencia, DateTime Vencimento, decimal Valor, bool EhLocacao);

    // Regras do financeiro:
    //  * O Cronograma é a fonte única das parcelas: a tabela, o "A receber", a lista de contratos ativos,
    //    o pop-up de detalhes, o "Registrar pagamento" e a receita do Dashboard usam todos a mesma lista.
    //  * Locação: uma parcela por mês durante o prazo (PrazoMeses), com o aluguel inteiro (a imobiliária recebe
    //    tudo e repassa ao proprietário o que sobra depois da taxa de administração do contrato).
    //  * Venda: UMA única parcela, que é só a COMISSÃO da imobiliária (valor da venda x % do contrato).
    //    O preço da venda não passa pelo sistema: o comprador paga o proprietário (em geral via banco).
    //    A comissão só é registrada quando o administrador tem certeza de que ela já caiu na conta.
    //  * Tabela e "A receber": parcelas não pagas que vencem até o fim do mês atual (sem limite para trás,
    //    então um atraso antigo nunca some). Parcelas futuras aparecem no pop-up do contrato.
    //  * "Registrar pagamento" aceita qualquer parcela ainda não paga (inclusive adiantar a próxima).
    //  * Registrar o pagamento já significa que o valor caiu na conta (locação e venda entram nos totais na hora).
    //  * A parte da imobiliária vem do percentual do CONTRATO: taxa de administração (locação) ou comissão (venda).
    //    Na venda o valor registrado já é só da imobiliária, então 100% dele é da empresa.
    public class FinanceiroService
    {
        private static FinanceiroService? _instancia;
        public static FinanceiroService Instancia => _instancia ??= new FinanceiroService();

        private const string Arquivo = "pagamentos.json";

        private readonly List<Pagamento> _pagamentos = new();
        private int _proximoId = 1;

        private FinanceiroService()
        {
            var lista = Persistencia.Carregar<Pagamento>(Arquivo);
            if (lista == null) return;

            _pagamentos.AddRange(lista);
            _proximoId = _pagamentos.Select(p => p.Id).DefaultIfEmpty(0).Max() + 1;
        }

        public void Salvar() => Persistencia.Salvar(Arquivo, _pagamentos);

        // ============================================================
        // CRONOGRAMA (fonte única das parcelas)
        // ============================================================
        public static List<ParcelaPrevista> Cronograma(Contrato contrato)
        {
            var lista = new List<ParcelaPrevista>();
            var inicio = contrato.DataInicio.Date;
            var primeiroMes = new DateTime(inicio.Year, inicio.Month, 1);

            if (contrato.Tipo == TipoContrato.Venda)
            {
                // Venda = um único pagamento: a comissão da imobiliária sobre o valor total da venda
                lista.Add(new ParcelaPrevista(1, 1, inicio, Vencimento(contrato, primeiroMes), contrato.GanhoImobiliaria, false));
            }
            else
            {
                int prazo = Math.Max(1, contrato.PrazoMeses);
                for (int i = 0; i < prazo; i++)
                {
                    var mes = primeiroMes.AddMonths(i);
                    lista.Add(new ParcelaPrevista(i + 1, prazo, mes, Vencimento(contrato, mes), contrato.ValorMensal, true));
                }
            }

            return lista;
        }

        // "Dia de vencimento" no mês da parcela (dia 31 vira o último dia do mês); a 1ª parcela nunca vence antes do início
        private static DateTime Vencimento(Contrato contrato, DateTime mes)
        {
            int dia = Math.Min(Math.Max(contrato.DiaVencimento, 1), DateTime.DaysInMonth(mes.Year, mes.Month));
            var vencimento = new DateTime(mes.Year, mes.Month, dia);
            return vencimento < contrato.DataInicio.Date ? contrato.DataInicio.Date : vencimento;
        }

        // Último dia do mês atual: parcelas que vencem até aqui entram na tabela e no "A receber"
        private static DateTime FimDoMesAtual => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);

        private HashSet<DateTime> CompetenciasPagas(int contratoId) =>
            _pagamentos.Where(p => p.ContratoId == contratoId).Select(p => p.Competencia.Date).ToHashSet();

        // ============================================================
        // CONSULTAS
        // ============================================================
        // Tabela: pagamentos registrados + parcelas não pagas que vencem até o fim do mês atual
        public List<MovimentacaoFinanceira> ObterMovimentacoes()
        {
            var contratos = ContratoService.Instancia.ObterAtivos();
            var linhas = new List<MovimentacaoFinanceira>();

            foreach (var p in _pagamentos)
            {
                var contrato = contratos.FirstOrDefault(c => c.Id == p.ContratoId);
                linhas.Add(MontarPago(p, contrato));
            }

            var limite = FimDoMesAtual;
            foreach (var contrato in contratos.Where(c => c.Status == StatusContrato.Ativo))
                linhas.AddRange(Pendentes(contrato).Where(m => m.Vencimento.Date <= limite));

            return linhas.OrderByDescending(l => l.Data).ThenBy(l => l.ContratoCodigo).ToList();
        }

        public List<Contrato> ContratosParaPagamento()
        {
            return ContratoService.Instancia.ObterAtivos()
                .Where(c => c.Status == StatusContrato.Ativo)
                .OrderBy(c => c.Codigo)
                .ToList();
        }

        // Diálogo "Registrar pagamento": todas as parcelas ainda não pagas do contrato (atrasadas, do mês e futuras),
        // da mais antiga para a mais nova
        public List<MovimentacaoFinanceira> ParcelasPendentes(int contratoId)
        {
            var contrato = ContratoService.Instancia.ObterAtivos().FirstOrDefault(c => c.Id == contratoId);
            return contrato == null
                ? new List<MovimentacaoFinanceira>()
                : Pendentes(contrato).OrderBy(p => p.Vencimento).ThenBy(p => p.Competencia).ToList();
        }

        // Contratos ativos com o resumo financeiro (quanto já foi pago, quanto falta, parcelas...).
        // Os que têm parcela atrasada vêm primeiro.
        public List<ContratoFinanceiro> ObterContratosFinanceiro()
        {
            return ContratosParaPagamento()
                .Select(MontarContratoFinanceiro)
                .OrderByDescending(c => c.ComAtraso)
                .ThenBy(c => c.Codigo)
                .ToList();
        }

        public ContratoFinanceiro? ObterContratoFinanceiro(int contratoId)
        {
            var contrato = ContratosParaPagamento().FirstOrDefault(c => c.Id == contratoId);
            return contrato == null ? null : MontarContratoFinanceiro(contrato);
        }

        // Receita da IMOBILIÁRIA prevista no mês: taxa de administração dos aluguéis que vencem no mês
        // + comissões de venda previstas para o mês. O restante do aluguel é do proprietário e não conta aqui.
        public (decimal Total, int Alugueis, int Comissoes) ReceitaDoMes(DateTime referencia)
        {
            var inicio = new DateTime(referencia.Year, referencia.Month, 1);
            var fim = inicio.AddMonths(1);
            decimal total = 0m;
            int alugueis = 0, comissoes = 0;

            foreach (var contrato in ContratosParaPagamento())
            {
                decimal percentual = PercentualDoContrato(contrato);
                foreach (var p in Cronograma(contrato).Where(p => p.Vencimento >= inicio && p.Vencimento < fim))
                {
                    total += Math.Round(p.Valor * percentual / 100m, 2);
                    if (p.EhLocacao) alugueis++; else comissoes++;
                }
            }

            return (total, alugueis, comissoes);
        }

        // Comissão total prevista nas vendas (não canceladas) e quanto dela já foi recebido
        public (decimal Prevista, decimal Recebida) ComissaoDasVendas(IEnumerable<Contrato> vendas)
        {
            var lista = vendas.ToList();
            return (lista.Sum(c => c.GanhoImobiliaria), lista.Sum(c => TotalRecebido(c.Id)));
        }

        // Valor já recebido de um contrato (usado no Dashboard)
        public decimal TotalRecebido(int contratoId) => _pagamentos.Where(p => p.ContratoId == contratoId).Sum(p => p.ValorRecebido);

        // Todas as parcelas do contrato, do início ao fim, cada uma marcada como paga ou não
        private ContratoFinanceiro MontarContratoFinanceiro(Contrato contrato)
        {
            var pagos = _pagamentos.Where(p => p.ContratoId == contrato.Id)
                .GroupBy(p => p.Competencia.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var parcelas = Cronograma(contrato).Select(p =>
            {
                pagos.TryGetValue(p.Competencia, out var pg);
                string referencia = p.EhLocacao ? p.Competencia.ToString("MM/yyyy") : "Comissão da venda";
                return new ParcelaContrato
                {
                    Numero = p.Numero,
                    Referencia = referencia,
                    Competencia = p.Competencia,
                    Vencimento = p.Vencimento,
                    ValorPrevisto = p.Valor,
                    EhLocacao = p.EhLocacao,
                    Pago = pg != null,
                    ValorPago = pg?.ValorRecebido ?? 0m,
                    DataPagamento = pg?.DataPagamento
                };
            }).ToList();

            return new ContratoFinanceiro
            {
                Contrato = contrato,
                Parcelas = parcelas,
                PercentualEmpresa = PercentualDoContrato(contrato)
            };
        }

        // ============================================================
        // AÇÕES
        // ============================================================
        public void Registrar(Pagamento pagamento)
        {
            pagamento.Id = _proximoId++;
            _pagamentos.Add(pagamento);
        }

        // Desfaz o registro: a parcela volta a aparecer como Pendente
        public void Desfazer(int pagamentoId)
        {
            _pagamentos.RemoveAll(p => p.Id == pagamentoId);
        }

        public int QuantidadeDoContrato(int contratoId) => _pagamentos.Count(p => p.ContratoId == contratoId);

        // Chamado ao excluir um contrato
        public void RemoverDoContrato(int contratoId)
        {
            _pagamentos.RemoveAll(p => p.ContratoId == contratoId);
        }

        // % da empresa sobre o valor RECEBIDO neste contrato:
        //   locação = taxa de administração do contrato (o resto é repassado ao proprietário)
        //   venda   = 100%, porque o valor registrado já é só a comissão da imobiliária
        public decimal PercentualDoContrato(Contrato contrato)
        {
            if (contrato.Tipo == TipoContrato.Venda) return 100m;
            if (contrato.PercentualImobiliaria > 0) return contrato.PercentualImobiliaria;
            return ImovelService.Instancia.ObterPorId(contrato.ImovelId)?.PercentualEmpresa ?? Contrato.PercentualPadrao(contrato.Tipo);
        }

        // ============================================================
        // MONTAGEM DAS LINHAS
        // ============================================================
        private MovimentacaoFinanceira MontarPago(Pagamento p, Contrato? contrato)
        {
            bool locacao = contrato == null || contrato.Tipo == TipoContrato.Locacao;

            // Descobre qual parcela do cronograma este pagamento quita (pela competência)
            int numero = 1, total = 1;
            decimal previsto = contrato?.ValorMensal ?? p.ValorRecebido;
            DateTime vencimento = p.Competencia;
            if (contrato != null)
            {
                var parcela = Cronograma(contrato).FirstOrDefault(x => x.Competencia == p.Competencia.Date);
                if (parcela != null)
                {
                    previsto = parcela.Valor;
                    vencimento = parcela.Vencimento;
                }
            }

            return new MovimentacaoFinanceira
            {
                PagamentoId = p.Id,
                ContratoId = p.ContratoId,
                ContratoCodigo = contrato?.Codigo ?? "—",
                ContratoResumo = contrato == null ? "Contrato removido" : contrato.Codigo + " • " + contrato.ImovelTitulo,
                Inquilino = contrato?.LocatarioNome ?? string.Empty,
                Descricao = Descricao(contrato, p.Competencia, locacao),
                EhLocacao = locacao,
                Competencia = p.Competencia,
                Vencimento = vencimento,
                DataPagamento = p.DataPagamento,
                Pago = true,
                ValorVenda = contrato != null && !locacao ? contrato.ValorMensal : 0m,
                ValorPrevisto = previsto,
                ValorRecebido = p.ValorRecebido,
                Percentual = p.PercentualEmpresa,
                Parcela = numero,
                TotalParcelas = total
            };
        }

        // Parcelas do cronograma que ainda não têm pagamento registrado
        private IEnumerable<MovimentacaoFinanceira> Pendentes(Contrato contrato)
        {
            decimal percentual = PercentualDoContrato(contrato);
            var pagas = CompetenciasPagas(contrato.Id);

            foreach (var p in Cronograma(contrato))
            {
                if (pagas.Contains(p.Competencia)) continue;
                yield return new MovimentacaoFinanceira
                {
                    ContratoId = contrato.Id,
                    ContratoCodigo = contrato.Codigo,
                    ContratoResumo = contrato.Codigo + " • " + contrato.ImovelTitulo,
                    Inquilino = contrato.LocatarioNome,
                    Descricao = Descricao(contrato, p.Competencia, p.EhLocacao),
                    EhLocacao = p.EhLocacao,
                    Competencia = p.Competencia,
                    Vencimento = p.Vencimento,
                    Pago = false,
                    ValorVenda = p.EhLocacao ? 0m : contrato.ValorMensal,
                    ValorPrevisto = p.Valor,
                    Percentual = percentual,
                    Parcela = 1,
                    TotalParcelas = 1
                };
            }
        }

        private static string Descricao(Contrato? c, DateTime competencia, bool locacao)
        {
            if (c == null) return "Contrato removido";
            string sufixo = locacao ? $" ({competencia:MM/yyyy})" : " (comissão da venda)";
            return $"{c.Codigo} • {c.ImovelTitulo} — {c.LocatarioNome}" + sufixo;
        }
    }
}
