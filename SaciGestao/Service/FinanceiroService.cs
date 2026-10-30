using System.Collections.Generic;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class FinanceiroService
    {
        private static FinanceiroService? _instancia;
        public static FinanceiroService Instancia => _instancia ??= new FinanceiroService();

        private FinanceiroService() { }

        public List<MovimentacaoFinanceira> ObterMovimentacoes()
        {
            var movimentacoes = new List<MovimentacaoFinanceira>();

            // Obtém os contratos ativos registados no sistema
            var contratosAtivos = ContratoService.Instancia.ObterAtivos();

            foreach (var contrato in contratosAtivos)
            {
                movimentacoes.Add(new MovimentacaoFinanceira
                {
                    Data = contrato.DataInicio,
                    Descricao = $"Contrato {contrato.TipoTexto} - {contrato.ImovelTitulo} ({contrato.LocatarioNome})",
                    Categoria = "Contratos",
                    Tipo = $"Entrada ({contrato.TipoTexto})",
                    Valor = contrato.ValorMensal,
                    Status = contrato.Status.ToString()
                });
            }

            return movimentacoes;
        }
    }
}