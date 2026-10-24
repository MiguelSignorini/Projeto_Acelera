using System;
using System.Collections.Generic;
using System.Linq;

using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class ImovelService
    {
        // Instância única: assim os dados continuam ao trocar de tela no menu
        public static ImovelService Instancia { get; } = new ImovelService();

        private readonly List<Imovel> _imoveis = new List<Imovel>();
        private int _proximoId = 1;

        private ImovelService()
        {
            CarregarDoArquivo();
        }

        public List<Imovel> ObterAtivos()
        {
            return _imoveis.Where(i => i.Ativo).ToList();
        }

        public void Adicionar(Imovel imovel)
        {
            AdicionarSemSalvar(imovel);
        }

        public void Atualizar(Imovel editado)
        {
            var existente = _imoveis.FirstOrDefault(i => i.Id == editado.Id);
            if (existente == null) return;

            existente.Titulo = editado.Titulo;
            existente.ProprietarioId = editado.ProprietarioId;
            existente.Fotos = new List<string>(editado.Fotos);
            existente.ProprietarioNome = editado.ProprietarioNome;
            existente.Endereco = editado.Endereco;
            existente.Numero = editado.Numero;
            existente.Bairro = editado.Bairro;
            existente.Cidade = editado.Cidade;
            existente.Estado = editado.Estado;
            existente.Cep = editado.Cep;
            existente.Tipo = editado.Tipo;
            existente.Finalidade = editado.Finalidade;
            existente.Status = editado.Status;
            existente.Valor = editado.Valor;
            existente.Quartos = editado.Quartos;
            existente.Suites = editado.Suites;
            existente.Banheiros = editado.Banheiros;
            existente.Cozinhas = editado.Cozinhas;
            existente.Salas = editado.Salas;
            existente.Suites = editado.Suites;       // Novo campo
            existente.Banheiros = editado.Banheiros; // Novo campo
            existente.Cozinhas = editado.Cozinhas;   // Novo campo
            existente.Salas = editado.Salas;         // Novo campo
            existente.Vagas = editado.Vagas;
            existente.Descricao = editado.Descricao;
            existente.MatriculaEdp = editado.MatriculaEdp;
            existente.MatriculaSabesp = editado.MatriculaSabesp;
            existente.Matricula = editado.Matricula;
            existente.Iptu = editado.Iptu;

        }

        // Remove definitivamente o imóvel da lista
        public void Excluir(int id)
        {
            _imoveis.RemoveAll(i => i.Id == id);
        }

        // ============================================================
        // PERSISTÊNCIA: lê Json\imoveis.json ao iniciar; só grava quando o sistema fecha
        // ============================================================
        private const string Arquivo = "imoveis.json";

        private void CarregarDoArquivo()
        {
            var lista = Persistencia.Carregar<Imovel>(Arquivo);

            if (lista != null)
            {
                _imoveis.AddRange(lista);
                _proximoId = _imoveis.Max(i => i.Id) + 1;
                return;
            }

            CarregarExemplos();
        }

        // Chamado pelo App quando o sistema é encerrado
        public void Salvar()
        {
            Persistencia.Salvar(Arquivo, _imoveis);
        }

        // Usado só na primeira execução, quando ainda não existe o arquivo JSON
        private void AdicionarSemSalvar(Imovel imovel)
        {
            imovel.Id = _proximoId++;
            imovel.Codigo = "SM" + imovel.Id.ToString("000");
            imovel.Ativo = true;
            _imoveis.Add(imovel);
        }

        private void CarregarExemplos()
        {
            AdicionarSemSalvar(new Imovel { ProprietarioId = 1, ProprietarioNome = "João Silva", Titulo = "Apt. Centro 2Q", Endereco = "Rua Principal", Numero = "100", Bairro = "Centro", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-000", Tipo = TipoImovel.Apartamento, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Disponível, Valor = 450000, Quartos = 2, Vagas = 1, Descricao = "2 quartos, sala, cozinha, banheiro social.", MatriculaEdp = "EDP-00123456", MatriculaSabesp = "SAB-00987654" });
            AdicionarSemSalvar(new Imovel { ProprietarioId = 1, ProprietarioNome = "João Silva", Titulo = "Casa Jardim América 3Q", Endereco = "Av. das Flores", Numero = "250", Bairro = "Jardim América", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-010", Tipo = TipoImovel.Casa, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Reservado, Valor = 350000, Quartos = 3, Vagas = 2, Descricao = "3 quartos (1 suíte), sala ampla, cozinha, quintal.", MatriculaEdp = "EDP-00223344", MatriculaSabesp = "SAB-00112233" });
            AdicionarSemSalvar(new Imovel { ProprietarioId = 4, ProprietarioNome = "Fernanda Lima", Titulo = "Apt. Vila Nova 1Q", Endereco = "Rua São Paulo", Numero = "87", Bairro = "Vila Nova", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-020", Tipo = TipoImovel.Apartamento, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Alugado, Valor = 2100, Quartos = 1, Vagas = 1, Descricao = "1 quarto, sala conjugada, cozinha americana.", MatriculaEdp = "EDP-00334455", MatriculaSabesp = "SAB-00223344" });
            AdicionarSemSalvar(new Imovel { ProprietarioId = 4, ProprietarioNome = "Fernanda Lima", Titulo = "Terreno Centro 300m²", Endereco = "Rua das Acácias", Numero = "12", Bairro = "Centro", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-030", Tipo = TipoImovel.Terreno, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Vendido, Valor = 210000, Quartos = 0, Vagas = 0, Descricao = "Terreno plano, murado, pronto para construir.", MatriculaEdp = "", MatriculaSabesp = "" });
            AdicionarSemSalvar(new Imovel { ProprietarioId = 4, ProprietarioNome = "Fernanda Lima", Titulo = "Casa Vila Nova 2Q", Endereco = "Rua das Flores", Numero = "45", Bairro = "Vila Nova", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-020", Tipo = TipoImovel.Casa, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Disponível, Valor = 1800, Quartos = 2, Vagas = 1, Descricao = "2 quartos, sala, cozinha, área de serviço.", MatriculaEdp = "EDP-00445566", MatriculaSabesp = "SAB-00334455" });
            AdicionarSemSalvar(new Imovel { ProprietarioId = 1, ProprietarioNome = "João Silva", Titulo = "Sala Comercial Centro", Endereco = "Av. Brasil", Numero = "900", Bairro = "Centro", Cidade = "Cachoeira Paulista", Estado = "SP", Cep = "12630-000", Tipo = TipoImovel.Outros, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Disponível, Valor = 3200, Quartos = 0, Vagas = 1, Descricao = "Sala comercial térrea, banheiro, copa.", MatriculaEdp = "EDP-00556677", MatriculaSabesp = "SAB-00445566" });
        }
    }
}
