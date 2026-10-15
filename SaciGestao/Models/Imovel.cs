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

        // ---------- Endereço ----------
        public string Endereco { get; set; } = string.Empty; // Rua/Av., sem o número
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;   // UF, ex.: SP
        public string Cep { get; set; } = string.Empty;

        public TipoImovel Tipo { get; set; }
        public FinalidadeImovel Finalidade { get; set; }
        public StatusImovel Status { get; set; } = StatusImovel.Disponível;
        public decimal Valor { get; set; }
        public int Quartos { get; set; }
        public int Vagas { get; set; }

        // Texto livre descrevendo os cômodos (ex.: "2 quartos, sala, cozinha...")
        public string Descricao { get; set; } = string.Empty;

        // ---------- Concessionárias (o medidor pertence ao imóvel) ----------
        public string MatriculaEdp { get; set; } = string.Empty;     // Energia
        public string MatriculaSabesp { get; set; } = string.Empty;  // Água

        // false = imóvel oculto da listagem
        public bool Ativo { get; set; } = true;

        // Endereço completo pronto pra exibir na tela
        public string EnderecoCompleto
        {
            get
            {
                string ruaNumero = Numero.Length > 0 ? $"{Endereco}, {Numero}" : Endereco;
                string cidadeEstado = (Cidade.Length > 0 || Estado.Length > 0)
                    ? $" - {Cidade}/{Estado}"
                    : string.Empty;
                string cep = Cep.Length > 0 ? $" - CEP {Cep}" : string.Empty;

                return $"{ruaNumero}{cidadeEstado}{cep}";
            }
        }

        // Cria uma cópia, usada na edição (só grava se o usuário salvar)
        public Imovel Clonar()
        {
            return (Imovel)MemberwiseClone();
        }
    }
}