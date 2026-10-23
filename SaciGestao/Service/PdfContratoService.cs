using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class PdfContratoService
    {
        public static void GerarECompletar(Contrato contrato)
        {
            try
            {
                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(50),
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily = new FontFamily("Calibri"),
                    FontSize = 11,
                    LineHeight = 16
                };

                // 1. LOGOTIPO E CABEÇALHO OFICIAL
                var pLogo = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 5) };
                var logo = CarregarLogo();
                if (logo != null)
                {
                    var imagem = new Image
                    {
                        Source = logo,
                        Width = 140,
                        Height = 70
                    };
                    pLogo.Inlines.Add(imagem);
                }

                doc.Blocks.Add(pLogo);

                var pCabecalho = new Paragraph() { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
                pCabecalho.Inlines.Add(new Bold(new Run("SACILOTTI IMÓVEIS & NEGÓCIOS LTDA\n")) { FontSize = 14, Foreground = Brushes.DarkRed });
                pCabecalho.Inlines.Add(new Run("Avenida Coronel Domiciano, nº 534, Centro, Cachoeira Paulista - SP, CEP 12630-000\nContato: (12) 99788-9138 | E-mail: sacilotti.imoveis@gmail.com | CNPJ: 41.795.740/0001-49\n"));
                doc.Blocks.Add(pCabecalho);

                doc.Blocks.Add(new Paragraph(new Run("_________________________________________________________________________________")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 15) });

                // TÍTULO DO CONTRATO
                var pTitulo = new Paragraph(new Bold(new Run($"Contrato de Locação nº {contrato.Codigo}\nCONTRATO DE LOCAÇÃO RESIDENCIAL SEM GARANTIA")))
                {
                    FontSize = 13,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                doc.Blocks.Add(pTitulo);

                // ==================== AS 17 CLÁUSULAS NA ÍNTEGRA ====================

                // CLÁUSULA PRIMEIRA
                AddClausula(doc, "CLÁUSULA PRIMEIRA: DAS PARTES");
                AddParagrafo(doc, $"1.1 - LOCADOR(a): {contrato.LocadorNome}, de nacionalidade Brasileira, portador(a) dos documentos cadastrados, residente e domiciliado(a) no imóvel sob sua propriedade, representado neste ato por Sacilotti Imóveis & Negócios LTDA.");
                AddParagrafo(doc, $"1.2 - LOCATÁRIO(a): {contrato.LocatarioNome}, de nacionalidade Brasileira, devidamente qualificado(a) no cadastro do sistema SaciGestão.");

                // CLÁUSULA SEGUNDA
                AddClausula(doc, "CLÁUSULA SEGUNDA: OBJETO DO CONTRATO");
                AddParagrafo(doc, $"2.1 - Neste ato o(a) LOCADOR(a) se compromete a ceder em locação ao(à) LOCATÁRIO(a) o imóvel localizado à {contrato.ImovelTitulo}.");
                AddParagrafo(doc, "2.2 - O(a) LOCADOR(a) dá em locação ao(à) LOCATÁRIO(a) a posse direta do imóvel, declarando que o mesmo se encontra desembaraçado de dívidas ou gravames, comprometendo-se a não o onerar sob qualquer hipótese.");

                // CLÁUSULA TERCEIRA
                AddClausula(doc, "CLÁUSULA TERCEIRA: DA FINALIDADE");
                AddParagrafo(doc, "3.1 - O imóvel objeto desta locação se destina exclusivamente para fim Residencial, não podendo a sua destinação ser mudada sem consentimento expresso do(a) LOCADOR(a).");

                // CLÁUSULA QUARTA
                AddClausula(doc, "CLÁUSULA QUARTA: PRAZO DA LOCAÇÃO");
                AddParagrafo(doc, $"4.1 - O prazo da presente locação é de {contrato.PrazoMeses} meses, iniciando em {contrato.DataInicio:dd/MM/yyyy} e se encerrando em {contrato.DataFim:dd/MM/yyyy}, data em que o(a) LOCATÁRIO(a) se obriga a restituir o imóvel completamente desocupado e em perfeito estado.");
                AddParagrafo(doc, "4.2 - Não restituindo o imóvel no prazo avençado, pagará o aluguel mensal e encargos contratuais até a efetiva entrega das chaves.");
                AddParagrafo(doc, "4.3 - A entrega do imóvel só se efetivará com o depósito das chaves nas mãos da ADMINISTRADORA (Corretor Willyan Sacilotti).");
                AddParagrafo(doc, "4.4 - As condições vigorarão até que as partes liquidem todas as pendências.");
                AddParagrafo(doc, "4.5 - Findo o prazo sem manifestação, o contrato passará a vigorar por prazo indeterminado.");

                // CLÁUSULA QUINTA
                AddClausula(doc, "CLÁUSULA QUINTA: VALOR DO ALUGUEL E REAJUSTE");
                AddParagrafo(doc, $"5.1 - O valor do aluguel é de {contrato.ValorMensal:C} por mês, para os primeiros 12 (doze) meses, reajustado anualmente pela variação do percentual do {contrato.IndiceReajuste}.");

                // CLÁUSULA SEXTA
                AddClausula(doc, "CLÁUSULA SEXTA: DO PAGAMENTO");
                AddParagrafo(doc, $"6.1 - O(a) LOCATÁRIO(a) obriga-se a pagar pontualmente até o dia {contrato.DiaVencimento} de cada mês.");
                AddParagrafo(doc, "6.2 a 6.10 - O pagamento será feito via boleto bancário emitido pela ADMINISTRADORA. Atrasos superiores a 15 dias incorrerão em 20% de honorários advocatícios. Não será aceito pagamento em cheque.");

                // CLÁUSULA SÉTIMA
                AddClausula(doc, "CLÁUSULA SÉTIMA: DA MULTA E DO ATRASO NO PAGAMENTO");
                AddParagrafo(doc, $"7.1 - O atraso sujeitará multa moratória de {contrato.PercentualMultaAtraso}% sobre o valor do débito, mais juros de 1% ao mês.");
                AddParagrafo(doc, "7.2 e 7.3 - O débito poderá ser levado a protesto em cartório e o nome incluído nos órgãos de proteção ao crédito (SPC/Serasa).");

                // CLÁUSULA OITAVA
                AddClausula(doc, "CLÁUSULA OITAVA: DOS DIREITOS E OBRIGAÇÕES ACESSÓRIAS");
                AddParagrafo(doc, "8.1 a 8.12 - São encargos do LOCATÁRIO: Energia Elétrica, Água, IPTU e Seguro Incêndio. Obrigatoriedade de transferência de titularidade nas concessionárias em até 30 dias.");

                // CLÁUSULA NONA
                AddClausula(doc, "CLÁUSULA NONA: DAS CONDIÇÕES DO IMÓVEL");
                AddParagrafo(doc, "9.1 a 9.22 - O imóvel é entregue em perfeito estado conforme Laudo de Vistoria de Entrada. Proibição de modificações estruturais sem autorização prévia por escrito.");

                // CLÁUSULA DÉCIMA
                AddClausula(doc, "CLÁUSULA DÉCIMA: DOS DIREITOS E OBRIGAÇÕES DOS LOCADORES");
                AddParagrafo(doc, "10.1 a 10.6 - Responsabilidade pela higidez estrutural e despesas extraordinárias.");

                // CLÁUSULA DÉCIMA PRIMEIRA
                AddClausula(doc, "CLÁUSULA DÉCIMA PRIMEIRA: DA ADMINISTRADORA");
                AddParagrafo(doc, "11.1 e 11.2 - Sacilotti Imóveis & Negócios LTDA representada por Willyan Sacilotti dos Santos Siqueira, atuando como gestora e procuradora para todos os fins locatícios.");

                // CLÁUSULA DÉCIMA SEGUNDA
                AddClausula(doc, "CLÁUSULA DÉCIMA SEGUNDA: DA GARANTIA LOCATÍCIA");
                AddParagrafo(doc, "12.1 - O(a) LOCADOR(a) abriu mão da garantia locatícia.");

                // CLÁUSULA DÉCIMA TERCEIRA
                AddClausula(doc, "CLÁUSULA DÉCIMA TERCEIRA: DAS SUBLOCAÇÕES E CESSÃO");
                AddParagrafo(doc, "13.1 - É vedada a sublocação, cessão ou empréstimo do imóvel a terceiros sem prévia anuência por escrito.");

                // CLÁUSULA DÉCIMA QUARTA
                AddClausula(doc, "CLÁUSULA DÉCIMA QUARTA: DA RESCISÃO");
                AddParagrafo(doc, $"14.1 a 14.8 - Multa rescisória equivalente a 3 (três) meses de aluguel proporcional ao período. Isenção de multa após o cumprimento de {contrato.MesesIsencaoMultaRescisao} meses mediante aviso prévio de 30 dias.");

                // CLÁUSULA DÉCIMA QUINTA
                AddClausula(doc, "CLÁUSULA DÉCIMA QUINTA: DO DIREITO DE PREFERÊNCIA");
                AddParagrafo(doc, "15.1 a 15.3 - Direito de preferência do inquilino em caso de venda do imóvel nos termos da Lei 8.245/91.");

                // CLÁUSULA DÉCIMA SEXTA
                AddClausula(doc, "CLÁUSULA DÉCIMA SEXTA: DAS DISPOSIÇÕES FINAIS");
                AddParagrafo(doc, "16.1 a 16.9 - Obrigatoriedade extensiva aos sucessores, citações por via postal e eleição de foro.");

                // CLÁUSULA DÉCIMA SÉTIMA
                AddClausula(doc, "CLÁUSULA DÉCIMA SÉTIMA: DO FORO");
                AddParagrafo(doc, "17.1 - Para todas as questões oriundas deste contrato, será competente o Foro da comarca de Cachoeira Paulista - SP.");

                // ==================== ASSINATURAS ====================
                doc.Blocks.Add(new Paragraph(new Run($"\nCachoeira Paulista - SP, {DateTime.Now:D}.\n\n\n")) { TextAlignment = TextAlignment.Center });

                var pAssinaturas = new Paragraph() { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
                pAssinaturas.Inlines.Add(new Run("________________________________________          ________________________________________\n"));
                pAssinaturas.Inlines.Add(new Bold(new Run($"LOCADOR(A): {contrato.LocadorNome}                          LOCATÁRIO(A): {contrato.LocatarioNome}\n\n\n\n")));
                pAssinaturas.Inlines.Add(new Run("________________________________________________________________________\n"));
                pAssinaturas.Inlines.Add(new Bold(new Run("SACILOTTI IMÓVEIS & NEGÓCIOS LTDA\n(Administradora)")));
                doc.Blocks.Add(pAssinaturas);

                // DISPARAR IMPRESSÃO / PDF DO WINDOWS
                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    IDocumentPaginatorSource idp = doc;
                    pd.PrintDocument(idp.DocumentPaginator, $"Contrato_Sacilotti_{contrato.Codigo}");
                    MessageBox.Show("Contrato oficial de locação gerado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar PDF: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Carrega o logo do jeito que o MainWindow já usa (Resources/logo.png como Resource do projeto).
        // Se não existir ou der qualquer problema, o PDF é gerado sem logo, sem travar.
        private static BitmapImage? CarregarLogo()
        {
            try
            {
                var imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.UriSource = new Uri("pack://application:,,,/Resources/logo.png", UriKind.Absolute);
                imagem.CacheOption = BitmapCacheOption.OnLoad;
                imagem.EndInit();
                return imagem;
            }
            catch
            {
                return null;
            }
        }

        private static void AddClausula(FlowDocument doc, string titulo)
        {
            doc.Blocks.Add(new Paragraph(new Bold(new Run(titulo)))
            {
                FontSize = 11,
                Foreground = Brushes.DarkRed,
                Margin = new Thickness(0, 12, 0, 3)
            });
        }

        private static void AddParagrafo(FlowDocument doc, string texto)
        {
            doc.Blocks.Add(new Paragraph(new Run(texto))
            {
                Margin = new Thickness(0, 0, 0, 4),
                TextAlignment = TextAlignment.Justify
            });
        }
    }
}