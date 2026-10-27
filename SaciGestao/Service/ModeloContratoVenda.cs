using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public static class ModeloContratoVenda
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");
        private const string Branco = "__________";

        private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? Branco : valor.Trim();

        private static string PessoaCompradora(Cliente c)
        {
            return $"{Ou(c.Nome)}, nacionalidade {Ou(c.Nacionalidade)}, {Ou(c.EstadoCivil)}, {Ou(c.Profissao)}, " +
                   $"inscrito(a) no CPF nº {Ou(c.Cpf)} e RG nº {Ou(c.Rg)}, " +
                   $"residente e domiciliado(a) à {Ou(c.Rua)}, nº {Ou(c.Numero)}, bairro {Ou(c.Bairro)}, " +
                   $"na cidade de {Ou(c.Cidade)} – SP, CEP {Ou(c.Cep)}";
        }

        private static string PessoaVendedora(Cliente c)
        {
            return $"{Ou(c.Nome)}, nacionalidade {Ou(c.Nacionalidade)}, {Ou(c.EstadoCivil)}, {Ou(c.Profissao)}, " +
                   $"portador(a) do RG nº {Ou(c.Rg)} e inscrito(a) no CPF nº {Ou(c.Cpf)}, " +
                   $"residente e domiciliado(a) à {Ou(c.Rua)}, nº {Ou(c.Numero)}, Bairro {Ou(c.Bairro)}, " +
                   $"{Ou(c.Cidade)} – SP, CEP {Ou(c.Cep)}";
        }

        public static string Titulo(Contrato c) =>
            $"Contrato de Compra e Venda nº {c.Id} / {c.DataAssinatura.Year}";

        public static List<BlocoContrato> Montar(Contrato c, Cliente vendedor, Cliente comprador, Imovel? imovel)
        {
            string enderecoImovel = imovel == null
                ? Branco
                : $"{Ou(imovel.Endereco)}, nº {Ou(imovel.Numero)}, Bairro {Ou(imovel.Bairro)}, {Ou(imovel.Cidade)} – SP, CEP {Ou(imovel.Cep)}";

            string descricaoImovel = imovel == null ? Branco : (imovel.Descricao ?? "Imóvel residencial/comercial");
            string inscricaoMunicipal = imovel?.MatriculaEdp ?? Branco;
            string matriculaImovel = imovel?.MatriculaSabesp ?? Branco;
            string valorTotalFormatado = $"{Dinheiro(c.ValorMensal)} ({Extenso.Reais(c.ValorMensal)})";

            var b = new List<BlocoContrato>();
            void Cl(string t) => b.Add(new(TipoBloco.Clausula, t));
            void It(string t) => b.Add(new(TipoBloco.Item, t));

            Cl("CLÁUSULA PRIMEIRA: DAS PARTES");
            It($"1.1 - PROMITENTE COMPRADORA: {PessoaCompradora(comprador)}.");
            It($"1.2 - PROMITENTE VENDEDOR: {PessoaVendedora(vendedor)}.");
            It($"1.3 - IMOBILIÁRIA: {DadosImobiliaria.RazaoSocial11}, com sede à {DadosImobiliaria.EnderecoCabecalho}, inscrita no CNPJ nº {DadosImobiliaria.Cnpj}, CRECI {Ou(DadosImobiliaria.CreciJuridica)}, neste ato representada por {DadosImobiliaria.Representante}.");

            Cl("CLÁUSULA SEGUNDA: DO OBJETO");
            It("2.1 - O PROMITENTE VENDEDOR por este Instrumento compromete-se a vender à PROMITENTE COMPRADORA o imóvel a seguir mencionado e caracterizado.");
            It($"2.2 - A PROMITENTE COMPRADORA está comprando {descricaoImovel}, situado na {enderecoImovel}, Inscrição Municipal nº {inscricaoMunicipal}, matrícula nº {matriculaImovel}.");

            Cl("CLÁUSULA TERCEIRA: DO PREÇO E FORMA DE PAGAMENTO");
            It($"3.1 - O preço certo e total pela aquisição objeto deste CONTRATO é de R$ {valorTotalFormatado}.");

            Cl("CLÁUSULA QUARTA: DA SITUAÇÃO DO IMÓVEL");
            It("4.1 - O PROMITENTE VENDEDOR declara que o bem está inteiramente livre e desembaraçado de quaisquer ônus, dívidas, execuções ou penhoras.");
            It("4.2 - O PROMITENTE VENDEDOR entregará o imóvel totalmente livre e desocupado de pessoas e objetos.");

            Cl("CLÁUSULA QUINTA: DA DOCUMENTAÇÃO");
            It("5.1 - O PROMITENTE VENDEDOR compromete-se a apresentar todas as documentações pessoais e do imóvel necessárias à transferência.");

            Cl("CLÁUSULA SEXTA: DA ESCRITURA DEFINITIVA");
            It("6.1 - O PROMITENTE VENDEDOR obriga-se a outorgar a competente Escritura Pública de Transferência de Propriedade.");
            It("6.2 - Correrão por conta exclusiva da PROMITENTE COMPRADORA as despesas de ITBI, emolumentos notariais e registrais.");

            Cl("CLÁUSULA SÉTIMA: DA ENTREGA DO IMÓVEL E DESPESAS");
            It("7.1 - O PROMITENTE VENDEDOR responsabiliza-se por quaisquer débitos de IPTU ou encargos anteriores à entrega da posse.");

            Cl("CLÁUSULA OITAVA: DA IRREVOGABILIDADE");
            It("8.1 - O presente instrumento é celebrado em caráter de irrevogabilidade e irretratabilidade, obrigando herdeiros e sucessores.");

            Cl("CLÁUSULA NONA: DAS HIPÓTESES RESOLUTIVAS");
            It("9.1 - É facultado à PROMITENTE COMPRADORA rescindir este contrato caso sejam detetados impedimentos legais ou ônus sobre o imóvel.");

            Cl("CLÁUSULA DÉCIMA: DAS DECLARAÇÕES");
            It("10.1 - Declara o PROMITENTE VENDEDOR inexistirem contra si protestos ou ações judiciais que afetem a transação.");

            Cl("CLÁUSULA DÉCIMA PRIMEIRA: DA RESCISÃO E MULTA");
            It("11.1 - O descumprimento de cláusulas ensejará multa compensatória de 10% (dez por cento) sobre o valor total da transação em favor da parte inocente.");

            Cl("CLÁUSULA DÉCIMA SEGUNDA: DAS DISPOSIÇÕES GERAIS");
            It("12.1 - As partes elegem o Foro da Comarca de Cachoeira Paulista - SP para dirimir quaisquer dúvidas oriundas deste instrumento.");

            return b;
        }

        private static string Dinheiro(decimal v) => v.ToString("N2", PtBr);
    }
}