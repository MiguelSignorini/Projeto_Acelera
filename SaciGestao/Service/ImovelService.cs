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
            CarregarExemplos();
        }

        public List<Imovel> ObterAtivos()
        {
            return _imoveis.Where(i => i.Ativo).ToList();
        }

        public void Adicionar(Imovel imovel)
        {
            imovel.Id = _proximoId++;
            imovel.Codigo = "SM" + imovel.Id.ToString("000");
            imovel.Ativo = true;
            _imoveis.Add(imovel);
        }

        public void Atualizar(Imovel editado)
        {
            var existente = _imoveis.FirstOrDefault(i => i.Id == editado.Id);
            if (existente == null) return;

            existente.Titulo = editado.Titulo;
            existente.Endereco = editado.Endereco;
            existente.Bairro = editado.Bairro;
            existente.Tipo = editado.Tipo;
            existente.Finalidade = editado.Finalidade;
            existente.Status = editado.Status;
            existente.Valor = editado.Valor;
            existente.Quartos = editado.Quartos;
            existente.Vagas = editado.Vagas;
        }

        // Remove definitivamente o imóvel da lista
        public void Excluir(int id)
        {
            _imoveis.RemoveAll(i => i.Id == id);
        }

        private void CarregarExemplos()
        {
            Adicionar(new Imovel { Titulo = "Apt. Centro 2Q", Endereco = "Rua Principal, 100", Bairro = "Centro", Tipo = TipoImovel.Apartamento, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Disponível, Valor = 450000, Quartos = 2, Vagas = 1 });
            Adicionar(new Imovel { Titulo = "Casa Jardim América 3Q", Endereco = "Av. das Flores, 250", Bairro = "Jardim América", Tipo = TipoImovel.Casa, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Reservado, Valor = 350000, Quartos = 3, Vagas = 2 });
            Adicionar(new Imovel { Titulo = "Apt. Vila Nova 1Q", Endereco = "Rua São Paulo, 87", Bairro = "Vila Nova", Tipo = TipoImovel.Apartamento, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Alugado, Valor = 2100, Quartos = 1, Vagas = 1 });
            Adicionar(new Imovel { Titulo = "Terreno Centro 300m²", Endereco = "Rua das Acácias, 12", Bairro = "Centro", Tipo = TipoImovel.Terreno, Finalidade = FinalidadeImovel.Venda, Status = StatusImovel.Vendido, Valor = 210000, Quartos = 0, Vagas = 0 });
            Adicionar(new Imovel { Titulo = "Casa Vila Nova 2Q", Endereco = "Rua das Flores, 45", Bairro = "Vila Nova", Tipo = TipoImovel.Casa, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Disponível, Valor = 1800, Quartos = 2, Vagas = 1 });
            Adicionar(new Imovel { Titulo = "Sala Comercial Centro", Endereco = "Av. Brasil, 900", Bairro = "Centro", Tipo = TipoImovel.Outros, Finalidade = FinalidadeImovel.Aluguel, Status = StatusImovel.Disponível, Valor = 3200, Quartos = 0, Vagas = 1 });
        }
    }
}