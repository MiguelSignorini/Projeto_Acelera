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
        public int QuantidadeParcelas { get; set; } = 1; // Venda: só consta no contrato em PDF (o Financeiro não gera parcelas da venda)

        // % que fica com a imobiliária, combinado neste contrato:
        //  * Venda: comissão sobre o valor total da venda (recebida uma única vez)
        //  * Locação: taxa de administração sobre cada aluguel (o resto é repassado ao proprietário)
        // Sugerido a partir do cadastro do imóvel ao criar o contrato e gravado aqui, para não mudar se o imóvel mudar depois.
        public decimal PercentualImobiliaria { get; set; }

        public static decimal PercentualPadrao(TipoContrato tipo) => tipo == TipoContrato.Venda ? 6m : 10m;
        public decimal PercentualMultaAtraso { get; set; } = 10.0m;
        public int MesesIsencaoMultaRescisao { get; set; } = 12;

        public string UC_Energia { get; set; } = string.Empty;
        public string UC_Agua { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        // Quanto fica com a imobiliária: comissão total (venda) ou taxa de cada mês (locação)
        public decimal GanhoImobiliaria => Math.Round(ValorMensal * PercentualImobiliaria / 100m, 2);
        public decimal RepasseProprietarioMensal => ValorMensal - GanhoImobiliaria;
        public string RotuloPercentualImobiliaria => Tipo == TipoContrato.Venda ? "Comissão da imobiliária (%)" : "Taxa de administração (%)";
        public string PercentualImobiliariaTexto => PercentualImobiliaria.ToString("0.##", new System.Globalization.CultureInfo("pt-BR")) + "%";
        public string ResumoImobiliariaTexto
        {
            get
            {
                var pt = new System.Globalization.CultureInfo("pt-BR");
                if (ValorMensal <= 0 || PercentualImobiliaria <= 0) return "Informe o valor e a porcentagem para ver quanto fica para a imobiliária.";
                return Tipo == TipoContrato.Venda
                    ? "A imobiliária recebe " + GanhoImobiliaria.ToString("C2", pt) + " de comissão sobre a venda (pagamento único)."
                    : "A imobiliária fica com " + GanhoImobiliaria.ToString("C2", pt) + " por mês e repassa " + RepasseProprietarioMensal.ToString("C2", pt) + " ao proprietário.";
            }
        }

        public string TipoTexto => Tipo == TipoContrato.Locacao ? "Locação" : "Venda";
        public string PeriodoTexto => DataInicio.ToString("dd/MM/yyyy") + " a " + DataFim.ToString("dd/MM/yyyy");
        public string ValorTexto => ValorMensal.ToString("C2", new System.Globalization.CultureInfo("pt-BR")) + (Tipo == TipoContrato.Locacao ? " /mês" : "");
        public string RotuloCondicaoTexto => Tipo == TipoContrato.Venda ? "Parcelas" : "Índice de reajuste";
        public string CondicaoTexto => Tipo == TipoContrato.Venda
            ? (QuantidadeParcelas <= 1 ? "À vista (1 parcela)" : QuantidadeParcelas + " parcelas")
            : IndiceReajuste;
        public string ObservacoesTexto => string.IsNullOrWhiteSpace(Observacoes) ? "Não informado" : Observacoes;
        public string UcEnergiaTexto => string.IsNullOrWhiteSpace(UC_Energia) ? "Não informado" : UC_Energia;
        public string UcAguaTexto => string.IsNullOrWhiteSpace(UC_Agua) ? "Não informado" : UC_Agua;

        // CIB do imóvel do contrato (lido do cadastro do imóvel, não é gravado no contrato)
        public string ImovelCibTexto
        {
            get
            {
                var cib = SaciGestao.Service.ImovelService.Instancia.ObterPorId(ImovelId)?.Cib;
                return string.IsNullOrWhiteSpace(cib) ? "Não informado" : cib;
            }
        }

        public Contrato Clonar()
        {
            return (Contrato)MemberwiseClone();
        }
    }
}