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
            var imovel = ImovelService.Instancia.ObterAtivos().FirstOrDefault(i => i.Id == contrato.ImovelId);
            if (imovel != null)
            {
                imovel.Status = contrato.Tipo == TipoContrato.Locacao ? StatusImovel.Alugado : StatusImovel.Vendido;
                ImovelService.Instancia.Atualizar(imovel);
            }
        }

        public void Atualizar(Contrato editado)
        {
            var existente = _contratos.FirstOrDefault(c => c.Id == editado.Id);
            if (existente == null) return;

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
        }

        public void Excluir(int id)
        {
            _contratos.RemoveAll(c => c.Id == id);
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