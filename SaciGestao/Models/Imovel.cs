namespace SaciGestao.Models
{
    public enum TipoImovel { Casa, Apartamento, Terreno, Outros }

    public enum FinalidadeImovel { Venda, Aluguel }

    public enum StatusImovel { Disponível, Reservado, Alugado, Vendido }

    public class Imovel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;   // ex.: SM001
        public string Titulo { get; set; } = string.Empty;   // ex.: Apt. Centro 2Q
        public string Endereco { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public TipoImovel Tipo { get; set; }
        public FinalidadeImovel Finalidade { get; set; }
        public StatusImovel Status { get; set; } = StatusImovel.Disponível;
        public decimal Valor { get; set; }
        public int Quartos { get; set; }
        public int Vagas { get; set; }

        // false = imóvel oculto da listagem
        public bool Ativo { get; set; } = true;

        // Cria uma cópia, usada na edição (só grava se o usuário salvar)
        public Imovel Clonar()
        {
            return (Imovel)MemberwiseClone();
        }
    }
}