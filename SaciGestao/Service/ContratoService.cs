using System;
using System.Collections.Generic;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class ContratoService
    {
        public static ContratoService Instancia { get; } = new ContratoService();

        private readonly List<Contrato> _contratos = new List<Contrato>();
        private int _proximoId = 1;

        private ContratoService()
        {
            CarregarDoArquivo();
        }

        private const string Arquivo = "contratos.json";

        // Lê Jsonntratos.json ao iniciar; só grava quando o sistema fecha (ver App.OnExit)
        private void CarregarDoArquivo()
        {
            var lista = Persistencia.Carregar<Contrato>(Arquivo);
            if (lista != null)
            {
                _contratos.AddRange(lista);
                _proximoId = _contratos.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1;
                return;
            }

            CarregarExemplos();
        }

        public void Salvar()
        {
            Persistencia.Salvar(Arquivo, _contratos);
        }

        public List<Contrato> ObterAtivos()
        {
            return _contratos.Where(c => c.Ativo).ToList();
        }

        public void Adicionar(Contrato contrato)
        {
            contrato.Id = _proximoId++;
            contrato.Codigo = "CTR" + contrato.Id.ToString("000");
            contrato.Ativo = true;
            _contratos.Add(contrato);

            // Regra RF12: Atualizar automaticamente o status do imóvel para Alugado ou Vendido
            AplicarStatusImovel(contrato);
        }

        public void Atualizar(Contrato editado)
        {
            var existente = _contratos.FirstOrDefault(c => c.Id == editado.Id);
            if (existente == null) return;

            // Se o contrato mudou de imóvel, devolve o imóvel antigo para Disponível
            // antes de marcar o novo como Alugado/Vendido
            if (existente.ImovelId != editado.ImovelId)
            {
                DevolverImovelSeNaoTiverOutroContrato(existente.ImovelId, existente.Id);
            }

            existente.ImovelId = editado.ImovelId;
            existente.LocatarioId = editado.LocatarioId;
            existente.LocadorId = editado.LocadorId;
            existente.ImovelTitulo = editado.ImovelTitulo;
            existente.LocatarioNome = editado.LocatarioNome;
            existente.LocadorNome = editado.LocadorNome;
            existente.Tipo = editado.Tipo;
            existente.Status = editado.Status;
            existente.ValorMensal = editado.ValorMensal;
            existente.PrazoMeses = editado.PrazoMeses;
            existente.DiaVencimento = editado.DiaVencimento;
            existente.DataInicio = editado.DataInicio;
            existente.DataFim = editado.DataFim;
            existente.DataAssinatura = editado.DataAssinatura;
            existente.UC_Energia = editado.UC_Energia;
            existente.UC_Agua = editado.UC_Agua;
            existente.IndiceReajuste = editado.IndiceReajuste;
            existente.QuantidadeParcelas = editado.QuantidadeParcelas;
            existente.PercentualMultaAtraso = editado.PercentualMultaAtraso;
            existente.MesesIsencaoMultaRescisao = editado.MesesIsencaoMultaRescisao;
            existente.Observacoes = editado.Observacoes;

            AplicarStatusImovel(existente);
        }

        // Contrato Ativo ocupa o imóvel (Alugado/Vendido); Encerrado ou Cancelado devolve para Disponível
        private void AplicarStatusImovel(Contrato contrato)
        {
            if (contrato.Status == StatusContrato.Ativo)
                AtualizarStatusImovel(contrato.ImovelId, contrato.Tipo == TipoContrato.Locacao
                    ? StatusImovel.Alugado
                    : StatusImovel.Vendido);
            else
                DevolverImovelSeNaoTiverOutroContrato(contrato.ImovelId, contrato.Id);
        }

        // Usado antes de excluir cliente/imóvel: existe contrato apontando para ele?
        public bool ClienteTemContrato(int clienteId) =>
            _contratos.Any(c => c.LocatarioId == clienteId || c.LocadorId == clienteId);

        public bool ImovelTemContrato(int imovelId) =>
            _contratos.Any(c => c.ImovelId == imovelId);

        // Contrato ativo que ocupa o imóvel (ignorando um contrato, ex.: o que está sendo editado)
        public Contrato? ContratoAtivoDoImovel(int imovelId, int contratoIdIgnorado = 0) =>
            _contratos.FirstOrDefault(c => c.Ativo && c.Status == StatusContrato.Ativo &&
                                           c.ImovelId == imovelId && c.Id != contratoIdIgnorado);

        // Mantém nomes copiados nos contratos iguais ao cadastro (chamado ao editar cliente/imóvel)
        public void AtualizarNomeCliente(int clienteId, string nome)
        {
            foreach (var c in _contratos)
            {
                if (c.LocatarioId == clienteId) c.LocatarioNome = nome;
                if (c.LocadorId == clienteId) c.LocadorNome = nome;
            }
        }

        public void AtualizarTituloImovel(int imovelId, string titulo)
        {
            foreach (var c in _contratos.Where(c => c.ImovelId == imovelId)) c.ImovelTitulo = titulo;
        }

        public void Excluir(int id)
        {
            var contrato = _contratos.FirstOrDefault(c => c.Id == id);
            if (contrato == null) return;

            _contratos.RemoveAll(c => c.Id == id);

            // Pagamentos do contrato saem junto (senão ficariam "soltos" no Financeiro)
            FinanceiroService.Instancia.RemoverDoContrato(id);

            // Devolve o imóvel para Disponível, a menos que outro contrato ativo ainda o use
            DevolverImovelSeNaoTiverOutroContrato(contrato.ImovelId, contrato.Id);
        }

        // ---------- Helpers ----------
        private void AtualizarStatusImovel(int imovelId, StatusImovel novoStatus)
        {
            var imovel = ImovelService.Instancia.ObterAtivos().FirstOrDefault(i => i.Id == imovelId);
            if (imovel == null) return;

            imovel.Status = novoStatus;
            ImovelService.Instancia.Atualizar(imovel);
        }

        // Só volta o imóvel para Disponível se não houver outro contrato ativo usando ele
        // (ignorando o próprio contrato que está sendo excluído/alterado, identificado por contratoIdIgnorado)
        private void DevolverImovelSeNaoTiverOutroContrato(int imovelId, int contratoIdIgnorado)
        {
            bool temOutroContratoAtivo = _contratos.Any(c =>
                c.Id != contratoIdIgnorado &&
                c.Ativo &&
                c.ImovelId == imovelId &&
                c.Status == StatusContrato.Ativo);

            if (temOutroContratoAtivo) return;

            AtualizarStatusImovel(imovelId, StatusImovel.Disponível);
        }

        private void CarregarExemplos()
        {
            Adicionar(new Contrato
            {
                ImovelId = 3,
                ImovelTitulo = "Apt. Vila Nova 1Q",
                LocatarioNome = "Carlos Eduardo Lima",
                LocadorNome = "Sacilotti Imóveis",
                Tipo = TipoContrato.Locacao,
                Status = StatusContrato.Ativo,
                ValorMensal = 950.00m,
                PrazoMeses = 36,
                DiaVencimento = 20,
                DataInicio = new DateTime(2026, 8, 10),
                DataFim = new DateTime(2029, 8, 10),
                DataAssinatura = new DateTime(2026, 9, 15)
            });
        }
    }
}