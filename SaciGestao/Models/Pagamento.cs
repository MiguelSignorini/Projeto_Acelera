using System;

namespace SaciGestao.Models
{
    // Pagamento REGISTRADO pelo administrador (é o que vai para o pagamentos.json).
    // Os pagamentos ainda não registrados aparecem na tela como "Pendente" e são calculados a partir
    // dos contratos ativos; só viram registro aqui quando o admin clica em "Registrar pagamento".
    public class Pagamento
    {
        public int Id { get; set; }
        public int ContratoId { get; set; }

        // Mês de referência (dia 1) nas locações; data de início do contrato nas vendas
        public DateTime Competencia { get; set; }

        public decimal ValorRecebido { get; set; }
        public DateTime DataPagamento { get; set; } = DateTime.Today;

        // Locação: o valor só "cai" na conta (e entra nos totais) quando este check está marcado.
        // Venda: confirmar o pagamento já conta como valor na conta.
        public bool CaiuNaConta { get; set; }

        // Percentual da empresa no momento do registro (vem do imóvel; guardado para o histórico não mudar)
        public decimal PercentualEmpresa { get; set; }

        public string Observacao { get; set; } = string.Empty;
    }
}
