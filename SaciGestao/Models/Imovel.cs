namespace SaciGestao.Models
{
    public enum TipoImovel { Casa, Apartamento, Terreno, Outros }
    public enum FinalidadeImovel { Venda, Aluguel }
    public enum StatusImovel { Disponível, Reservado, Alugado, Vendido }

    public class Imovel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;

        // ---------- Proprietário / Cliente ----------
        public string ProprietarioNome { get; set; } = string.Empty; // Dono / Proprietário

        // ---------- Endereço ----------
        public string Endereco { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;

        public TipoImovel Tipo { get; set; }
        public FinalidadeImovel Finalidade { get; set; }
        public StatusImovel Status { get; set; } = StatusImovel.Disponível;
        public decimal Valor { get; set; }

        // ---------- Cômodos Específicos ----------
        public int Quartos { get; set; }
        public int Suites { get; set; }
        public int Banheiros { get; set; }
        public int Cozinhas { get; set; } = 1;
        public int Salas { get; set; } = 1;
        public int Vagas { get; set; }

        public string Descricao { get; set; } = string.Empty;

        // ---------- Concessionárias ----------
        public string MatriculaEdp { get; set; } = string.Empty;
        public string MatriculaSabesp { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

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

        public Imovel Clonar()
        {
            return (Imovel)MemberwiseClone();
        }
    }
}