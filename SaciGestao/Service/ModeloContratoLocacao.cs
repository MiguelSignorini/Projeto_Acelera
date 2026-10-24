using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public enum TipoBloco { Clausula, Item, SubItem }

    public record BlocoContrato(TipoBloco Tipo, string Texto);

    // Dados da imobiliária (administradora). Os campos vazios saem em branco (______) no PDF
    // para serem preenchidos a mão, até serem informados aqui.
    public static class DadosImobiliaria
    {
        public const string NomeCabecalho = "Sacilotti Imóveis & Negócios LTDA";
        public const string Telefone = "(12) 9978-89138";
        public const string Email = "sacilotti.imoveis@gmail.com";
        public const string EnderecoCabecalho = "Avenida Coronel Domiciano, nº 534, 12630000, Centro, Cachoeira Paulista, SP, São Paulo";

        public const string RazaoSocial11 = "SACILOTTI IMÓVEIS E NEGÓCIOS LTDA";
        public const string Logradouro = "Coronel Domiciano";
        public const string Cnpj = "41.795.740/0001-49";
        public const string CreciJuridica = "";
        public const string Representante = "WILLYAN SACILOTTI DOS SANTOS SIQUEIRA";
        public const string CreciRepresentante = "";
        public const string RgRepresentante = "";
        public const string CpfRepresentante = "";
    }

    // O texto do contrato abaixo segue a minuta "CTR RESIDENCIAL SEM GARANTIA", com os erros de digitação e espaçamento corrigidos.
    // Só o que está entre chaves {} vem do sistema (contrato, clientes e imóvel).
    public static class ModeloContratoLocacao
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");
        private const string Branco = "__________";

        private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? Branco : valor.Trim();

        private static string Pessoa(Cliente c)
        {
                        string portador = "portador (a) da";
            string fimTel = "com o e-mail";
            return $"{Ou(c.Nome)}, de nacionalidade {Ou(c.Nacionalidade)}, {Ou(c.EstadoCivil)}, inscrito(a) sob o CPF nº {Ou(c.Cpf)}, " +
                   $"{portador} cédula de identidade nº {Ou(c.Rg)}, emitida pelo órgão {Ou(c.OrgaoEmissorRg)}, " +
                   $"residente e domiciliado em {Ou(c.Rua)}, nº {Ou(c.Numero)}, {Ou(c.Cep)}, {Ou(c.Bairro)}, {Ou(c.Cidade)}, SP, BR, " +
                   $"com telefone para contato +55 {Ou(c.TelefonePrincipal)}, {fimTel} para contato {Ou(c.Email)}.";
        }

        private static string Comodos(Imovel? i)
        {
            if (i == null) return Branco + ".";

            var partes = new List<string>();
            if (i.Quartos > 0)
            {
                string q = $"{i.Quartos:00} {(i.Quartos == 1 ? "quarto" : "quartos")}";
                if (i.Suites > 0) q += $", sendo {i.Suites:00} {(i.Suites == 1 ? "suíte" : "suítes")}";
                partes.Add(q);
            }
            if (i.Salas > 0) partes.Add(i.Salas == 1 ? "Sala" : $"{i.Salas:00} salas");
            if (i.Cozinhas > 0) partes.Add(i.Cozinhas == 1 ? "Cozinha" : $"{i.Cozinhas:00} cozinhas");
            if (i.Banheiros > 0) partes.Add(i.Banheiros == 1 ? "Banheiro" : $"{i.Banheiros:00} banheiros");
            if (i.Vagas > 0) partes.Add(i.Vagas == 1 ? "garagem" : $"garagem para {i.Vagas:00} vagas");

            if (partes.Count == 0) return string.IsNullOrWhiteSpace(i.Descricao) ? Branco + "." : i.Descricao.Trim();
            if (partes.Count == 1) return partes[0] + ".";
            return string.Join(", ", partes.Take(partes.Count - 1)) + " e " + partes[^1] + ".";
        }

        public static string Titulo(Contrato c) =>
            $"Contrato de Locação nº {c.Id} / {c.DataAssinatura.Year}";

        public static string LinhaData(Contrato c)
        {
            string data = c.DataAssinatura.ToString("dddd, d 'de' MMMM 'de' yyyy", PtBr);
            data = char.ToUpper(data[0], PtBr) + data.Substring(1);
            return $"Cachoeira Paulista, São Paulo {data}.";
        }

        public static string Dinheiro(decimal v) => v.ToString("N2", PtBr);

        public static List<BlocoContrato> Montar(Contrato c, Cliente locador, Cliente locatario, Imovel? imovel)
        {
            string valor = $"{Dinheiro(c.ValorMensal)} ({Extenso.Reais(c.ValorMensal)})";
            string multa = $"{Extenso.Numero(c.PercentualMultaAtraso)}% ({Extenso.Percentual(c.PercentualMultaAtraso)})";
            string meses = $"{c.MesesIsencaoMultaRescisao} ({Extenso.Inteiro(c.MesesIsencaoMultaRescisao)})";
            string enderecoImovel = imovel == null
                ? Branco
                : $"{Ou(imovel.Endereco)}, nº {Ou(imovel.Numero)}, {Ou(imovel.Cep)}, {Ou(imovel.Bairro)}, {Ou(imovel.Cidade)}, {Ou(imovel.Estado)}, BR";

            string creciJ = Ou(DadosImobiliaria.CreciJuridica);
            string indice = Ou(c.IndiceReajuste);

            var b = new List<BlocoContrato>();
            void Cl(string t) => b.Add(new(TipoBloco.Clausula, t));
            void It(string t) => b.Add(new(TipoBloco.Item, t));
            void Sub(string t) => b.Add(new(TipoBloco.SubItem, t));

            Cl("CLÁUSULA PRIMEIRA: DAS PARTES");
            It($"1.1 - LOCADOR(a): {Pessoa(locador)}");
            It($"1.2 - LOCATÁRIO(a): {Pessoa(locatario)}");

            Cl("CLÁUSULA SEGUNDA: OBJETO DO CONTRATO");
            It($"2.1 - Neste ato o(a) LOCADOR(a) se compromete a ceder em locação ao(à) LOCATÁRIO(a) o imóvel localizado à {enderecoImovel}, contendo: {Comodos(imovel)}");
            It("2.2 - O(a) LOCADOR(a) dão em locação ao(à) LOCATÁRIO(a) a posse direta do imóvel, e declara que o mesmo se encontra desembaraçado de dívidas ou gravames, comprometendo-se, enquanto durar o prazo da locação, a não o onerar sob qualquer hipótese ou condição, sob pena de incidir em infração contratual.");

            Cl("CLÁUSULA TERCEIRA: DA FINALIDADE");
            It("3.1 - O imóvel objeto desta locação se destina exclusivamente para fim Residencial, não podendo a sua destinação ser mudada sem consentimento expresso do(a) LOCADOR(a).");

            Cl("CLÁUSULA QUARTA: PRAZO DA LOCAÇÃO");
            It($"4.1 - O prazo da presente locação e que ficou combinado entre os contratantes é de {c.PrazoMeses}, iniciando em {c.DataInicio:dd/MM/yyyy} e se encerrando em {c.DataFim:dd/MM/yyyy}, data em que o(a) LOCATÁRIO(a) se compromete e se obriga a restituir o imóvel ao LOCADOR(a), completamente desocupado de pessoas e coisas e em perfeito estado de conservação e limpeza, tal como o recebe, conforme vistoria precedida pela ADMINISTRADORA, cujo relatório deste faz parte integrante, independente de aviso, notificação ou interpelação, sob pena de incorrer na multa prevista na cláusula décima quarta deste instrumento e sujeitar-se ao disposto no artigo 575 do Código Civil.");
            It("4.2 - Não restituindo o imóvel no prazo aqui avençado, ao LOCADOR(a) pagará enquanto estiver na posse do imóvel, o aluguel mensal e encargos contratuais, reajustado nos termos deste instrumento até efetiva entrega das chaves.");
            It("4.3 - Fica estabelecido que a entrega do imóvel só seja reconhecida e se efetivará com o depósito das chaves nas mãos da ADMINISTRADORA do imóvel que é o corretor de imóveis WILLYAN SACILOTTI DOS SANTOS SIQUEIRA, ou do procurador do(a) LOCADOR(a) e, em qualquer situação, contra recibo.");
            It("4.4 - As cláusulas e condições do presente contrato iniciaram sua vigência na data acima estipulada e vigorarão até que as partes tenham liquidado todas as pendências entre si.");
            It("4.5 - Findo o prazo inicial estipulado no item 4.1 sem manifestação de qualquer das partes em rescindi-lo, o presente instrumento passará a vigorar por prazo indeterminado.");

            Cl("CLÁUSULA QUINTA: VALOR DO ALUGUEL E REAJUSTE");
            It($"5.1 - O valor do aluguel, livremente pactuado entre as partes, é de {valor}, por mês, para os primeiros 12 (doze) meses, e, a partir daí, será reajustado de acordo com a variação do percentual do {indice}, ou pela média da variação dos índices inflacionários do ano corrente ao da execução do aluguel, até o primeiro dia anterior ao pagamento de todos os valores devidos. No caso de extinção ou impedimento do uso deste índice, ocasionado por alguma mudança no âmbito governamental, serão adotados outros que o vierem a substituir ou que a legislação permitir.");

            Cl("CLÁUSULA SEXTA: DO PAGAMENTO");
            It($"6.1 - O(a) LOCATÁRIO(a) se compromete e se obriga a pagar, pontualmente até o dia {c.DiaVencimento}.");
            It("6.2 - Será pago pelo(a) LOCATÁRIO(a) saldo residual referente à data de recebimento das chaves do imóvel, ao dia de vencimento do primeiro aluguel.");
            It($"6.3 - O pagamento deverá ser feito na conta da ADMINISTRADORA do imóvel, por meio de boleto bancário, enviado mensalmente via e-mail e/ou WhatsApp, em nome de SACILOTTI IMÓVEIS E NEGÓCIOS LTDA, CNPJ: {DadosImobiliaria.Cnpj}.");
            It("6.4 - Caso o(a) LOCATÁRIO(a) tenha algum problema referente ao boleto, nesse caso, o mesmo deve entrar em contato com a ADMINISTRADORA com no mínimo 24 (vinte e quatro) horas de antecedência.");
            It("6.5 - A tolerância não se estende aos encargos e impostos, devendo ser pagos em suas datas de vencimento.");
            It("6.6 - A falta de pagamento dos aluguéis e encargos, na data aqui avençada, por si só, constituirá o(a) LOCATÁRIO(a) em mora, independentemente de qualquer notificação, interpelação ou aviso extrajudicial, e caracterizará infração grave que enseja a imediata rescisão do presente contrato, com todas as combinações nele fixadas.");
            It("6.7 - O(a) LOCADOR(a) admitindo em benefício do(a) LOCATÁRIO(a), qualquer atraso no pagamento do aluguel, encargos ou quaisquer outras obrigações que incumba o(a) LOCATÁRIO(a), ou, ainda, no cumprimento de qualquer obrigação aqui ajustada, será tolerância do(a) LOCADOR(a), por mera liberalidade, e não poderá ser considerada como alteração das condições deste contrato, nem dará ensejo à invocação do disposto do artigo 838, Inciso I do Código Civil.");
            It("6.8 - O(a) LOCATÁRIO(a) declara, neste ato, ter pleno conhecimento de que o resgate de recibos posteriores não significa, nem representa quitação de outras obrigações estipuladas no presente contrato, deixadas de cobrar na época certa, principalmente, os encargos fixados neste contrato.");
            It("6.9 - Ocorrendo atraso de 15 (quinze) dias ou mais do vencimento do aluguel, os documentos serão encaminhados ao departamento jurídico para as medidas judiciais competentes, cabendo ao(a) LOCATÁRIO(a) o pagamento dos honorários advocatícios contratuais na base de 20% (vinte por cento) do valor do débito, bem como todas as despesas e custas processuais despendidas com o ajuizamento de processo.");
            It("6.10 - O(a) LOCATÁRIO(a) declara ciência de que NÃO será aceito pagamento em CHEQUE.");

            Cl("CLÁUSULA SÉTIMA: DA MULTA E DO ATRASO NO PAGAMENTO");
            It($"7.1 - O não pagamento do aluguel até a data de vencimento, sujeitará ao(a) LOCATÁRIO(a) o pagamento de uma multa de {multa} sobre o valor do débito, mais juros legais de 1% (um por cento) ao mês e correção monetária, pelo índice do {indice}.");
            It("7.2 - O(a) LOCATÁRIO(a) está ciente que em caso de inadimplência no pagamento do aluguel, o boleto bancário poderá ser levado a protesto em cartório no prazo de 5 (cinco) dias corridos, com a consequente inclusão do nome do(a) LOCATÁRIO(a) nos órgãos de proteção ao crédito SPC e Serasa.");
            It("7.3 - O(a) LOCATÁRIO(a), desde já, declara que está ciente que caso ocorra o inadimplemento de qualquer obrigação oriunda do presente instrumento, poderá ocorrer a negativação do seu nome, decorrente inclusão do nome do(a) LOCATÁRIO(a) nos órgãos de proteção ao crédito SPC e Serasa.");

            Cl("CLÁUSULA OITAVA: DOS DIREITOS E OBRIGAÇÕES ACESSÓRIAS");
            It("8.1 - Além do aluguel, é de responsabilidade do(a) LOCATÁRIO(a) efetuar o pagamento dos seguintes encargos lançados sobre o imóvel aqui locado, que poderão ser exigidos juntamente com o aluguel:");
            Sub("a. Energia Elétrica;");
            Sub("b. Água;");
            Sub("c. IPTU;");
            Sub("d. Do Prêmio de Seguro Complementar Contra Fogo (Seguro Incêndio), o qual deverá ser renovado anualmente, figurando na Apólice o(a) LOCADOR(a) como beneficiários;");
            Sub("e. Os demais encargos e tributos futuros que normalmente incidem ou venham incidir sobre o imóvel durante o prazo de locação.");
            It("8.2 - O não pagamento dos encargos na época determinada facultará ao(a) LOCADOR(a) a justa recusa ao recebimento do aluguel, sujeitando-se o(a) LOCATÁRIO(a) ao pagamento dos ônus decorrentes do inadimplemento, previstos para cada débito, independentemente de eventual ação de despejo.");
            It("8.3 - O não pagamento dos encargos acima mencionados, na época determinada, poderá acarretar a rescisão deste instrumento e, constituirão em mora o(a) LOCATÁRIO(a).");
            It("8.4 - O(a) LOCATÁRIO(a) se obriga a transferir a titularidade da fatura de energia elétrica e fatura de água para seu nome junto às concessionárias, no prazo de 30 (trinta) dias, a contar do recebimento das chaves do imóvel.");
            It("8.5 - A ligação de energia elétrica deverá estar em nome do(a) LOCATÁRIO(a). Não é permitida a transferência para nome de terceiros sem o consentimento por escrito do(a) LOCADOR(a).");
            It("8.6 - O seguro incêndio do imóvel deverá ser pago anualmente juntamente com aluguel. O não pagamento do seguro incêndio ou a não apresentação da apólice à ADMINISTRADORA até a data de vencimento da apólice anterior, acarretará em autorização ao(a) LOCADOR(a) efetuar o pagamento com recursos próprios e efetuar cobrança judicial ou extrajudicial do(a) LOCATÁRIO(a).");
            It("8.7 - O referido seguro incêndio trata-se apenas da proteção da construção não estando incluídos objetos e pertences existentes no interior e exterior do imóvel.");
            It("8.8 - As partes acordam que o seguro incêndio terá como beneficiário o imóvel locado e seu(a) referido(a) LOCADOR(a).");
            It("8.9 - Ficando contas em aberto referentes ao período de locação sobre a responsabilidade do(a) LOCATÁRIO(a), a ADMINISTRADORA ou o(a) LOCADOR(a) solicitará junto à concessionária de energia ou água, o bloqueamento dos débitos em nome do(a) LOCATÁRIO(a).");
            It("8.10 - Não é autorizado o(a) LOCATÁRIO(a) efetuar convênios com terceiros vinculados à conta de Energia.");
            It("8.11 - O(a) LOCATÁRIO(a) é responsável pelas multas, acréscimos e despesas decorrentes de eventuais retenções de avisos de impostos, taxas e encargos mencionados nesta cláusula ou de qualquer cobrança ou obrigação que incida ou venha a incidir sobre o imóvel locado.");
            It("8.12 - Todo débito que originar deste contrato e desta locação, inclusive aluguéis e encargos, estarão sujeitos à correção monetária e juros de mora de 1% (um por cento) ao mês, que incidirão sobre os débitos apurados, independentes de outras multas ou acréscimos que sobre o débito levantado venham recair.");

            Cl("CLÁUSULA NONA: DAS CONDIÇÕES DO IMÓVEL");
            It("9.1 - O(a) LOCATÁRIO(a) declara, desde já, que vistoriaram o imóvel objeto da locação e que o mesmo se encontra em condição de uso e se compromete e se obriga a devolver o aludido imóvel em idênticas condições, ao término desta locação e, se isso não ocorrer se obriga a reparar ao(a) LOCADOR(a) os danos que sejam constatados no imóvel através de laudo de vistoria.");
            It("9.2 - O(a) LOCATÁRIO(a) se obriga a restituir o imóvel, finda a locação, no estado em que recebeu, conforme observado as suas características descritas no Termo de Vistoria de Entrada que, antes da entrega das chaves ao(a) LOCATÁRIO(a), foi realizada e devidamente assinada e acordada entre as partes. No momento da restituição das chaves, proceder-se-á uma nova vistoria, e sendo encontrados danos, ressalvados aqueles decorrentes de deterioração pelo uso normal, deverá o(a) LOCATÁRIO(a) providenciar os reparos para que o imóvel retorne ao mesmo estado quando iniciou até a data da efetiva devolução das chaves.");
            It("9.3 - O laudo de vistoria de saída será elaborado pela ADMINISTRADORA, após a entrega do imóvel pelo(a) LOCATÁRIO(a), e, caso convenha o(a) LOCATÁRIO(a), este poderá acompanhar a vistoria, porém, mesmo que isso não ocorra, o referido laudo procedido somente pela ADMINISTRADORA, prevalecerá para todos os efeitos legais.");
            It("9.4 - O(a) LOCATÁRIO(a) declara que, por ocasião da entrega das chaves, constatou o estado do imóvel, conforme laudo que assina juntamente com este contrato. Qualquer ressalva do(a) LOCATÁRIO(a) quanto ao estado do imóvel, deverá ser informada assim que houver a retirada das chaves no início da locação, constando no Termo de Vistoria de Entrada.");
            It("9.5 - Não poderá o(a) LOCATÁRIO(a) modificar a disposição interna dos cômodos do imóvel, nem sua fachada ou qualquer de sua parte externa, bem como, a qualidade e a cor das pinturas internas ou externas, o formato, a cor e qualidade das portas, janelas e batentes, sem o consentimento, por escrito, do(a) LOCADOR(a).");
            It("9.6 - Quaisquer estragos ocasionados no imóvel e sua instalação, bem como, as despesas a que o(a) LOCADOR(a) for obrigado(a) a desembolsar por eventuais modificações feitas no imóvel, por iniciativa do(a) LOCATÁRIO(a), não ficam compreendidas na multa da cláusula décima quarta, mas serão pagas à parte.");
            It("9.7 - O(a) LOCATÁRIO(a) obriga-se fazer às suas custas e as despesas que porventura sejam necessários para manutenção e conservação do imóvel locado, especialmente: pintura, reparos e manutenções que sejam causadas pelo uso do(a) LOCATÁRIO(a), mantendo tudo em perfeito estado de funcionamento para assim devolver o imóvel, quando findo e rescindido o presente contrato. Todo material a ser empregado para o cumprimento desta cláusula deverá ser da mesma qualidade, tipo e padrões ou equivalente dos encontrados no imóvel, quando recebeu. O não cumprimento do aqui estipulado implicará em infração contratual, sujeitando o(a) LOCATÁRIO(a) ao pagamento da multa fixada na cláusula décima quarta sem prejuízo dos demais direitos do(a) LOCATÁRIO(a).");
            It("9.8 - O(a) LOCATÁRIO(a) é responsável por qualquer dano que venha sofrer o imóvel objeto desta locação, inclusive os causados por incêndio, inundação ou qualquer outra espécie, desde que tenha sido o causador do dano ou tenha, de qualquer forma, concorrido para o seu acontecimento, quer seja por sua culpa, imprudência, negligência ou imperícia ou dos seus prepostos.");
            It("9.9 - O(a) LOCATÁRIO(a) salva as obras que importem na segurança do imóvel, obrigam-se por todas as outras, devendo trazer o imóvel locado em boas condições de higiene e limpeza, com os aparelhos sanitários e de iluminação, pinturas, telhados, vidraças, fechos e fechaduras, torneiras, pias, banheiros, ralos e demais acessórios, em perfeito estado de conservação e funcionamento, para assim restituí-los quando findo ou rescindido este contrato, sem direito à retenção ou indenização por quaisquer benfeitorias, ainda que útil necessária ou voluntária, as quais ficarão, desde logo, incorporadas ao imóvel.");
            It("9.10 - Fica facultado ao(a) LOCADOR(a) o direito de optar pela reposição do imóvel, em suas condições físicas e estéticas originais;");
            It("9.11 - Qualquer anormalidade que porventura venha a surgir no imóvel no que se refere à solidez de sua construção, ou uso de suas partes componentes deverá ser imediatamente comunicada, por escrito, à ADMINISTRADORA.");
            It("9.12 - O(a) LOCATÁRIO(a) desde já faculta ao(a) LOCADOR(a), ADMINISTRADOR(a) ou representante realizar a vistoria do imóvel locado, quando entender conveniente mediante dia e hora marcada, sob pena de incorrer em infração contratual sujeitando-se as penalidades legais e ao pagamento da multa fixada na cláusula décima quarta, além de ser compelido judicialmente a fazê-lo.");
            It("9.13 - O(a) LOCATÁRIO(a) é obrigado(a) à sua própria conta as exigências das autoridades Federais, Estaduais e Municipais, Polícia Militar (Corpo de Bombeiro), inclusive as do Departamento de Saúde e Higiene do Trabalho, sem direito a qualquer indenização pelas obras que executar, as quais ficarão desde já incorporadas ao imóvel, ficando a seu cargo todas as reparações previstas no artigo 23, inciso III e V, da lei 8.245/91, sem direito a qualquer reembolso ou de exercitar retenções.");
            It("9.14 - Nenhuma intimação do serviço sanitário ou qualquer outro órgão competente será motivo para o(a) LOCATÁRIO(a) abandonar o imóvel ou pedir a rescisão deste contrato, salvo após prévia vistoria judicial, que apure estar à construção ameaçada de ruir ou causar danos físicos e materiais.");
            It("9.15 - A introdução de qualquer obra ou benfeitoria no imóvel por iniciativa do(a) LOCATÁRIO(a), só poderá ocorrer com a autorização, por escrito, do(a) LOCADOR(a).");
            It("9.16 - Qualquer benfeitoria que o(a) LOCATÁRIO(a) promover no imóvel, quer seja útil, necessária ou voluntária, ficarão incorporadas ao imóvel, NÃO havendo direito à retenção, reembolso ou indenização sob qualquer título.");
            It("9.17 - Caso não convenha ao(a) LOCADOR(a) a permanência de quaisquer obras ou benfeitorias realizadas pelo(a) LOCATÁRIO(a), mesmo necessárias ou consentidas, deverá o(a) LOCATÁRIO(a), uma vez finda a locação, removê-las às suas expensas, de modo a devolver o imóvel nas mesmas condições em que o recebeu, conforme a aplicação no disposto Cláusula nona - Item 9.2.");
            It("9.18 - Obriga-se desde já o(a) LOCATÁRIO(a), a respeitar os regulamentos e leis vigentes, bem como o direito de vizinhança, evitando a prática de quaisquer atos que possam perturbar a tranquilidade ou ameaçar a saúde pública, sob pena de, caso não use o imóvel dentro das normas e leis vigentes, ser ingressada ação de despejo por infração contratual.");
            It("9.19 - Fica proibido o uso do imóvel para fins que produzam qualquer tipo de emissão sonora mesmo em horário comercial, sob pena de rescisão contratual acarretada de pagamento de multa rescisória.");
            It("9.20 - Havendo regulamento especial para o imóvel, notadamente como convenção e regimento interno do condomínio ou de associação de moradores, o(a) LOCATÁRIO(a) se obriga a cumpri-lo e respeitá-lo integralmente.");
            It("9.21 - No caso de desapropriação do imóvel locado, ficará o(a) LOCADOR(a), desobrigado(a) de todas as cláusulas deste contrato, ressalvados o(a) LOCATÁRIO(a), tão somente, a faculdade de haver do poder expropriante a indenização a que porventura tiver direito.");
            It("9.22 - Obriga-se o(a) LOCATÁRIO(a), no ato da entrega das chaves do imóvel a apresentação das duas últimas contas de água, energia e gás (se houver), devidamente quitadas, respondendo de qualquer forma, até a data da desocupação e mesmo após ela por tais despesas durante o período locativo, podendo ficar a entrega das chaves vinculada à apresentação das contas mencionadas devidamente quitadas.");

            Cl("CLÁUSULA DÉCIMA: DOS DIREITOS E OBRIGAÇÕES DOS LOCADORES");
            It("10.1 - O(a) LOCADOR(a) se obriga a transferir para seu nome a conta de consumo de energia elétrica e água junto à concessionária, no prazo de 30 (trinta) dias, a contar da assinatura do TERMO DE RESCISÃO DO CONTRATO DE LOCAÇÃO RESIDENCIAL.");
            It("10.2 - O desgaste no imóvel causado pela ação do tempo e o desgaste natural é de responsabilidade do(a) LOCADOR(a).");
            It("10.3 - O(a) LOCADOR(a) é responsável pela higidez e manutenção estrutural do imóvel, bem como responde pelos vícios ou defeitos anteriores à locação.");
            It("10.4 - O(a) LOCADOR(a) se compromete a entregar o imóvel ao(a) LOCATÁRIO(a) em condições perfeitas e adequadas de conservação, uso e funcionamento, em conformidade com o Termo de Vistoria de Entrada, que é parte integrante deste contrato.");
            It("10.5 - Fica assegurada ao(a) LOCADOR(a), através de seus representantes ou preposto, a vistoriar periodicamente o imóvel locado com dia e hora marcada, durante todo prazo de locação, devendo, portanto, informar à ADMINISTRADORA, com antecedência de 3 (três) dias da data em que promoverá a vistoria e a identificação do seu preposto ou representante, caso não compareça pessoalmente.");
            It("10.6 - É de responsabilidade do(a) LOCADOR(a) pagar as despesas extraordinárias de condomínio/associação/fundo de reserva (se houver), bem como as contas durante o período em que o imóvel estiver desocupado.");

            Cl("CLÁUSULA DÉCIMA PRIMEIRA: DA ADMINISTRADORA");
            It($"11.1 - {DadosImobiliaria.RazaoSocial11}, com sede à Avenida {DadosImobiliaria.Logradouro}, nº 534, bairro Centro, na cidade de Cachoeira Paulista - SP, CEP 12630-000, inscrita no CNPJ nº {DadosImobiliaria.Cnpj}, CRECI {creciJ} - J, neste ato representada por {DadosImobiliaria.Representante}, brasileiro, divorciado, corretor de imóveis inscrito pelo CRECI/SP nº {Ou(DadosImobiliaria.CreciRepresentante)} F, inscrito no RG nº {Ou(DadosImobiliaria.RgRepresentante)} e CPF nº {Ou(DadosImobiliaria.CpfRepresentante)}, doravante designada simplesmente ADMINISTRADORA.");
            It($"11.2 - O(a) LOCADOR(a) e LOCATÁRIO (a) concedem à ADMINISTRADORA, {DadosImobiliaria.Representante}, CPF nº {Ou(DadosImobiliaria.CpfRepresentante)}, poderes para representá-lo(a) nesta locação, podendo receber, pagar, dar quitações, efetuar acordos, notificar, assinar documentos e aditamentos, ou seja, administrar e gerir sua propriedade objeto deste contrato, troca de titularidade e o que mais se fizer necessário junto à EDP UC ({Ou(c.UC_Energia)}) e SABESP({Ou(c.UC_Agua)}).");

            Cl("CLÁUSULA DÉCIMA SEGUNDA: DA GARANTIA LOCATÍCIA");
            It("12.1 - O(a) LOCADOR(a) abriu mão da garantia locatícia, sendo LOCATÁRIO(a) indicado por ele para essa locação.");

            Cl("CLÁUSULA DÉCIMA TERCEIRA: DAS SUBLOCAÇÕES E CESSÃO À TERCEIROS");
            It("13.1 - É expressamente vedado o(a) LOCATÁRIO(a) sublocar, arrendar, doar, ceder, emprestar, enfim, transferir, a qualquer título, o imóvel locado e este contrato a terceiros, no todo, em parte, ou dar destinação diversa do fim previsto neste instrumento, sem a prévia anuência, por escrito, do(a) LOCADOR(a), sob pena de rescisão e sem prejuízo de incorrer na multa estabelecida neste contrato.");

            Cl("CLÁUSULA DÉCIMA QUARTA: DA RESCISÃO");
            It("14.1 - Fica estipulada a multa equivalente a três valores do aluguel mensal vigente na ocasião da rescisão ou infração deste instrumento, na qual incorrerá a parte que infringir qualquer cláusula deste contrato, com a faculdade para a parte inocente, de poder considerar simultaneamente rescindida a locação, independentemente de qualquer formalidade judicial ou extrajudicial. A multa será sempre devida e paga proporcional, seja qual for o prazo já decorrido do presente contrato, ficando claro que o pagamento dessa multa não exime o(a) LOCATÁRIO(a) do pagamento dos aluguéis e demais encargos em atraso, indenizações por danos causados no imóvel e todo débito originado da avença aqui pactuada.");
            It("14.2 - Caso a rescisão seja motivada pelo(a) LOCATÁRIO(a), não perderá o(a) LOCADOR(a) com a cobrança ou recebimento da multa compensatória, o direito de exigir, o que lhe for devido de alugueres, e bem assim, o de cobrar pelos estragos porventura constatados, não se eximindo, ainda, o(a) LOCATÁRIO(a), em qualquer hipótese, das demais obrigações contratuais.");
            It($"14.3 - Fica ajustado que a partir de {meses} meses de cumprimento contratual, o(a) LOCATÁRIO(a) poderá desocupar o imóvel e neste caso fica isentos do pagamento da multa rescisória. Para a desocupação isenta de multa, será necessário o aviso por escrito ao(a) LOCADOR(a) com antecedência de 30 (trinta) dias antes da entrega das chaves.");
            It($"14.4 - Na hipótese de rescisão pelo(a) LOCATÁRIO(a), antes do término do prazo convencionado na Cláusula quarta, a multa pactuada na Cláusula décima quarta - Item 14.1, será devida somente para rescisão antes dos {meses} primeiros meses de contrato.");
            It("14.5 - Se o(a) LOCATÁRIO(a) abandonar o imóvel objeto do presente contrato, o mesmo se rescindirá de imediato, independentemente de qualquer notificação ou interpelação judicial ou extrajudicial.");
            It("14.6 - O(a) LOCATÁRIO(a) deverá avisar a ADMINISTRADORA por escrito com antecedência de 30 (trinta) dias seu interesse em desocupar o imóvel, obrigando-se a restituí-lo inteiramente livre e desocupado de pessoas e coisas, em perfeito estado de conservação e limpeza.");
            It("14.7 - Após a desocupação do imóvel, decorrido o prazo acima, o(a) LOCATÁRIO(a), deverá marcar dia e hora com a ADMINISTRADORA, por escrito, para que proceda à vistoria de desocupação.");
            It("14.8 - Se a ADMINISTRADORA encontrar, por meio de vistoria que vier a fazer, quaisquer defeitos ou danos, poderá recusar-se a receber as chaves, correndo o aluguel, impostos, taxa e demais encargos da locação, por conta do(a) LOCATÁRIO(a), até que fiquem plenamente satisfeitas as exigências do presente contrato.");

            Cl("CLÁUSULA DÉCIMA QUINTA: DO DIREITO DE PREFERÊNCIA");
            It("15.1 - Na hipótese de o(a) LOCADOR(a) vier a colocar o imóvel a venda, nos termos da Lei 8.245/91, será assegurado o(a) LOCATÁRIO(a) o direito de preferência para aquisição do mesmo, nas mesmas condições, sendo que no caso de não o exercer, desde já autoriza a visitação ao imóvel de pessoas interessadas, em horário comercial, previamente marcado entre a ADMINISTRADORA e o(a) LOCATÁRIO(a).");
            It("15.2 - O(a) LOCADOR(a) expressamente se obriga no caso de alienação do presente imóvel e na vigência do contrato ou da prorrogação, a fazer constar na respectiva Escritura de Compra e Venda a existência deste instrumento contratual, bem como a obrigação de o novo proprietário adquirente respeitá-lo em todas as suas cláusulas.");
            It("15.3 - O(a) LOCATÁRIO(a) está ciente que em caso de venda do imóvel a terceiros, o(a) LOCATÁRIO(a) terá um prazo de 90 (noventa) dias para desocupar o imóvel, e será apurado a multa de rescisão contratual em conformidade com o acordo deste instrumento.");

            Cl("CLÁUSULA DÉCIMA SEXTA: DAS DISPOSIÇÕES FINAIS");
            It("16.1 - Para todas as dúvidas existentes e decorrentes do acordo ora pactuado, LOCADOR(a) e LOCATÁRIO(a), devem entrar em contato com a ADMINISTRADORA.");
            It("16.2 - O presente contrato obriga as partes, seus sucessores e/ou remanescentes a qualquer título, os quais deverão cumpri-lo em todos os seus termos e condições.");
            It("16.3 - Qualquer omissão ou tolerância das partes em exigir o estrito cumprimento das obrigações ora avençadas ou em exercer qualquer direito decorrente deste contrato não constituirá novação ou renúncia, nem afetará seu direito de exercê-lo a qualquer tempo.");
            It("16.4 - O presente contrato é assinado em caráter irrevogável e irretratável, obrigando as partes e seus sucessores a qualquer título inclusive ao seu integral cumprimento.");
            It("16.5 - Obriga-se o(a) LOCATÁRIO(a) no curso da locação, a satisfazer a todas as exigências dos Poderes Públicos a que der causa, não motivando elas a rescisão deste contrato.");
            It("16.6 - O(a) LOCATÁRIO(a), expressamente, autoriza ao(a) LOCADOR(a) a proceder a sua citação inicial, interpelações, intimações, notificações ou qualquer outro ato de comunicação processual, por via postal, em toda e qualquer ação judicial ou procedimento extrajudicial, decorrente da relação locatícia aqui ajustada, tudo nos exatos termos e em conformidade com o Inciso IV do artigo 58 da Lei 8245/91.");
            It("16.7 - O endereço do(a) LOCATÁRIO(a) para recebimento de correspondência, citações ou notificações judiciais ou extrajudiciais, será sempre do imóvel locado.");
            It("16.8 - Tudo quanto foi devido em razão deste contrato e, que não comporte o processo executivo, será cobrado em ação competente, ficando a cargo do devedor, em qualquer caso, os honorários do advogado, que o credor constitui para a ressalva dos seus direitos. Fica estipulado em 20% (vinte por cento) o honorário de advogado, caso o(a) LOCADOR(a), tenha de perseguir seus direitos em juízo, independente da multa contratual estabelecida, além das despesas e custas judiciais e extrajudiciais.");
            It("16.9 - Os contratantes declaram, para todos os efeitos legais, aceitarem os termos, cláusulas, e condições deste instrumento, que afirmam ser uma manifestação de suas vontades, e ratificando o presente contrato de locação, se comprometem a cumpri-lo fielmente, por si, seus herdeiros e sucessores.");

            Cl("CLÁUSULA DÉCIMA SÉTIMA: DO FORO");
            It("17.1 - Para todas as questões oriundas deste contrato, será competente o Foro da cidade de Cachoeira Paulista - SP, com renúncia de qualquer outro, por mais especial que se apresente. Assim, por estarem justos e contratados, assinam o presente, em 03 (três) vias, de igual teor e forma, na presença das testemunhas signatárias, para que surta seus efeitos legais e jurídicos.");

            return b;
        }
    }

    // Números por extenso (pt-BR) para valores em reais, percentuais e prazos.
    public static class Extenso
    {
        private static readonly string[] Unidades =
            { "zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove", "dez", "onze", "doze",
              "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove" };
        private static readonly string[] Dezenas =
            { "", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa" };
        private static readonly string[] Centenas =
            { "", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos" };

        private static string Ate999(int n)
        {
            if (n == 0) return "";
            if (n == 100) return "cem";
            var partes = new List<string>();
            if (n >= 100) partes.Add(Centenas[n / 100]);
            n %= 100;
            if (n > 0)
            {
                if (n < 20) partes.Add(Unidades[n]);
                else
                {
                    string d = Dezenas[n / 10];
                    partes.Add(n % 10 == 0 ? d : d + " e " + Unidades[n % 10]);
                }
            }
            return string.Join(" e ", partes);
        }

        public static string Inteiro(long n)
        {
            if (n == 0) return "zero";
            if (n < 0) return "menos " + Inteiro(-n);

            var grupos = new List<string>();
            long milhoes = n / 1_000_000, milhares = (n / 1000) % 1000, resto = n % 1000;

            if (milhoes > 0) grupos.Add(Ate999((int)milhoes) + (milhoes == 1 ? " milhão" : " milhões"));
            if (milhares > 0) grupos.Add(milhares == 1 ? "mil" : Ate999((int)milhares) + " mil");
            if (resto > 0) grupos.Add(Ate999((int)resto));

            // "e" entre os grupos quando o último é menor que 100 ou centena exata (ex.: mil e quinhentos)
            if (grupos.Count == 1) return grupos[0];
            bool ligaComE = resto > 0 && (resto < 100 || resto % 100 == 0);
            if (resto == 0) return string.Join(" ", grupos);
            return string.Join(" ", grupos.Take(grupos.Count - 1)) + (ligaComE ? " e " : " ") + grupos[^1];
        }

        public static string Reais(decimal valor)
        {
            long reais = (long)Math.Floor(valor);
            int centavos = (int)Math.Round((valor - reais) * 100m);

            string r = reais == 0 && centavos > 0 ? "" : Inteiro(reais) + (reais == 1 ? " real" : (reais % 1_000_000 == 0 && reais > 0 ? " de reais" : " reais"));
            string c = centavos > 0 ? Inteiro(centavos) + (centavos == 1 ? " centavo" : " centavos") : "";

            if (r.Length > 0 && c.Length > 0) return r + " e " + c;
            return r.Length > 0 ? r : c;
        }

        // 10 -> "10"; 2,5 -> "2,5"
        public static string Numero(decimal v) =>
            v == Math.Floor(v) ? ((long)v).ToString(CultureInfo.InvariantCulture) : v.ToString("0.##", new CultureInfo("pt-BR"));

        // 10 -> "dez por cento"; 2,5 -> "dois vírgula cinco por cento"
        public static string Percentual(decimal v)
        {
            if (v == Math.Floor(v)) return Inteiro((long)v) + " por cento";

            string[] partes = v.ToString("0.##", CultureInfo.InvariantCulture).Split('.');
            string dec = string.Join(" ", partes[1].Select(ch => Unidades[ch - '0']));
            return Inteiro(long.Parse(partes[0], CultureInfo.InvariantCulture)) + " vírgula " + dec + " por cento";
        }
    }
}
