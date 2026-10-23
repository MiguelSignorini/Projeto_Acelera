using System;

namespace SaciGestao.Models
{
    public enum TipoContrato { Locacao, Venda }
    public enum StatusContrato { Ativo, Encerrado, Cancelado }

    public class Contrato
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // Ex: CTR001

        // Relacionamentos por ID
        public int ImovelId { get; set; }
        public int LocatarioId { get; set; } // Cliente (Inquilino / Comprador)
        public int LocadorId { get; set; }   // Cliente (Proprietário)

        // Propriedades auxiliares para exibição em tabelas/telas
        public string ImovelTitulo { get; set; } = string.Empty;
        public string LocatarioNome { get; set; } = string.Empty;
        public string LocadorNome { get; set; } = string.Empty;

        // Dados do Contrato
        public TipoContrato Tipo { get; set; } = TipoContrato.Locacao;
        public StatusContrato Status { get; set; } = StatusContrato.Ativo;

        public decimal ValorMensal { get; set; }
        public int PrazoMeses { get; set; } = 36;
        public int DiaVencimento { get; set; } = 20;

        public DateTime DataInicio { get; set; } = DateTime.Today;
        public DateTime DataFim { get; set; } = DateTime.Today.AddMonths(36);
        public DateTime DataAssinatura { get; set; } = DateTime.Today;

        public string IndiceReajuste { get; set; } = "IGP-M/FGV";
        public decimal PercentualMultaAtraso { get; set; } = 10.0m;
        public int MesesIsencaoMultaRescisao { get; set; } = 12;

        public string UC_Energia { get; set; } = string.Empty;
        public string UC_Agua { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public string TipoTexto => Tipo == TipoContrato.Locacao ? "Locação" : "Venda";
        public string PeriodoTexto => DataInicio.ToString("dd/MM/yyyy") + " a " + DataFim.ToString("dd/MM/yyyy");
        public string ValorTexto => ValorMensal.ToString("C2", new System.Globalization.CultureInfo("pt-BR")) + (Tipo == TipoContrato.Locacao ? " /mês" : "");
        public string ObservacoesTexto => string.IsNullOrWhiteSpace(Observacoes) ? "Não informado" : Observacoes;
        public string UcEnergiaTexto => string.IsNullOrWhiteSpace(UC_Energia) ? "Não informado" : UC_Energia;
        public string UcAguaTexto => string.IsNullOrWhiteSpace(UC_Agua) ? "Não informado" : UC_Agua;

        public Contrato Clonar()
        {
            return (Contrato)MemberwiseClone();
        }
    }
}