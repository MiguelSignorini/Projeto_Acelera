using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class PdfContratoService
    {
        public static void GerarECompletar(Contrato contrato)
        {
            try
            {
                var dialogo = new SaveFileDialog
                {
                    Title = "Salvar contrato em PDF",
                    Filter = "Arquivo PDF (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    FileName = $"Contrato_Sacilotti_{contrato.Codigo}.pdf"
                };
                if (dialogo.ShowDialog() != true) return;

                GerarArquivo(contrato, dialogo.FileName);

                MessageBox.Show($"Contrato salvo em:\n{dialogo.FileName}", "PDF gerado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar PDF: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void GerarArquivo(Contrato contrato, string caminho)
        {
            var locador = ClienteService.Instancia.ObterTodos().FirstOrDefault(c => c.Id == contrato.LocadorId)
                          ?? new Cliente { Nome = contrato.LocadorNome };
            var locatario = ClienteService.Instancia.ObterTodos().FirstOrDefault(c => c.Id == contrato.LocatarioId)
                            ?? new Cliente { Nome = contrato.LocatarioNome };
            var imovel = ImovelService.Instancia.ObterAtivos().FirstOrDefault(i => i.Id == contrato.ImovelId);

            bool ehVenda = contrato.Tipo == TipoContrato.Venda;

            var doc = new Document();
            doc.Info.Title = ehVenda ? ModeloContratoVenda.Titulo(contrato) : ModeloContratoLocacao.Titulo(contrato);
            var normal = doc.Styles[StyleNames.Normal]!;
            normal.Font.Name = "Calibri";
            normal.Font.Size = 10;

            var sec = doc.AddSection();
            sec.PageSetup.PageFormat = PageFormat.A4;
            sec.PageSetup.LeftMargin = Unit.FromPoint(56);
            sec.PageSetup.RightMargin = Unit.FromPoint(56);
            sec.PageSetup.TopMargin = Unit.FromPoint(80);
            sec.PageSetup.BottomMargin = Unit.FromPoint(60);
            sec.PageSetup.HeaderDistance = Unit.FromPoint(14);
            sec.PageSetup.FooterDistance = Unit.FromPoint(20);

            MontarCabecalhoRodape(sec);

            // Título principal do documento
            var pNumero = sec.AddParagraph(ehVenda ? ModeloContratoVenda.Titulo(contrato) : ModeloContratoLocacao.Titulo(contrato));
            pNumero.Format.Alignment = ParagraphAlignment.Right;
            pNumero.Format.SpaceAfter = 6;

            var pTitulo = sec.AddParagraph();
            pTitulo.Format.Alignment = ParagraphAlignment.Center;
            pTitulo.Format.SpaceAfter = 10;
            string tituloTexto = ehVenda
                ? "INSTRUMENTO PARTICULAR DE COMPROMISSO DE COMPRA E VENDA DE IMÓVEL"
                : "CONTRATO DE LOCAÇÃO RESIDENCIAL SEM GARANTIA";
            pTitulo.AddFormattedText(tituloTexto, TextFormat.Bold).Size = 14;

            // Cláusulas dinâmicas (Locação ou Venda)
            var blocos = ehVenda
                ? ModeloContratoVenda.Montar(contrato, locador, locatario, imovel)
                : ModeloContratoLocacao.Montar(contrato, locador, locatario, imovel);

            foreach (var bloco in blocos)
            {
                switch (bloco.Tipo)
                {
                    case TipoBloco.Clausula:
                        var pc = sec.AddParagraph();
                        pc.Format.Alignment = ParagraphAlignment.Center;
                        pc.Format.SpaceBefore = 10;
                        pc.Format.SpaceAfter = 6;
                        pc.Format.KeepWithNext = true;
                        pc.AddFormattedText(bloco.Texto, TextFormat.Bold).Size = 11.5;
                        break;

                    case TipoBloco.Item:
                        var pi = sec.AddParagraph();
                        pi.Format.Alignment = ParagraphAlignment.Justify;
                        pi.Format.SpaceAfter = 8;
                        var m = Regex.Match(bloco.Texto, @"^\d+\.\d+\s?-");
                        if (m.Success)
                        {
                            pi.AddFormattedText(m.Value, TextFormat.Bold);
                            pi.AddText(bloco.Texto.Substring(m.Length));
                        }
                        else
                        {
                            pi.AddText(bloco.Texto);
                        }
                        break;

                    case TipoBloco.SubItem:
                        var ps = sec.AddParagraph(bloco.Texto);
                        ps.Format.LeftIndent = Unit.FromPoint(30);
                        ps.Format.Alignment = ParagraphAlignment.Justify;
                        ps.Format.SpaceAfter = 3;
                        break;
                }
            }

            MontarAssinaturas(sec, contrato, locador, locatario, ehVenda);

            var renderer = new PdfDocumentRenderer { Document = doc };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(caminho);
        }

        // Cabeçalho (logo + dados da imobiliária) e rodapé "Página X de Y" em todas as páginas
        private static void MontarCabecalhoRodape(Section sec)
        {
            var tab = sec.Headers.Primary.AddTable();
            tab.AddColumn(Unit.FromPoint(40));
            tab.AddColumn(Unit.FromPoint(300));
            tab.AddColumn(Unit.FromPoint(99));
            var linha = tab.AddRow();
            linha.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Top;

            var logo = CarregarLogoBase64();
            if (logo != null)
            {
                var img = linha.Cells[0].AddParagraph().AddImage(logo);
                img.LockAspectRatio = true;
                img.Width = Unit.FromPoint(32);
            }

            var pNome = linha.Cells[1].AddParagraph();
            pNome.Format.Font.Size = 9;
            pNome.AddFormattedText(DadosImobiliaria.NomeCabecalho, TextFormat.Bold);
            var pEnd = linha.Cells[1].AddParagraph(DadosImobiliaria.EnderecoCabecalho);
            pEnd.Format.Font.Size = 9;

            var pTel = linha.Cells[2].AddParagraph(DadosImobiliaria.Telefone);
            pTel.Format.Alignment = ParagraphAlignment.Right;
            pTel.Format.Font.Size = 9;
            var pMail = linha.Cells[2].AddParagraph(DadosImobiliaria.Email);
            pMail.Format.Alignment = ParagraphAlignment.Right;
            pMail.Format.Font.Size = 9;

            var rodape = sec.Footers.Primary.AddParagraph();
            rodape.Format.Alignment = ParagraphAlignment.Right;
            rodape.Format.Font.Size = 9;
            rodape.AddText("Página ");
            rodape.AddPageField();
            rodape.AddText(" de ");
            rodape.AddNumPagesField();
        }

        private static void MontarAssinaturas(Section sec, Contrato contrato, Cliente locador, Cliente locatario, bool ehVenda)
        {
            const string Linha = "________________________________________";

            var pData = sec.AddParagraph(ModeloContratoLocacao.LinhaData(contrato));
            pData.Format.Alignment = ParagraphAlignment.Right;
            pData.Format.SpaceBefore = 18;
            pData.Format.SpaceAfter = 14;
            pData.Format.KeepWithNext = true;

            void Titulo(string texto)
            {
                var p = sec.AddParagraph();
                p.Format.Alignment = ParagraphAlignment.Center;
                p.Format.SpaceBefore = 14;
                p.Format.KeepWithNext = true;
                p.AddFormattedText(texto, TextFormat.Bold);
            }

            void Assinante(string nome)
            {
                var p = sec.AddParagraph();
                p.Format.Alignment = ParagraphAlignment.Center;
                p.Format.SpaceBefore = 22;
                p.Format.KeepWithNext = true;
                p.AddText(Linha);
                p.AddLineBreak();
                p.AddText(nome);
            }

            // Assinaturas das partes conforme o tipo de contrato
            Titulo(ehVenda ? "PROMITENTE COMPRADORA" : "LOCATÁRIOS");
            Assinante(locatario.Nome);

            Titulo(ehVenda ? "PROMITENTE VENDEDOR" : "LOCADORES");
            Assinante(locador.Nome);

            Titulo("IMOBILIÁRIA");
            Assinante("Sacilotti Imóveis & Negócios LTDA");

            // Tabela padronizada de testemunhas lado a lado
            Titulo("TESTEMUNHAS");
            var tab = sec.AddTable();
            tab.Format.KeepTogether = true;
            tab.AddColumn(Unit.FromPoint(220));
            tab.AddColumn(Unit.FromPoint(20));
            tab.AddColumn(Unit.FromPoint(220));

            var assin = tab.AddRow();
            assin.Height = Unit.FromPoint(40);
            assin.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Bottom;
            assin.Cells[0].AddParagraph(Linha);
            assin.Cells[2].AddParagraph(Linha);

            var nomes = tab.AddRow();
            nomes.Cells[0].AddParagraph("Nome:");
            nomes.Cells[2].AddParagraph("Nome:");

            var cpfs = tab.AddRow();
            cpfs.Cells[0].AddParagraph("CPF:");
            cpfs.Cells[2].AddParagraph("CPF:");
        }

        private static string? CarregarLogoBase64()
        {
            try
            {
                var recurso = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo.png", UriKind.Absolute));
                if (recurso == null) return null;
                using var ms = new MemoryStream();
                recurso.Stream.CopyTo(ms);
                return "base64:" + Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}