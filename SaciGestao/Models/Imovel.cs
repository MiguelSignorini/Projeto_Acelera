using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        public int ProprietarioId { get; set; }                       // Cliente (Proprietário) dono do imóvel
        public string ProprietarioNome { get; set; } = string.Empty; // Nome exibido (cópia do cliente)

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
        public string Matricula { get; set; } = string.Empty;
        public string Iptu { get; set; } = string.Empty;

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

        // ---------- Fotos (caminhos dos arquivos copiados para a pasta de fotos do app) ----------
        public List<string> Fotos { get; set; } = new List<string>();

        [JsonIgnore] public string? FotoCapa => Fotos.Count > 0 ? Fotos[0] : null;
        [JsonIgnore] public string QuartosTexto => Quartos == 1 ? "1 quarto" : Quartos + " quartos";
        [JsonIgnore] public string SuitesTexto => Suites == 1 ? "1 suíte" : Suites + " suítes";
        [JsonIgnore] public string BanheirosTexto => Banheiros == 1 ? "1 banheiro" : Banheiros + " banheiros";
        [JsonIgnore] public string VagasTexto => Vagas == 1 ? "1 vaga" : Vagas + " vagas";
        private const string NaoInformado = "Não informado";
        private static string OuNaoInformado(string? valor) => string.IsNullOrWhiteSpace(valor) ? NaoInformado : valor;

        [JsonIgnore] public string DescricaoTexto => OuNaoInformado(Descricao);
        [JsonIgnore] public string MatriculaTexto => OuNaoInformado(Matricula);
        [JsonIgnore] public string IptuTexto => OuNaoInformado(Iptu);
        [JsonIgnore] public string MatriculaEdpTexto => OuNaoInformado(MatriculaEdp);
        [JsonIgnore] public string MatriculaSabespTexto => OuNaoInformado(MatriculaSabesp);
        [JsonIgnore] public string ProprietarioTexto => OuNaoInformado(ProprietarioNome);
        [JsonIgnore] public string CidadeEstadoTexto => string.IsNullOrWhiteSpace(Cidade) && string.IsNullOrWhiteSpace(Estado)
            ? NaoInformado
            : (Cidade + (string.IsNullOrWhiteSpace(Estado) ? "" : " / " + Estado));
        [JsonIgnore] public string LocalizacaoCurta => string.IsNullOrWhiteSpace(Cidade) ? Bairro : Bairro + ", " + Cidade;

        // Usado pelas listas de seleção (ComboBox) para mostrar o nome
        public override string ToString() => Titulo;

        public Imovel Clonar()
        {
            return (Imovel)MemberwiseClone();
        }
    }
}