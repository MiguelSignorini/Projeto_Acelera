using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    // Uma barra do funil comercial: rótulo, quantidade, detalhe em R$, maior valor do grupo e cor
    public class BarraResumo
    {
        public string Rotulo { get; set; } = string.Empty;
        public int Valor { get; set; }
        public int Maximo { get; set; }
        public string Detalhe { get; set; } = string.Empty;
        public string Cor { get; set; } = "#1E5BB8";
    }

    // Uma fatia do gráfico de rosca (o desenho é um arco, ver MontarRosca)
    public class FatiaRosca
    {
        public string Rotulo { get; set; } = string.Empty;
        public int Valor { get; set; }
        public string Percentual { get; set; } = string.Empty;
        public string Cor { get; set; } = "#1E5BB8";
        public string Arco { get; set; } = string.Empty;
    }

    // Aviso do painel "Atenção": ícone, texto e cores (texto escuro sobre fundo claro)
    public class AvisoResumo
    {
        public string Icone { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public string Cor { get; set; } = "#1E5BB8";
        public string CorFundo { get; set; } = "#E8F0FB";
    }

    // Uma linha da tabela "Contratos a vencer"
    public class VencimentoResumo
    {
        public string Codigo { get; set; } = string.Empty;
        public string Imovel { get; set; } = string.Empty;
        public string Inquilino { get; set; } = string.Empty;
        public string Termino { get; set; } = string.Empty;
        public string Prazo { get; set; } = string.Empty;
        public string CorFundo { get; set; } = "#E4E7EC";
        public string CorTexto { get; set; } = "#374151";
        public string Valor { get; set; } = string.Empty;
    }

    public class DashboardViewModel : ViewModelBase
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        // ---------- Cabeçalho ----------
        public string Saudacao { get; }
        public string DataHoje { get; }

        // ---------- Indicadores principais ----------
        public decimal ReceitaMensal { get; }
        public string ReceitaDetalhe { get; }
        public int OcupacaoPercentual { get; }
        public string OcupacaoDetalhe { get; }
        public decimal ValorEmCarteira { get; }
        public string CarteiraDetalhe { get; }
        public decimal VendasFechadas { get; }
        public string VendasDetalhe { get; }

        // ---------- Blocos ----------
        public List<VencimentoResumo> ContratosAVencer { get; }
        public List<BarraResumo> Funil { get; }
        public List<FatiaRosca> ImoveisPorStatus { get; }
        public int TotalGrafico { get; }
        public List<AvisoResumo> Avisos { get; }
        public List<Imovel> ImoveisParaOferecer { get; }

        public DashboardViewModel()
        {
            var imoveis = ImovelService.Instancia.ObterAtivos();
            var clientes = ClienteService.Instancia.ObterTodos();
            var contratos = ContratoService.Instancia.ObterAtivos();
            var hoje = DateTime.Today;

            // --- Cabeçalho ---
            string primeiroNome = (UsuarioService.Instancia.UsuarioLogado?.NomeCompleto ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            int hora = DateTime.Now.Hour;
            string periodo = hora < 12 ? "Bom dia" : hora < 18 ? "Boa tarde" : "Boa noite";
            Saudacao = string.IsNullOrEmpty(primeiroNome) ? periodo : periodo + ", " + primeiroNome;
            string data = hoje.ToString("dddd, dd 'de' MMMM 'de' yyyy", PtBr);
            DataHoje = char.ToUpper(data[0], PtBr) + data.Substring(1);

            var ativos = contratos.Where(c => c.Status == StatusContrato.Ativo).ToList();
            var locacoesAtivas = ativos.Where(c => c.Tipo == TipoContrato.Locacao).ToList();

            // 1) Receita recorrente
            ReceitaMensal = locacoesAtivas.Sum(c => c.ValorMensal);
            ReceitaDetalhe = locacoesAtivas.Count + (locacoesAtivas.Count == 1 ? " contrato de locação ativo" : " contratos de locação ativos");

            // 2) Taxa de ocupação: imóveis de aluguel que já estão alugados
            var paraAlugar = imoveis.Where(i => i.Finalidade == FinalidadeImovel.Aluguel).ToList();
            int alugados = paraAlugar.Count(i => i.Status == StatusImovel.Alugado);
            OcupacaoPercentual = paraAlugar.Count > 0 ? (int)Math.Round(100.0 * alugados / paraAlugar.Count) : 0;
            OcupacaoDetalhe = alugados + " de " + paraAlugar.Count + " imóveis de aluguel ocupados";

            // 3) Carteira à venda (o que ainda pode virar negócio)
            var aVenda = imoveis.Where(i => i.Finalidade == FinalidadeImovel.Venda &&
                                            (i.Status == StatusImovel.Disponível || i.Status == StatusImovel.Reservado)).ToList();
            ValorEmCarteira = aVenda.Sum(i => i.Valor);
            CarteiraDetalhe = aVenda.Count + (aVenda.Count == 1 ? " imóvel à venda" : " imóveis à venda");

            // 4) Vendas concluídas
            var vendas = contratos.Where(c => c.Tipo == TipoContrato.Venda && c.Status != StatusContrato.Cancelado).ToList();
            VendasFechadas = vendas.Sum(c => c.ValorMensal);
            VendasDetalhe = vendas.Count + (vendas.Count == 1 ? " venda fechada" : " vendas fechadas");

            // --- Contratos a vencer (próximos 120 dias, mais urgentes primeiro) ---
            ContratosAVencer = locacoesAtivas
                .Where(c => c.DataFim.Date <= hoje.AddDays(120))
                .OrderBy(c => c.DataFim)
                .Take(6)
                .Select(c => MontarVencimento(c, hoje))
                .ToList();

            // --- Funil comercial ---
            var disponiveis = imoveis.Where(i => i.Status == StatusImovel.Disponível).ToList();
            var reservados = imoveis.Where(i => i.Status == StatusImovel.Reservado).ToList();
            int fechados = imoveis.Count(i => i.Status == StatusImovel.Alugado || i.Status == StatusImovel.Vendido);
            int leads = clientes.Count(c => c.Status == StatusCliente.EmAtendimento);
            int maximo = Math.Max(1, new[] { leads, disponiveis.Count, reservados.Count, fechados }.Max());
            Funil = new List<BarraResumo>
            {
                new BarraResumo { Rotulo = "Clientes em atendimento", Valor = leads, Maximo = maximo, Cor = "#1E5BB8", Detalhe = "interessados" },
                new BarraResumo { Rotulo = "Imóveis disponíveis", Valor = disponiveis.Count, Maximo = maximo, Cor = "#2E9E5B", Detalhe = Moeda(disponiveis.Sum(i => i.Valor)) },
                new BarraResumo { Rotulo = "Imóveis reservados", Valor = reservados.Count, Maximo = maximo, Cor = "#D9A21B", Detalhe = Moeda(reservados.Sum(i => i.Valor)) },
                new BarraResumo { Rotulo = "Negócios fechados", Valor = fechados, Maximo = maximo, Cor = "#6B46C1", Detalhe = "alugados e vendidos" }
            };

            // --- Rosca de status ---
            TotalGrafico = imoveis.Count;
            ImoveisPorStatus = MontarRosca(new[]
            {
                ("Disponível", imoveis.Count(i => i.Status == StatusImovel.Disponível), "#2E9E5B"),
                ("Reservado",  imoveis.Count(i => i.Status == StatusImovel.Reservado),  "#D9A21B"),
                ("Alugado",    imoveis.Count(i => i.Status == StatusImovel.Alugado),    "#1E5BB8"),
                ("Vendido",    imoveis.Count(i => i.Status == StatusImovel.Vendido),    "#6B46C1")
            });

            // --- Avisos (só o que exige ação da imobiliária) ---
            Avisos = new List<AvisoResumo>();
            int vencendo = ContratosAVencer.Count(v => v.CorFundo != "#E4E7EC");
            if (vencendo > 0)
                Avisos.Add(Aviso(0xE787, vencendo + (vencendo == 1 ? " contrato precisa de renovação em até 60 dias" : " contratos precisam de renovação em até 60 dias"), "#7A4B00", "#FFF0C2"));

            if (reservados.Count > 0)
                Avisos.Add(Aviso(0xE81C, reservados.Count + (reservados.Count == 1 ? " imóvel reservado aguardando fechamento" : " imóveis reservados aguardando fechamento"), "#14407A", "#DCE9FB"));

            int semFoto = imoveis.Count(i => i.Fotos.Count == 0 && i.Status == StatusImovel.Disponível);
            if (semFoto > 0)
                Avisos.Add(Aviso(0xEB9F, semFoto + (semFoto == 1 ? " imóvel disponível está sem foto (anúncio fraco)" : " imóveis disponíveis estão sem foto (anúncio fraco)"), "#374151", "#E4E7EC"));

            int semDono = imoveis.Count(i => i.ProprietarioId == 0);
            if (semDono > 0)
                Avisos.Add(Aviso(0xE77B, semDono + (semDono == 1 ? " imóvel sem proprietário vinculado" : " imóveis sem proprietário vinculado"), "#8B1A1A", "#FBDADA"));

            if (Avisos.Count == 0)
                Avisos.Add(Aviso(0xE73E, "Tudo em dia, nenhuma pendência no momento.", "#14532D", "#D4EDDA"));

            ImoveisParaOferecer = disponiveis.OrderByDescending(i => i.Id).Take(5).ToList();
        }

        private static VencimentoResumo MontarVencimento(Contrato c, DateTime hoje)
        {
            int dias = (c.DataFim.Date - hoje).Days;

            var v = new VencimentoResumo
            {
                Codigo = c.Codigo,
                Imovel = c.ImovelTitulo,
                Inquilino = c.LocatarioNome,
                Termino = c.DataFim.ToString("dd/MM/yyyy"),
                Valor = Moeda(c.ValorMensal)
            };

            if (dias < 0)
            {
                v.Prazo = "Vencido há " + (-dias) + (dias == -1 ? " dia" : " dias");
                v.CorFundo = "#FBDADA"; v.CorTexto = "#8B1A1A";
            }
            else if (dias <= 60)
            {
                v.Prazo = dias == 0 ? "Vence hoje" : "Em " + dias + (dias == 1 ? " dia" : " dias");
                v.CorFundo = "#FFF0C2"; v.CorTexto = "#7A4B00";
            }
            else
            {
                v.Prazo = "Em " + dias + " dias";
            }

            return v;
        }

        private static string Moeda(decimal valor) => valor.ToString("C0", PtBr);

        private static AvisoResumo Aviso(int icone, string texto, string cor, string corFundo)
        {
            return new AvisoResumo { Icone = char.ConvertFromUtf32(icone), Texto = texto, Cor = cor, CorFundo = corFundo };
        }

        // Rosca desenhada com arcos: cada fatia é uma linha curva grossa (stroke) sobre um círculo de raio 78
        private static List<FatiaRosca> MontarRosca((string rotulo, int valor, string cor)[] itens)
        {
            const double centro = 100, raio = 78;
            int total = itens.Sum(i => i.valor);
            double inicio = -Math.PI / 2; // começa no topo

            var fatias = new List<FatiaRosca>();
            foreach (var (rotulo, valor, cor) in itens)
            {
                double fracao = total > 0 ? (double)valor / total : 0;
                var fatia = new FatiaRosca
                {
                    Rotulo = rotulo,
                    Valor = valor,
                    Cor = cor,
                    Percentual = Math.Round(fracao * 100).ToString("0", PtBr) + "%"
                };

                if (fracao >= 0.9999)
                {
                    // Círculo completo: um arco só não fecha, então são dois semicírculos
                    fatia.Arco = $"M {F(centro)},{F(centro - raio)} A {F(raio)},{F(raio)} 0 1 1 {F(centro)},{F(centro + raio)} A {F(raio)},{F(raio)} 0 1 1 {F(centro)},{F(centro - raio)}";
                }
                else if (fracao > 0)
                {
                    double fim = inicio + fracao * 2 * Math.PI;
                    double x1 = centro + raio * Math.Cos(inicio), y1 = centro + raio * Math.Sin(inicio);
                    double x2 = centro + raio * Math.Cos(fim), y2 = centro + raio * Math.Sin(fim);
                    int grande = fracao > 0.5 ? 1 : 0;
                    fatia.Arco = $"M {F(x1)},{F(y1)} A {F(raio)},{F(raio)} 0 {grande} 1 {F(x2)},{F(y2)}";
                    inicio = fim;
                }

                fatias.Add(fatia);
            }
            return fatias;
        }

        private static string F(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
