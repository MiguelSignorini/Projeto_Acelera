# SaciGestao — Documentação da Estrutura do Projeto

Sistema desktop de gestão imobiliária da **Sacilotti Imóveis & Negócios**. Cadastro de imóveis (com fotos e CIB), clientes (com foto) e contratos de locação e venda (à vista ou parcelada), módulo **Financeiro** (pagamentos e parcelas), dashboard, configurações de perfil e exportação de contrato em PDF. Sem banco de dados: persistência em arquivos JSON.

---

## 1. Visão geral técnica

| Item | Valor |
|------|-------|
| Linguagem | C# (`Nullable` e `ImplicitUsings` habilitados) |
| Framework | `net10.0-windows`, WPF (`OutputType = WinExe`) |
| Arquitetura | MVVM + camada de Services (singletons em memória) |
| Persistência | `System.Text.Json` em `SaciGestao/Json/*.json` |
| Pacotes NuGet | `PDFsharp-MigraDoc-WPF 6.2.4` (geração de PDF); `CommunityToolkit.Mvvm 8.4.2` (referenciado, **não usado** — os ViewModels usam `ViewModelBase`/`RelayCommand` próprios) |
| Solução | `SaciGestao/SaciGestao.slnx` |
| Logins de teste | `admin`, `carolina` e `enari`, todos com senha `123` (senhas guardadas como hash — ver §6) |
| Idioma/cultura | pt-BR (valores monetários, datas) |

---

## 2. Árvore de pastas

```
Projeto_Acelera/
├── README.md                      Documentação de uso/funcionalidades
├── ESTRUTURA_DO_PROJETO.md        (este arquivo)
├── .gitignore                     Ignora bin/, obj/, .vs/, *.suo, *.user etc.
└── SaciGestao/
    ├── SaciGestao.slnx / .csproj / .csproj.user
    ├── App.xaml / App.xaml.cs             Ponto de entrada, tratamento de erro global e salvamento ao fechar
    ├── AssemblyInfo.cs                    Tema WPF (ThemeInfo)
    ├── MainWindow.xaml / .cs              Janela única: login + menu lateral + área de conteúdo
    ├── Models/                            Entidades de dados
    │   ├── Cliente.cs              (também contém ClienteService e FiltroValorParaBoolConverter)
    │   ├── Imovel.cs
    │   ├── Contrato.cs
    │   ├── Pagamento.cs            Pagamento registrado (vai para pagamentos.json)
    │   ├── MovimentacaoFinanceira.cs   Linha da tabela do Financeiro (não é gravada)
    │   ├── ContratoFinanceiro.cs   Resumo financeiro de um contrato + ParcelaContrato (não é gravado)
    │   └── Usuario.cs
    ├── Service/                           Regras de negócio e dados em memória
    │   ├── Persistencia.cs                Leitura/gravação JSON, pastas Json e Fotos
    │   ├── ImovelService.cs
    │   ├── ContratoService.cs
    │   ├── FinanceiroService.cs           Cronograma de parcelas, pagamentos, receita do mês
    │   ├── UsuarioService.cs              Login, hash de senhas, recuperação
    │   ├── PdfContratoService.cs          Geração do PDF (MigraDoc)
    │   ├── ModeloContratoLocacao.cs       Cláusulas da locação + Extenso + DadosImobiliaria
    │   └── ModeloContratoVenda.cs         Cláusulas da compra e venda
    ├── ViewModels/                        Lógica de tela (MVVM)
    │   ├── ViewModelBase.cs               INotifyPropertyChanged + SetProperty
    │   ├── RelayCommand.cs                ICommand genérico
    │   ├── LoginViewModel.cs
    │   ├── DashboardViewModel.cs
    │   ├── ImoveisViewModel.cs
    │   ├── ClientesViewModel.cs
    │   ├── ContratosViewModel.cs
    │   └── FinanceiroViewModel.cs
    ├── Views/                             Telas XAML (+ code-behind mínimo)
    │   ├── LoginView.xaml(.cs)            UserControl dentro da MainWindow
    │   ├── DashboardView.xaml(.cs)        UserControl
    │   ├── ImoveisView.xaml(.cs)          UserControl (lista/form/detalhes/galeria)
    │   ├── ClientesView.xaml(.cs)         UserControl
    │   ├── ContratosView.xaml(.cs)        UserControl
    │   ├── FinanceiroView.xaml(.cs)       UserControl (indicadores, contratos ativos, pagamentos, pop-ups)
    │   ├── ConfiguracoesView.xaml(.cs)    UserControl (perfil, senha, recuperação, sessão)
    │   ├── Dialogo.xaml(.cs)              Janela de aviso/confirmação no visual do sistema
    │   ├── FotoZoomWindow.xaml(.cs)       Janela de zoom de foto
    │   └── ImovelFormWindow.xaml(.cs)     Formulário antigo (NÃO usado mais — ver §10)
    ├── Helpers/
    │   ├── Mascara.cs                     Máscaras de digitação (CPF, RG, telefone, CEP, inteiro, decimal)
    │   ├── Adaptavel.cs                   Layout adaptável (PainelAdaptavel, grades e colunas de tabela)
    │   ├── Responsivo.cs                  Modo compacto do menu lateral
    │   └── Conversores.cs                 IguaisConverter, TextoParaVisibilidadeConverter
    ├── Converters/
    │   ├── CaminhoParaImagemConverter.cs  nome/caminho de foto → BitmapImage
    │   └── TextoAmigavelConverter.cs      "Locacao"→"Locação", "EmAtendimento"→"Em Atendimento"
    ├── Resources/
    │   ├── Tema.xaml                      Cores e estilos compartilhados (ResourceDictionary global)
    │   ├── Logo4.png                      Logo clara (menu lateral e login, fundo escuro)
    │   └── logo.png                       Logo escura (cabeçalho do contrato em PDF, fundo branco)
    ├── Json/                              Dados (entrada)
    │   ├── clientes.json, imoveis.json, contratos.json, pagamentos.json, usuarios.json
    │   └── Comparacao/                    Mesmos arquivos gravados ao fechar pelo "Sair" (ver §6)
    └── Fotos/                             Fotos dos imóveis (nome = GUID sem hífens + extensão)
        ├── clientes/                      Fotos dos clientes
        └── usuarios/                      Fotos de perfil dos usuários
```

> Namespaces: `SaciGestao`, `SaciGestao.Models`, `SaciGestao.Service`, `SaciGestao.ViewModels`, `SaciGestao.Views`, `SaciGestao.Converters`, `SaciGestao.Helpers`. Observação: a pasta chama-se `Service` (singular) e o namespace também.

---

## 3. Fluxo de execução

```
App.OnStartup
  ├─ registra o tratamento de erro global (grava o detalhe em %TEMP%\SaciGestao_erro.log e mostra um Dialogo)
  ├─ força a criação dos singletons: ImovelService, ClienteService, ContratoService, UsuarioService
  │    (cada um lê seu JSON; se ausente/vazio/corrompido → dados de exemplo)
  └─ new MainWindow().Show()         (janela única, começa mostrando a LoginView)

LoginView ──(LoginViewModel.LoginRealizado)──► MainWindow.Entrar(usuario): esconde o login, mostra o sistema

MainWindow
  ├─ barra superior: "Bem-vindo, <nome>", data de hoje e foto do usuário (abre Configurações)
  ├─ menu lateral (RadioButtons): rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro
  │    Menu_Checked → AreaConteudo.Content = new <X>View()
  ├─ Navegar(nomeMenu, abrirNovo) usado pelos atalhos do Dashboard
  ├─ "Salvar" (rodapé do menu) → grava nos JSON principais (pasta Json) e fecha
  └─ "Sair" (rodapé do menu) → fecha; os JSON vão para Json/Comparacao
  Responsivo: ≥1000 px menu completo | 760–1000 px só ícones | <760 px menu vira gaveta (☰)

Encerramento (Salvar / Sair / X da janela / desligar Windows)
  App.OnExit / OnSessionEnding → SalvarTudo()
     → Imovel/Financeiro/Cliente/Contrato/UsuarioService.Salvar() → Persistencia.Salvar(...)
```

Os dados vivem **somente em memória** durante a sessão e só são gravados ao fechar. Encerramento abrupto = perda das alterações da sessão.

---

## 4. Models (entidades)

### 4.1 `Cliente` (`Models/Cliente.cs`)
Enums: `TipoCliente { Proprietário, Comprador, Inquilino }`, `StatusCliente { Ativo, EmAtendimento, Inativo }`.

| Propriedade | Tipo | Observação |
|---|---|---|
| `Id` | int | autoincremento no service |
| `Nome`, `Cpf`, `Rg`, `OrgaoEmissorRg`, `Nacionalidade`, `EstadoCivil` | string | |
| `DataNascimento` | DateTime? | |
| `TelefonePrincipal`, `TelefoneSecundario`, `Email` | string | |
| `FotoCaminho` | string? | `"clientes/<arquivo>"` dentro de `Fotos/`; `null` = sem foto (mostra as iniciais) |
| `Tipo` | TipoCliente | |
| `Status` | StatusCliente | padrão `Ativo` |
| `Profissao`, `Empresa` | string | |
| `Rua`, `Numero`, `Bairro`, `Cep`, `Cidade` | string | endereço de domicílio |

Calculadas (somente leitura, **não** vão ao JSON): `StatusTexto`, `Iniciais`, `NascimentoTexto` (data + idade), `*Texto` (RgTexto, TelefonePrincipalTexto, … "Não informado" quando vazio). Métodos: `Clonar()` (MemberwiseClone), `ToString()` → `Nome`.

### 4.2 `Imovel` (`Models/Imovel.cs`)
Enums: `TipoImovel { Casa, Apartamento, Terreno, Outros }`, `FinalidadeImovel { Venda, Aluguel }`, `StatusImovel { Disponível, Reservado, Alugado, Vendido }`.

| Grupo | Propriedades |
|---|---|
| Identificação | `Id`, `Codigo` (`SM001`…), `Titulo` |
| Proprietário | `ProprietarioId` (FK → Cliente), `ProprietarioNome` (cópia exibida, atualizada ao renomear o cliente) |
| Endereço | `Endereco`, `Numero`, `Bairro`, `Cidade`, `Estado`, `Cep` |
| Negócio | `Tipo`, `Finalidade`, `Status` (padrão Disponível), `Valor` (decimal), `PercentualEmpresa` (comissão na venda / taxa no aluguel; padrão 6% e 10%) |
| Cômodos | `Quartos`, `Suites`, `Banheiros`, `Cozinhas`, `Salas`, `Vagas` |
| Texto/docs | `Descricao`, `MatriculaEdp` (energia), `MatriculaSabesp` (água), `Matricula` (cartório), `Iptu` (inscrição municipal), `Cib` (Cadastro Imobiliário Brasileiro — "CPF do imóvel", opcional e único) |
| Controle | `Ativo` (bool, padrão true) |
| Fotos | `List<string> Fotos` (apenas o nome do arquivo; a 1ª é a capa) |

Calculadas: `EnderecoCompleto`, `FotoCapa`, `TemCib`, `CibTexto`, `QuartosTexto`, `SuitesTexto`, `BanheirosTexto`, `VagasTexto`, `DescricaoTexto`, `MatriculaTexto`, `IptuTexto`, `MatriculaEdpTexto`, `MatriculaSabespTexto`, `ProprietarioTexto`, `CidadeEstadoTexto`, `LocalizacaoCurta`, `GanhoEmpresa`, `PercentualEmpresaTexto`, `GanhoEmpresaTexto`, `RotuloPercentual`. `ToString()` → `Titulo`; `Clonar()`.

### 4.3 `Contrato` (`Models/Contrato.cs`)
Enums: `TipoContrato { Locacao, Venda }`, `StatusContrato { Ativo, Encerrado, Cancelado }`.

| Grupo | Propriedades |
|---|---|
| Identificação | `Id`, `Codigo` (`CTR001`…) |
| Relacionamentos (por ID) | `ImovelId`, `LocatarioId` (Cliente Inquilino/Comprador), `LocadorId` (Cliente Proprietário — deve ser o dono do imóvel) |
| Cópias p/ exibição | `ImovelTitulo`, `LocatarioNome`, `LocadorNome` (atualizadas ao renomear cliente/imóvel) |
| Dados | `Tipo`, `Status`, `ValorMensal` (na Venda guarda o **valor total** da venda), `PrazoMeses` (36), `DiaVencimento` (20), `QuantidadeParcelas` (só Venda; 1 = à vista) |
| Datas | `DataInicio`, `DataFim` (= início + prazo), `DataAssinatura` |
| Cláusulas | `IndiceReajuste` (só Locação: IGP-M/FGV, IPCA, INPC), `PercentualMultaAtraso` (10), `MesesIsencaoMultaRescisao` (12) |
| Utilidades | `UC_Energia`, `UC_Agua`, `Observacoes` |
| Controle | `Ativo` |

Calculadas: `TipoTexto`, `PeriodoTexto`, `ValorTexto` (R$ + "/mês" para locação), `RotuloCondicaoTexto`/`CondicaoTexto` (parcelas na venda, índice na locação), `ObservacoesTexto`, `UcEnergiaTexto`, `UcAguaTexto`, `ImovelCibTexto` (CIB lido do imóvel). `Clonar()`.

### 4.4 Financeiro (`Pagamento`, `MovimentacaoFinanceira`, `ContratoFinanceiro`)
- **`Pagamento`** (gravado em `pagamentos.json`): `Id`, `ContratoId`, `Competencia` (chave da parcela — 1º dia do mês na locação; início + N meses na venda), `ValorRecebido`, `DataPagamento`, `PercentualEmpresa` (guardado no registro para o histórico não mudar), `Observacao`. Registrar o pagamento já significa que o valor foi recebido na conta.
- **`MovimentacaoFinanceira`** (não gravada): uma linha da tabela — pagamento registrado (`Pago`) ou parcela pendente. Calculadas: `Valor`, `ValorEmpresa`, `ValorRepasse`, `NaConta` (= pago), `Atrasado`, textos e visibilidade dos botões.
- **`ContratoFinanceiro`** + **`ParcelaContrato`** (não gravados): resumo de um contrato ativo com todas as parcelas do cronograma — `TotalPago`, `Falta`, `Atrasado`, `ParcelasPagas/Pendentes/Atrasadas`, `Progresso`, `GanhoEmpresa`, `RepasseProprietario`, `Proxima` (próximo vencimento), `StatusTexto` ("Em dia", "N atrasadas", "Quitado"), textos de regra de vencimento, reajuste e multa.

### 4.5 `Usuario` (`Models/Usuario.cs`)
`Id`, `NomeUsuario`, `Senha` (**hash** `sha256$…`), `NomeCompleto`, `Email`, `Telefone`, `Cargo`, `FotoCaminho?`, `PerguntaSecreta`, `RespostaSecreta` (**hash**).

### Relacionamentos entre entidades (por ID, sem banco de dados)
```
Cliente (Proprietário) 1 ──< Imovel            via Imovel.ProprietarioId
Imovel                 1 ──< Contrato          via Contrato.ImovelId   (no máximo 1 contrato Ativo por imóvel)
Cliente (Proprietário) 1 ──< Contrato (locador)   via Contrato.LocadorId  (= dono do imóvel)
Cliente (Inquilino/Comprador) 1 ──< Contrato (locatário) via Contrato.LocatarioId
Contrato               1 ──< Pagamento         via Pagamento.ContratoId
```
A integridade é garantida pelas telas/services: não é possível excluir cliente ou imóvel ligado a contrato; excluir um contrato remove os pagamentos dele; renomear cliente/imóvel atualiza as cópias de nome nos contratos e imóveis.

---

## 5. Services

Todos os services de dados seguem o mesmo padrão: **singleton** (`Instancia`), lista privada `List<T>`, `_proximoId`, `Arquivo` (nome do JSON), construtor que carrega do JSON ou gera exemplos, `Salvar()`.

| Service | Arquivo JSON | Métodos públicos | Particularidades |
|---|---|---|---|
| `ImovelService` | `imoveis.json` | `ObterAtivos()`, `ObterPorId`, `ObterPorCib`, `NormalizarCib` (static), `Adicionar`, `Atualizar`, `AtualizarNomeProprietario`, `Excluir(id)`, `Salvar()` | `Codigo = "SM" + Id:000`; `Excluir` remove da lista; aplica percentual padrão (6% venda / 10% aluguel) em imóveis antigos sem percentual |
| `ClienteService` (em `Models/Cliente.cs`) | `clientes.json` | `ObterTodos()`, `Adicionar`, `Atualizar`, `Excluir(id)`, `Salvar()` | 6 clientes de exemplo; ao carregar, move fotos antigas soltas em `Fotos/` para `Fotos/clientes/` |
| `ContratoService` | `contratos.json` | `ObterAtivos()`, `Adicionar`, `Atualizar`, `Excluir(id)`, `ClienteTemContrato`, `ImovelTemContrato`, `ContratoAtivoDoImovel`, `AtualizarNomeCliente`, `AtualizarTituloImovel`, `Salvar()` | `Codigo = "CTR" + Id:000`; **sincroniza status do imóvel** (ver §7); excluir remove os pagamentos do contrato |
| `FinanceiroService` | `pagamentos.json` | `Cronograma(contrato)` (static), `ObterMovimentacoes()`, `ParcelasPendentes(id)`, `ContratosParaPagamento()`, `ObterContratosFinanceiro()`, `ObterContratoFinanceiro(id)`, `ReceitaDoMes(data)`, `TotalRecebido(id)`, `Registrar`, `Desfazer`, `QuantidadeDoContrato`, `RemoverDoContrato`, `PercentualDoImovel`, `Salvar()` | o **cronograma** é a fonte única das parcelas (ver §7) |
| `UsuarioService` | `usuarios.json` | `ObterPorNomeUsuario`, `Autenticar`, `ValidarResposta`, `RedefinirSenha`, `ConferirSenha` (static), `Proteger`/`ProtegerResposta` (static), `Sair`, `Salvar()`, propriedade `UsuarioLogado` | `admin` criado se não houver usuários; senhas e respostas em hash SHA-256 com sal; converte texto puro de arquivos antigos ao carregar |
| `Persistencia` (static) | — | `Carregar<T>`, `Salvar<T>`, `CaminhoArquivo`, `CaminhoDaFoto`, `PastaJson`, `PastaComparacao`, `PastaFotos`, `PastaFotosClientes`, `SalvarNoPrincipal` | ver §6 |
| `PdfContratoService` (static) | — | `GerarECompletar(contrato)` (SaveFileDialog + Dialogo com "Abrir PDF"/"Abrir pasta"), `GerarArquivo(contrato, caminho)` | MigraDoc, A4, Calibri 10, cabeçalho com `logo.png` (base64), rodapé "Página X de Y", bloco de assinaturas |
| `ModeloContratoLocacao` (static) | — | `Montar(contrato, locador, locatario, imovel)` → `List<BlocoContrato>`, `Titulo`, `LinhaData`, `Dinheiro` | minuta "CONTRATO DE LOCAÇÃO RESIDENCIAL SEM GARANTIA"; `record BlocoContrato(TipoBloco, string)`; `enum TipoBloco { Clausula, Item, SubItem }` |
| `ModeloContratoVenda` (static) | — | `Montar(contrato, vendedor, comprador, imovel)`, `Titulo` | "INSTRUMENTO PARTICULAR DE COMPROMISSO DE COMPRA E VENDA"; cláusula 3.2 com a forma de pagamento (à vista ou parcelas) |
| `Extenso` (static, em `ModeloContratoLocacao.cs`) | — | `Inteiro`, `Reais`, `Numero`, `Percentual` | números por extenso até centenas de bilhões |
| `DadosImobiliaria` (static, em `ModeloContratoLocacao.cs`) | — | constantes | Nome, telefone, e-mail, endereço, CNPJ, representante; CRECI/RG/CPF vazios saem como `__________` no PDF |

---

## 6. Persistência (`Persistencia.cs`)

- **Raiz**: sobe até 8 níveis a partir de `AppContext.BaseDirectory` procurando `SaciGestao.csproj`; se achar, `Json/` e `Fotos/` ficam ao lado do código-fonte; senão usa a pasta do executável.
- **Entrada e saída**: ao iniciar, lê `Json/*.json`. Ao fechar pelo **"Sair"** (ou X), grava em `Json/Comparacao/` — a pasta `Json/` fica intacta para comparar antes × depois. O botão **"Salvar"** do menu liga `SalvarNoPrincipal` e grava direto em `Json/`.
- **Opções JSON**: `WriteIndented`, `UnsafeRelaxedJsonEscaping` (acentos preservados), `JsonStringEnumConverter` (enums como texto, ex.: `"Disponível"`, `"Locacao"`), e um modificador que **remove propriedades sem setter** (as `*Texto` calculadas). Campos desconhecidos de arquivos antigos (ex.: `CaiuNaConta`) são ignorados.
- **`Carregar<T>`**: devolve `null` se o arquivo não existe, está vazio ou é inválido (aí o service gera os exemplos). Um arquivo com lista vazia `[]` é válido (tudo foi apagado). Se inválido, copia para `<arquivo>.corrompido`.
- **`Salvar<T>`**: grava em `<arquivo>.tmp` e faz `File.Move(overwrite)` (gravação atômica); se falhar, registra o erro em `%TEMP%\SaciGestao_erro.log` sem travar o fechamento.
- **Fotos**: o JSON guarda só o nome (`<guid>.jpg`, `clientes/<arquivo>`, `usuarios/<arquivo>`); `CaminhoDaFoto(nome)` resolve para `Fotos/<nome>`. Ao adicionar, o arquivo é **copiado** para a pasta com nome `Guid.NewGuid().ToString("N") + extensão`.
- **Senhas**: `usuarios.json` guarda `Senha` e `RespostaSecreta` no formato `sha256$<sal>$<hash>`. Arquivos antigos em texto puro são convertidos ao carregar (as senhas continuam as mesmas).

### Esquema dos JSONs
- `clientes.json` / `imoveis.json` / `contratos.json` / `pagamentos.json` / `usuarios.json`: array de objetos com as propriedades graváveis dos models (§4). Enums como string; datas ISO (`2026-08-10T00:00:00`).

---

## 7. Regras de negócio

1. **Status do imóvel × contrato** (`ContratoService`):
   - Contrato `Ativo`: imóvel vira `Alugado` (Locação) ou `Vendido` (Venda).
   - Contrato `Encerrado`/`Cancelado`, excluído ou trocado de imóvel: o imóvel volta a `Disponível` (se nenhum outro contrato ativo o usar).
   - Um imóvel só pode ter **um contrato ativo** por vez, e o status de um imóvel com contrato ativo não pode ser trocado à mão.
2. **Ao selecionar um imóvel no contrato** (`ContratosViewModel.AoSelecionarImovel`): preenche título, tipo do contrato (imóvel de venda → Venda, de aluguel → Locação), valor (se vazio ou ainda o sugerido pelo imóvel anterior), `UC_Energia ← MatriculaEdp`, `UC_Agua ← MatriculaSabesp` e o locador (dono do imóvel).
3. **Listas de seleção no contrato**: imóveis `Disponível` ou `Reservado` (+ o atual); locatários = clientes não inativos do tipo Inquilino/Comprador; locadores = clientes não inativos do tipo Proprietário (sempre incluindo o atualmente selecionado).
4. **Data fim** do contrato = `DataInicio + PrazoMeses` ao salvar.
5. **Cronograma de parcelas** (`FinanceiroService.Cronograma`) — fonte única usada pela tabela, "A receber", contratos ativos, pop-up, "Registrar pagamento", Dashboard e PDF:
   - Locação: uma parcela por mês durante `PrazoMeses`, valor = `ValorMensal`.
   - Venda: `ValorMensal` (total) dividido em `QuantidadeParcelas` mensais; a última parcela ajusta os centavos.
   - Nos dois tipos a parcela vence no `DiaVencimento` (dia 31 vira o último dia do mês); a 1ª nunca vence antes do início.
   - Tabela e "A receber": parcelas não pagas que vencem até o fim do mês atual, sem limite para trás. Parcelas futuras ficam no pop-up do contrato.
   - "Registrar pagamento" aceita qualquer parcela ainda não paga (inclusive adiantar a próxima).
   - A parte da empresa vem do `PercentualEmpresa` do imóvel; o restante é o repasse ao proprietário.
6. **Validações**:
   - Imóvel: título, endereço, bairro e proprietário obrigatórios; valor > 0 (formato pt-BR, ex.: `450.000,00`); percentual entre 0 e 100; cômodos inteiros ≥ 0; CIB opcional, 4 a 20 caracteres (letras, números, `. - /`) e único.
   - Cliente: todos os campos obrigatórios exceto telefone secundário e foto; CPF com 11 dígitos, dígitos verificadores válidos (CPF novo ou alterado) e sem repetir; RG 9 dígitos; telefone com DDD; CEP 8 dígitos; e-mail válido; nascimento não pode ser no futuro.
   - Contrato: imóvel, locatário e locador obrigatórios; locador = dono do imóvel; valor > 0; dia de vencimento 1–31; prazo ≥ 1 mês; índice (Locação) ou parcelas ≥ 1 (Venda); com pagamentos registrados, tipo, início e parcelas não podem mudar.
   - Pagamento: valor > 0 e no máximo o dobro da parcela; data não pode ser futura; confirmação obrigatória.
   - Senha: nova senha ≥ 3 caracteres e confirmação igual. Perfil: e-mail e telefone opcionais, mas válidos se preenchidos.
7. **Exclusões**: cliente ligado a contrato ou dono de imóvel e imóvel ligado a contrato não podem ser excluídos; toda exclusão pede confirmação (`Dialogo`).
8. **Dashboard** (cálculos): receita do mês = aluguéis + parcelas de venda dos contratos ativos que vencem no mês (mesmo valor da tela de Contratos); ocupação = alugados / imóveis de aluguel; carteira = soma dos imóveis de venda Disponível/Reservado; vendas fechadas = contratos de venda não cancelados (com o valor já recebido); contratos a vencer = locações ativas com término em até 120 dias (renovação urgente ≤ 60 dias); avisos (renovações, reservas, imóveis sem foto, sem proprietário, sem CIB).

---

## 8. ViewModels (MVVM)

Base comum: `ViewModelBase` (`INotifyPropertyChanged`, `OnPropertyChanged`, `SetProperty<T>`) e `RelayCommand(Action<object?>, Func<object?,bool>?)` (usa `CommandManager.RequerySuggested`).

Padrão de telas CRUD (Imóveis, Clientes, Contratos): três "estados" de tela controlados por booleanos — `MostrandoLista` / `MostrandoFormulario` / `MostrandoDetalhes` — e campos `*Form` para o formulário, `ErroForm` para mensagens, `TituloFormulario`, `EditandoExistente`, `_idEmEdicao` (0 = novo). Comandos comuns: `NovoCommand`, `EditarCommand`, `VisualizarCommand`, `ExcluirCommand`, `SalvarFormCommand`, `CancelarFormCommand`, `FecharDetalhesCommand`.

| ViewModel | Principais membros |
|---|---|
| `LoginViewModel` | Propriedades: `NomeUsuarioLogin`, `SenhaLogin`, `ErroLogin`, `MostrandoRecuperar`, `RecNomeUsuario`, `PerguntaRevelada`, `PerguntaTexto`, `RecResposta`, `RecNovaSenha`, `RecConfirmarSenha`, `ErroRecuperar`, `SucessoRecuperar`. Comandos: `EntrarCommand`, `AbrirRecuperarCommand`, `VoltarLoginCommand`, `ContinuarRecuperarCommand`, `RedefinirSenhaCommand`. Evento: `LoginRealizado(Usuario)`. |
| `DashboardViewModel` | Somente leitura, calculado no construtor: `Saudacao`, `DataHoje`, `ReceitaMensal/Detalhe`, `OcupacaoPercentual/Detalhe`, `ValorEmCarteira/Detalhe`, `VendasFechadas/Detalhe`, `ContratosAVencer` (`VencimentoResumo`), `Funil` (`BarraResumo`), `ImoveisPorStatus` (`FatiaRosca` — arcos da rosca), `TotalGrafico`, `Avisos` (`AvisoResumo`), `ImoveisParaOferecer`. Método estático `TextoReceitaDoMes`. |
| `ImoveisViewModel` | Coleções `Imoveis`, `Bairros`, listas de filtro (`Tipos`, `Finalidades`, `Statuses`, `Faixas` → `FaixaPreco{Texto,Min,Max}`, só para venda), filtros (`Busca` — inclui CIB, `Tipo`, `Finalidade`, `Status`, `Bairro`, `Faixa`), campos `*Form` (Titulo, Endereco, Numero, Bairro, Cidade, Estado, Cep, Valor, Percentual, Quartos, Suites, Banheiros, Cozinhas, Salas, Vagas, Descricao, MatriculaEdp, MatriculaSabesp, Matricula, Iptu, Cib, ProprietarioId, Tipo/Finalidade/Status), `ListaProprietarios`, `FotosForm`, `ImovelDetalhe`, galeria (`FotoAtual`, `TemFotoAtual`, `VariasFotos`, `ContadorFotos`). Comandos extras: `AdicionarFotosCommand`, `RemoverFotoCommand`, `DefinirCapaCommand`, `AmpliarFotoCommand`, `FotoAnteriorCommand`, `ProximaFotoCommand`, `LimparFiltrosCommand`, `ExcluirDoFormCommand`, `EditarDosDetalhesCommand`. |
| `ClientesViewModel` | Coleção de clientes filtrada; filtros `Busca` (nome ou CPF, com ou sem pontuação) e `FiltroTipo`; totais `TotalProprietarios/Compradores/Inquilinos` (excluem inativos, atualizam a cada mudança); listas `Nacionalidades`, `EstadosCivis`, `TiposEnum`, `StatusesForm`; campos `*Form` (Nome, Cpf, Rg, OrgaoEmissorRg, Nacionalidade, EstadoCivil, DataNascimento, TelefonePrincipal/Secundario, Email, Rua, Numero, Bairro, Cep, Cidade, Profissao, Empresa, Foto, Tipo, Status); `ClienteDetalhe`; comandos `EscolherFotoCommand`, `RemoverFotoCommand`, `EditarDoDetalheCommand`, `ExcluirDoDetalheCommand`, `ExcluirDoFormCommand`. |
| `ContratosViewModel` | `Contratos`, `ContratoForm` (objeto `Contrato` editado direto), `ContratoDetalhe`, `ImovelIdSelecionado`, `TipoSelecionadoForm`, `MostrarIndiceReajuste`/`MostrarParcelas`, `ListaImoveis`, `ListaLocatarios`, `ListaLocadores`, `IndicesReajuste`, `TiposContratoEnum`, `StatusesContratoEnum`, `Busca`, `FiltroTipo`, totais (`TotalAtivos`, `ReceitaMensal`/`ReceitaDetalhe` — receita do mês, `TotalTodos`, `TotalLocacao`, `TotalVenda`), `ErroForm`. Comando extra: `GerarPdfCommand` → `PdfContratoService.GerarECompletar`. |
| `FinanceiroViewModel` | Tabela `Movimentacoes`, gráfico `Meses` (`BarraMes`), indicadores (`ReceitaNaConta`, `AReceber`, `GanhoEmpresa`, `Repasse`, `TotalLocacao`, `TotalVenda`, `QuantidadePendentes`, `QuantidadeAtrasados`), lista `ContratosAtivos` (`ContratoFinanceiro`) e pop-up `DetalheContrato`, filtros (`DataInicial`, `DataFinal`, `TipoSelecionado`, `SituacaoSelecionada`, `Busca`, `PeriodoAtivo`), diálogo "Registrar pagamento" (`ContratosDisponiveis`, `ParcelasDisponiveis`, `ContratoSelecionado`, `ParcelaSelecionada`, `ValorForm`, `DataPagamentoForm`, `ConfirmadoForm`, `ObservacaoForm`, `ResumoEmpresa`). Comandos: `AbrirRegistrarCommand`, `ConfirmarRegistroCommand`, `FecharDialogoCommand`, `DesfazerCommand`, `LimparFiltrosCommand`, `PeriodoCommand`, `AbrirDetalheCommand`, `FecharDetalheCommand`, `RegistrarDoDetalheCommand`. |

`ConfiguracoesView` não tem ViewModel: o code-behind edita o usuário logado (perfil, senha, recuperação e sessão).

---

## 9. Views, recursos e comunicação entre camadas

```
View (XAML)  ──Binding / Command──►  ViewModel  ──chama──►  Service (singleton)  ──►  Persistencia ──► Json/ e Fotos/
     ▲                                   │                        │
     └──── PropertyChanged ──────────────┘                        └─ Services chamam outros Services
                                                                      (ContratoService → ImovelService/FinanceiroService;
                                                                       ViewModels/PDF → Cliente/Imovel/Usuario/FinanceiroService)
```

- **Criação do DataContext**: no code-behind de cada View (`DataContext = new XViewModel();`).
- **Troca de tela**: `MainWindow.Menu_Checked` instancia a View escolhida em `AreaConteudo` (cada troca recria View + ViewModel; o estado fica nos Services). A foto do usuário na barra superior abre a `ConfiguracoesView`.
- **Atalhos do Dashboard**: `DashboardView.Atalho_Click` lê `Button.Tag` (nome do RadioButton), chama `MainWindow.Navegar(tag, abrirNovo: true)`, que via **reflexão** executa o `NovoCommand` do ViewModel da tela aberta.
- **Login → sistema**: evento `LoginViewModel.LoginRealizado` → `LoginView.LoginRealizado` → `MainWindow.Entrar`. "Sair da conta" (Configurações) volta ao login sem fechar a janela e limpa o usuário logado.
- **PasswordBox**: sem binding; `LoginView.xaml.cs` repassa `Password` ao ViewModel nos eventos `PasswordChanged` (`txtSenhaLogin`, `txtNovaSenha`, `txtConfirmarSenha`).
- **Avisos e confirmações**: `Views/Dialogo` substitui a caixa padrão do Windows — `Dialogo.Avisar(...)`, `Dialogo.Confirmar(...)` e `Dialogo.Mostrar(...)`, com tipos `Informacao`, `Sucesso`, `Atencao`, `Perigo` (botão vermelho) e `Erro`.
- **FotoZoomWindow**: aberta por `ImoveisViewModel.AmpliarFoto`; recebe `(IList<string> fotos, int indiceInicial)`; expõe `IndiceAtual`; zoom pela roda do mouse, botões +/−/ajustar, arrastar, setas/teclado.
- **Converters** (declarados como recursos nas Views): `CaminhoParaImagemConverter` (`CaminhoParaImagem`, parâmetro = largura de decodificação, padrão 900), `TextoAmigavelConverter`, `FiltroValorParaBoolConverter` (chips de filtro), `BooleanToVisibilityConverter`, `IguaisConverter`, `TextoParaVisibilidadeConverter`.
- **Helpers**: `Mascara.Tipo` (propriedade anexada nas TextBox: `Cpf`, `Rg`, `Telefone`, `Cep`, `Inteiro`, `Decimal`); `PainelAdaptavel` e `Adaptavel.LarguraMinima` (grades que se reorganizam pela largura); `Adaptavel.Tabela`/`OcultarAbaixo` (colunas de tabela que somem em telas estreitas); `Responsivo.Compacto` (menu só com ícones).
- **Tema** (`Resources/Tema.xaml`, mesclado em `App.xaml`):
  - Cores: `Primaria #920505`, `PrimariaEscura #780404`, `PrimariaMaisEscura`, `PrimariaClara #FBEDEC`, `PrimariaMedia`, `Sucesso #1E7B3A`, `SucessoClaro`, `Atencao #8A5300`, `AtencaoClara`, `Perigo #C62828`, `PerigoClaro`, `Texto #2B2B2B`, `TextoSuave #4B5563`, `Borda #8591A1`, `BordaSuave`, `FundoSuave`, `FundoApp`, `CorMenu #232323`; sombras `SombraCartao` e `SombraFlutuante`.
  - Estilos compartilhados (usados por todas as telas): `Cartao`, `Rotulo`, `RotuloForm`, `TextoCelula`, `CampoForm`, `CampoSenha`, `CampoFormMultilinha`, `ComboArredondado`, `BotaoPrimario`, `BotaoContorno`, `BotaoPerigo`, `BotaoTextoPerigo`, `BotaoAcao`, `BotaoAcaoPerigo`, `CabecalhoColuna`, `CelulaGrid`, `LinhaGrid`.
  - Estilos implícitos (valem para qualquer controle sem estilo próprio): `ComboBox`, `ComboBoxItem`, `DatePicker`, `CheckBox`, `ScrollBar` (barra fina) e `ToolTip`.
  - `MainWindow` define ainda `CorMenuHover`, `CorMenuAtivo`, `CorTextoMenu`, `CorDestaque`, `CorFundo`, `CorTexto` e os estilos `MenuItemStyle`, `MenuAcaoStyle`, `BotaoIconeStyle`.
- **Estilos locais**: cada View só define os estilos exclusivos dela (ex.: `CartaoImovel`, `ChipImovel`, `CaixaDetalhe`, `TituloSecaoDetalhe`, `EtiquetaStatus`, `BotaoSetaGaleria`, `MiniaturaGaleria`, `CartaoIndicador`). Ícones via fonte Segoe MDL2 Assets (códigos como `&#xE80F;`).
- **Acessibilidade**: contraste WCAG 2.1 (≥4,5:1 texto, ≥3:1 bordas), status sempre com texto, idioma `pt-BR`, operação por teclado.
- **Itens do menu** (`MainWindow.xaml`): `rbDashboard` (Dashboard), `rbImoveis` (Imóveis), `rbClientes` (Clientes), `rbContratos` (Contratos), `rbFinanceiro` (Financeiro); rodapé com `Salvar` e `Sair`. Elementos nomeados: `AreaConteudo` (ContentControl), `runNomeUsuario`, `txtData`.

---

## 10. Observações, pendências e pontos de atenção

- **Código legado/não usado**: `Views/ImovelFormWindow` (formulário antigo, só tem 6 campos; o cadastro atual é inline em `ImoveisView`); pacote `CommunityToolkit.Mvvm`.
- **Fotos órfãs**: fotos de imóvel copiadas para `Fotos/` continuam na pasta se o cadastro for cancelado ou o imóvel excluído.
- **Segurança**: senhas com hash, mas sem perfis de acesso (todos os usuários têm as mesmas permissões).
- **Persistência**: gravação apenas no encerramento; encerramento abrupto perde as alterações da sessão.
- **CIB**: o formato oficial do código ainda não foi divulgado pela Receita Federal, por isso o campo é texto livre (sem máscara).
- **Contrato de venda**: `PrazoMeses` continua editável na venda e define a data de término; o cronograma de pagamento usa `QuantidadeParcelas`.
- **`.csproj`**: contém `Compile Remove="NovaPasta\**"` (pasta inexistente) e `Resource` para `Logo4.png` e `logo.png`.
- **Versionamento**: arquivos de `.vs/` aparecem como modificados no git apesar do `.gitignore` (foram commitados antes); considere `git rm -r --cached SaciGestao/.vs`.
