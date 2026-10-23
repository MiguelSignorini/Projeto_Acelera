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
            CarregarExemplos();
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
            AtualizarStatusImovel(contrato.ImovelId, contrato.Tipo == TipoContrato.Locacao
                ? StatusImovel.Alugado
                : StatusImovel.Vendido);
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
            existente.Observacoes = editado.Observacoes;

            AtualizarStatusImovel(existente.ImovelId, existente.Tipo == TipoContrato.Locacao
                ? StatusImovel.Alugado
                : StatusImovel.Vendido);
        }

        public void Excluir(int id)
        {
            var contrato = _contratos.FirstOrDefault(c => c.Id == id);
            if (contrato == null) return;

            _contratos.RemoveAll(c => c.Id == id);

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