# SaciGestao — Documentação da Estrutura do Projeto

Sistema desktop de gestão imobiliária da **Sacilotti Imóveis & Negócios**. Cadastro de imóveis (com fotos), clientes e contratos (locação/venda), dashboard e exportação de contrato em PDF. Sem banco de dados: persistência em arquivos JSON.

---

## 1. Visão geral técnica

| Item | Valor |
|------|-------|
| Linguagem | C# (`Nullable` e `ImplicitUsings` habilitados) |
| Framework | `net10.0-windows`, WPF (`OutputType = WinExe`) |
| Arquitetura | MVVM + camada de Services (singletons em memória) |
| Persistência | `System.Text.Json` em `SaciGestao/Json/*.json` |
| Pacotes NuGet | `CommunityToolkit.Mvvm 8.4.2` (referenciado, **não usado** no código — ViewModels usam `ViewModelBase`/`RelayCommand` próprios), `PDFsharp-MigraDoc-WPF 6.2.4` (geração de PDF) |
| Solução | `SaciGestao/SaciGestao.slnx` |
| Login padrão | `admin` / `123` (pergunta secreta: "Qual o nome do seu primeiro animal de estimação?" → `rex`) |
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
    ├── App.xaml / App.xaml.cs             Ponto de entrada, carga e salvamento global
    ├── AssemblyInfo.cs                    Tema WPF (ThemeInfo)
    ├── MainWindow.xaml / .cs              Shell: menu lateral + área de conteúdo
    ├── Models/                            Entidades de dados
    │   ├── Cliente.cs    (também contém ClienteService e FiltroValorParaBoolConverter)
    │   ├── Imovel.cs
    │   ├── Contrato.cs
    │   └── Usuario.cs
    ├── Service/                           Regras de negócio e dados em memória
    │   ├── Persistencia.cs                Leitura/gravação JSON, pastas Json e Fotos
    │   ├── ImovelService.cs
    │   ├── ContratoService.cs
    │   ├── UsuarioService.cs
    │   ├── PdfContratoService.cs          Geração do PDF (MigraDoc)
    │   └── ModeloContratoLocacao.cs       Texto/cláusulas do contrato + DadosImobiliaria
    ├── ViewModels/                        Lógica de tela (MVVM)
    │   ├── ViewModelBase.cs               INotifyPropertyChanged + SetProperty
    │   ├── RelayCommand.cs                ICommand genérico
    │   ├── LoginViewModel.cs
    │   ├── DashboardViewModel.cs
    │   ├── ImoveisViewModel.cs
    │   ├── ClientesViewModel.cs
    │   └── ContratosViewModel.cs
    ├── Views/                             Telas XAML (+ code-behind mínimo)
    │   ├── LoginWindow.xaml(.cs)
    │   ├── DashboardView.xaml(.cs)        UserControl
    │   ├── ImoveisView.xaml(.cs)          UserControl (lista/form/detalhes/galeria)
    │   ├── ClientesView.xaml(.cs)         UserControl
    │   ├── ContratosView.xaml(.cs)        UserControl
    │   ├── FotoZoomWindow.xaml(.cs)       Janela de zoom de foto
    │   └── ImovelFormWindow.xaml(.cs)     Formulário antigo (NÃO usado mais — ver §10)
    ├── Converters/
    │   ├── CaminhoParaImagemConverter.cs  nome/caminho de foto → BitmapImage
    │   └── TextoAmigavelConverter.cs      "Locacao"→"Locação", "EmAtendimento"→"Em Atendimento"
    ├── Resources/
    │   ├── Tema.xaml                      Paleta de cores (ResourceDictionary global)
    │   └── logo.png, logo1.png, logo2.png, logo3.png
    ├── Json/                              Dados persistidos
    │   ├── clientes.json
    │   ├── imoveis.json
    │   ├── contratos.json
    │   └── usuarios.json
    └── Fotos/                             Fotos dos imóveis (nome = GUID sem hífens + extensão)
```

> Namespaces: `SaciGestao`, `SaciGestao.Models`, `SaciGestao.Service`, `SaciGestao.ViewModels`, `SaciGestao.Views`, `SaciGestao.Converters`. Observação: a pasta chama-se `Service` (singular) e o namespace também.

---

## 3. Fluxo de execução

```
App.OnStartup
  ├─ força a criação dos singletons: ImovelService, ClienteService, ContratoService, UsuarioService
  │    (cada um lê seu JSON; se ausente/corrompido → dados de exemplo)
  └─ new LoginWindow().Show()

LoginWindow  ──(LoginViewModel.LoginRealizado)──►  new MainWindow(usuario).Show(); Close()

MainWindow
  ├─ exibe NomeCompleto do usuário logado
  ├─ menu lateral (RadioButtons): rbDashboard, rbImoveis, rbClientes, rbContratos, rbFinanceiro
  │    Menu_Checked → AreaConteudo.Content = new <X>View()
  │    (rbFinanceiro → "Tela em construção")
  └─ Navegar(nomeMenu, abrirNovo) usado pelos atalhos do Dashboard

Encerramento (Sair / X da janela / desligar Windows)
  App.OnExit / OnSessionEnding → SalvarTudo()
     → Imovel/Cliente/Contrato/UsuarioService.Salvar() → Persistencia.Salvar(...)
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
| Proprietário | `ProprietarioId` (FK → Cliente), `ProprietarioNome` (cópia exibida) |
| Endereço | `Endereco`, `Numero`, `Bairro`, `Cidade`, `Estado`, `Cep` |
| Negócio | `Tipo`, `Finalidade`, `Status` (padrão Disponível), `Valor` (decimal) |
| Cômodos | `Quartos`, `Suites`, `Banheiros`, `Cozinhas`, `Salas`, `Vagas` |
| Texto/docs | `Descricao`, `MatriculaEdp` (energia), `MatriculaSabesp` (água), `Matricula`, `Iptu` |
| Controle | `Ativo` (bool, padrão true) |
| Fotos | `List<string> Fotos` (apenas o nome do arquivo; a 1ª é a capa) |

Calculadas: `EnderecoCompleto`, `FotoCapa`, `QuartosTexto`, `SuitesTexto`, `BanheirosTexto`, `VagasTexto`, `DescricaoTexto`, `MatriculaTexto`, `IptuTexto`, `MatriculaEdpTexto`, `MatriculaSabespTexto`, `ProprietarioTexto`, `CidadeEstadoTexto`, `LocalizacaoCurta`. `ToString()` → `Titulo`; `Clonar()`.

### 4.3 `Contrato` (`Models/Contrato.cs`)
Enums: `TipoContrato { Locacao, Venda }`, `StatusContrato { Ativo, Encerrado, Cancelado }`.

| Grupo | Propriedades |
|---|---|
| Identificação | `Id`, `Codigo` (`CTR001`…) |
| Relacionamentos (por ID) | `ImovelId`, `LocatarioId` (Cliente Inquilino/Comprador), `LocadorId` (Cliente Proprietário) |
| Cópias p/ exibição | `ImovelTitulo`, `LocatarioNome`, `LocadorNome` |
| Dados | `Tipo`, `Status`, `ValorMensal` (para Venda guarda o valor da venda), `PrazoMeses` (36), `DiaVencimento` (20) |
| Datas | `DataInicio`, `DataFim` (= início + prazo), `DataAssinatura` |
| Cláusulas | `IndiceReajuste` ("IGP-M/FGV"; opções IGP-M/FGV, IPCA, INPC), `PercentualMultaAtraso` (10), `MesesIsencaoMultaRescisao` (12) |
| Utilidades | `UC_Energia`, `UC_Agua`, `Observacoes` |
| Controle | `Ativo` |

Calculadas: `TipoTexto`, `PeriodoTexto`, `ValorTexto` (R$ + "/mês" para locação), `ObservacoesTexto`, `UcEnergiaTexto`, `UcAguaTexto`. `Clonar()`.

### 4.4 `Usuario` (`Models/Usuario.cs`)
`Id`, `NomeUsuario`, `Senha` (**texto puro**), `NomeCompleto`, `FotoCaminho?`, `PerguntaSecreta`, `RespostaSecreta`.

### Relacionamentos entre entidades (por ID, sem integridade referencial)
```
Cliente (Proprietário) 1 ──< Imovel            via Imovel.ProprietarioId
Imovel                 1 ──< Contrato          via Contrato.ImovelId
Cliente (Proprietário) 1 ──< Contrato (locador)   via Contrato.LocadorId
Cliente (Inquilino/Comprador) 1 ──< Contrato (locatário) via Contrato.LocatarioId
```
Nomes (`ProprietarioNome`, `ImovelTitulo`, `LocatarioNome`, `LocadorNome`) são cópias gravadas no momento do salvamento e **não** são atualizadas se o cliente/imóvel for renomeado depois.

---

## 5. Services

Todos os services de dados seguem o mesmo padrão: **singleton** (`Instancia`), lista privada `List<T>`, `_proximoId`, `Arquivo` (nome do JSON), construtor que carrega do JSON ou gera exemplos, `Salvar()`.

| Service | Arquivo JSON | Métodos públicos | Particularidades |
|---|---|---|---|
| `ImovelService` | `imoveis.json` | `ObterAtivos()`, `Adicionar`, `Atualizar`, `Excluir(id)`, `Salvar()` | `Codigo = "SM" + Id:000`; `Excluir` remove da lista (hard delete); `Atualizar` copia campo a campo |
| `ClienteService` (em `Models/Cliente.cs`) | `clientes.json` | `ObterTodos()`, `Adicionar`, `Atualizar`, `Excluir(id)`, `Salvar()` | 6 clientes de exemplo; sem checagem de vínculos ao excluir |
| `ContratoService` | `contratos.json` | `ObterAtivos()`, `Adicionar`, `Atualizar`, `Excluir(id)`, `Salvar()` | `Codigo = "CTR" + Id:000`; **sincroniza status do imóvel** (ver §7) |
| `UsuarioService` | `usuarios.json` | `ObterPorNomeUsuario`, `Autenticar`, `ValidarResposta`, `RedefinirSenha`, `Sair`, `Salvar()`, propriedade `UsuarioLogado` | usuário `admin` criado se não houver arquivo; comparações de usuário/resposta sem diferenciar maiúsculas |
| `Persistencia` (static) | — | `Carregar<T>`, `Salvar<T>`, `CaminhoArquivo`, `CaminhoDaFoto`, `PastaJson`, `PastaFotos` | ver §6 |
| `PdfContratoService` (static) | — | `GerarECompletar(contrato)` (abre SaveFileDialog + mensagem), `GerarArquivo(contrato, caminho)` | MigraDoc, A4, fonte Calibri 10, cabeçalho/rodapé com logo (base64), bloco de assinaturas |
| `ModeloContratoLocacao` (static) | — | `Montar(contrato, locador, locatario, imovel)` → `List<BlocoContrato>`, `Titulo`, `LinhaData`, `Dinheiro`, `Inteiro`, `Reais`, `Numero`, `Percentual` | Texto da minuta "CTR RESIDENCIAL SEM GARANTIA"; converte números em extenso; `record BlocoContrato(TipoBloco, string)`; `enum TipoBloco { Clausula, Item, SubItem }` |
| `DadosImobiliaria` (static, em `ModeloContratoLocacao.cs`) | — | constantes | Nome, telefone, e-mail, endereço, CNPJ, representante; CRECI/RG/CPF vazios saem como `__________` no PDF |

---

## 6. Persistência (`Persistencia.cs`)

- **Raiz**: sobe até 8 níveis a partir de `AppContext.BaseDirectory` procurando `SaciGestao.csproj`; se achar, `Json/` e `Fotos/` ficam ao lado do código-fonte; senão usa a pasta do executável.
- **Opções JSON**: `WriteIndented`, `UnsafeRelaxedJsonEscaping` (acentos preservados), `JsonStringEnumConverter` (enums como texto, ex.: `"Disponível"`, `"Locacao"`), e um modificador que **remove propriedades sem setter** (as `*Texto` calculadas).
- **`Carregar<T>`**: devolve `null` se arquivo não existe, vazio, lista vazia ou inválido. Se inválido, copia para `<arquivo>.corrompido`. `null` → service gera dados de exemplo.
- **`Salvar<T>`**: grava em `<arquivo>.tmp` e faz `File.Move(overwrite)` (gravação atômica); exceções são engolidas.
- **Fotos**: o JSON guarda só o nome (`<guid>.jpg`); `CaminhoDaFoto(nome)` resolve para `Fotos/<nome>` (caminhos absolutos antigos continuam funcionando). Ao adicionar, o arquivo é **copiado** para `Fotos/` com nome `Guid.NewGuid().ToString("N") + extensão`.

### Esquema dos JSONs
- `clientes.json` / `imoveis.json` / `contratos.json` / `usuarios.json`: array de objetos com as propriedades graváveis dos models (§4). Enums como string; datas ISO (`2026-08-10T00:00:00`).

---

## 7. Regras de negócio

1. **Status do imóvel × contrato** (`ContratoService`):
   - `Adicionar`/`Atualizar`: imóvel vira `Alugado` (Locação) ou `Vendido` (Venda).
   - Se o contrato troca de imóvel, o imóvel antigo volta a `Disponível` (se nenhum outro contrato ativo o usar).
   - `Excluir`: imóvel volta a `Disponível`, a menos que outro contrato `Ativo` o use.
2. **Ao selecionar um imóvel no contrato** (`ContratosViewModel.AoSelecionarImovel`): preenche título, valor (se ainda 0), `UC_Energia ← MatriculaEdp`, `UC_Agua ← MatriculaSabesp` e o locador (dono do imóvel, por `ProprietarioId` ou, em dados antigos, por nome).
3. **Listas de seleção no contrato**: imóveis `Disponível` (+ o atual); locatários = clientes não inativos do tipo Inquilino/Comprador; locadores = clientes não inativos do tipo Proprietário (sempre incluindo o atualmente selecionado).
4. **Data fim** do contrato = `DataInicio + PrazoMeses` ao salvar.
5. **Validações**:
   - Imóvel: título, endereço, bairro obrigatórios; valor > 0 (formato pt-BR, ex.: `450.000,00`); cômodos inteiros ≥ 0; proprietário obrigatório.
   - Cliente: todos os campos obrigatórios exceto telefone secundário.
   - Contrato: imóvel, locatário e locador obrigatórios; valor > 0.
   - Recuperação de senha: nova senha ≥ 3 caracteres e confirmação igual.
6. **Dashboard** (cálculos): receita mensal = soma de locações ativas; ocupação = alugados / imóveis de aluguel; carteira = soma dos imóveis de venda Disponível/Reservado; vendas fechadas = soma de contratos de venda não cancelados; contratos a vencer = locações ativas com término em até 120 dias (renovação urgente ≤ 60 dias); avisos (reservas, imóveis sem foto, imóveis sem proprietário).

---

## 8. ViewModels (MVVM)

Base comum: `ViewModelBase` (`INotifyPropertyChanged`, `OnPropertyChanged`, `SetProperty<T>`) e `RelayCommand(Action<object?>, Func<object?,bool>?)` (usa `CommandManager.RequerySuggested`).

Padrão de telas CRUD (Imóveis, Clientes, Contratos): três "estados" de tela controlados por booleanos — `MostrandoLista` / `MostrandoFormulario` / `MostrandoDetalhes` — e campos `*Form` para o formulário, `ErroForm` para mensagens, `TituloFormulario`, `EditandoExistente`, `_idEmEdicao` (0 = novo). Comandos comuns: `NovoCommand`, `EditarCommand`, `VisualizarCommand`, `ExcluirCommand`, `SalvarFormCommand`, `CancelarFormCommand`, `FecharDetalhesCommand`.

| ViewModel | Principais membros |
|---|---|
| `LoginViewModel` | Propriedades: `NomeUsuarioLogin`, `SenhaLogin`, `ErroLogin`, `MostrandoRecuperar`, `RecNomeUsuario`, `PerguntaRevelada`, `PerguntaTexto`, `RecResposta`, `RecNovaSenha`, `RecConfirmarSenha`, `ErroRecuperar`, `SucessoRecuperar`. Comandos: `EntrarCommand`, `AbrirRecuperarCommand`, `VoltarLoginCommand`, `ContinuarRecuperarCommand`, `RedefinirSenhaCommand`. Evento: `LoginRealizado(Usuario)`. |
| `DashboardViewModel` | Somente leitura, calculado no construtor: `Saudacao`, `DataHoje`, `ReceitaMensal/Detalhe`, `OcupacaoPercentual/Detalhe`, `ValorEmCarteira/Detalhe`, `VendasFechadas/Detalhe`, `ContratosAVencer` (`VencimentoResumo`), `Funil` (`BarraResumo`), `ImoveisPorStatus` (`FatiaRosca` — arcos SVG da rosca), `TotalGrafico`, `Avisos` (`AvisoResumo`), `ImoveisParaOferecer`. |
| `ImoveisViewModel` | Coleções `Imoveis`, `Bairros`, listas de filtro (`Tipos`, `Finalidades`, `Statuses`, `Faixas` → `FaixaPreco{Texto,Min,Max}`), filtros (`Busca`, `Tipo`, `Finalidade`, `Status`, `Bairro`, `Faixa`), campos `*Form` (Titulo, Endereco, Numero, Bairro, Cidade, Estado, Cep, Valor, Quartos, Suites, Banheiros, Cozinhas, Salas, Vagas, Descricao, MatriculaEdp, MatriculaSabesp, Matricula, Iptu, ProprietarioId, Tipo/Finalidade/Status), `ListaProprietarios`, `FotosForm`, `ImovelDetalhe`, galeria (`FotoAtual`, `TemFotoAtual`, `VariasFotos`, `ContadorFotos`). Comandos extras: `AdicionarFotosCommand`, `RemoverFotoCommand`, `DefinirCapaCommand`, `AmpliarFotoCommand`, `FotoAnteriorCommand`, `ProximaFotoCommand`, `LimparFiltrosCommand`, `ExcluirDoFormCommand`, `EditarDosDetalhesCommand`. |
| `ClientesViewModel` | Coleção de clientes filtrada; filtros `Busca` e `FiltroTipo` ("Todos"/tipo); totais estáticos `TotalProprietarios/Compradores/Inquilinos` (excluem inativos); listas estáticas `Nacionalidades`, `EstadosCivis`, `TiposEnum`, `StatusesForm`; campos `*Form` (Nome, Cpf, Rg, OrgaoEmissorRg, Nacionalidade, EstadoCivil, DataNascimento, TelefonePrincipal/Secundario, Email, Rua, Numero, Bairro, Cep, Cidade, Profissao, Empresa, Tipo, Status); `ClienteDetalhe`; comandos `EditarDoDetalheCommand`, `ExcluirDoDetalheCommand`, `ExcluirDoFormCommand`. |
| `ContratosViewModel` | `Contratos`, `ContratoForm` (objeto `Contrato` editado direto), `ContratoDetalhe`, `ImovelIdSelecionado`, `ListaImoveis`, `ListaLocatarios`, `ListaLocadores`, `IndicesReajuste`, `TiposContratoEnum`, `StatusesContratoEnum`, `Busca`, `FiltroTipo`, totais (`TotalAtivos`, `ReceitaMensal`, `TotalTodos`, `TotalLocacao`, `TotalVenda`), `ErroForm`. Comando extra: `GerarPdfCommand` → `PdfContratoService.GerarECompletar`. |

---

## 9. Views, recursos e comunicação entre camadas

```
View (XAML)  ──Binding / Command──►  ViewModel  ──chama──►  Service (singleton)  ──►  Persistencia ──► Json/ e Fotos/
     ▲                                   │                        │
     └──── PropertyChanged ──────────────┘                        └─ Services chamam outros Services
                                                                      (ContratoService → ImovelService;
                                                                       ViewModels/PDF → Cliente/Imovel/UsuarioService)
```

- **Criação do DataContext**: no code-behind de cada View (`DataContext = new XViewModel();`) — `LoginWindow`, `DashboardView`, `ClientesView`, etc.
- **Troca de tela**: `MainWindow.Menu_Checked` instancia a View escolhida em `AreaConteudo` (cada troca recria View + ViewModel; o estado fica nos Services).
- **Atalhos do Dashboard**: `DashboardView.Atalho_Click` lê `Button.Tag` (nome do RadioButton), chama `MainWindow.Navegar(tag, abrirNovo: true)`, que via **reflexão** executa o `NovoCommand` do ViewModel da tela aberta.
- **Login → Main**: evento `LoginViewModel.LoginRealizado` tratado em `LoginWindow.xaml.cs`.
- **PasswordBox**: sem binding; `LoginWindow.xaml.cs` repassa `Password` ao ViewModel nos eventos `PasswordChanged` (`txtSenhaLogin`, `txtNovaSenha`, `txtConfirmarSenha`).
- **FotoZoomWindow**: aberta por `ImoveisViewModel.AmpliarFoto`; recebe `(IList<string> fotos, int indiceInicial)`; expõe `IndiceAtual`; zoom pela roda do mouse, botões +/−/ajustar, arrastar, setas/teclado.
- **Converters** (declarados como recursos nas Views): `CaminhoParaImagemConverter` (`CaminhoParaImagem`, parâmetro = largura de decodificação, padrão 900), `TextoAmigavelConverter`, `FiltroValorParaBoolConverter` (chips de filtro), `BooleanToVisibilityConverter` (`BoolParaVisibilidade`).
- **Tema** (`Resources/Tema.xaml`, mesclado em `App.xaml`): `Primaria #1E5BB8`, `PrimariaEscura #174A96`, `PrimariaClara #E8F0FB`, `Sucesso #1E7B3A`, `SucessoClaro`, `Atencao #8A5300`, `AtencaoClara`, `Perigo #C62828` (só para perigo/erro), `PerigoClaro`, `Texto #1F2933`, `TextoSuave #4B5563`, `Borda #8591A1`, `CorMenu #222B33`. `MainWindow` define ainda `CorMenuHover`, `CorMenuAtivo`, `CorTextoMenu`, `CorDestaque`, `CorFundo`, `CorTexto` e estilos `MenuItemStyle`, `MenuSairStyle`, `BotaoIconeStyle`.
- **Estilos reutilizáveis nas Views** (definidos localmente em cada View, ex.: `ImoveisView`): `Cartao`, `Rotulo`, `RotuloForm`, `CampoForm`, `CampoFormMultilinha`, `ComboArredondado`, `BotaoPrimario`, `BotaoContorno`, `BotaoTextoPerigo`, `BotaoAcao`, `BotaoAcaoPerigo`, `CabecalhoColuna`, `CelulaGrid`, `LinhaGrid`, `TextoCelula`, `EtiquetaStatus`, `TextoStatus`, `RotuloDetalhe`, `ValorDetalhe`, `CartaoImovel`, `ChipImovel`, `TextoChipImovel`, `CaixaDetalhe`, `TituloSecaoDetalhe`, `IconeSecaoDetalhe`, `ChipDetalhe`, `BotaoSetaGaleria`, `BotaoMiniFoto`, `MiniaturaGaleria`. Ícones via fonte Segoe MDL2/Fluent (códigos como `&#xE80F;`).
- **Acessibilidade**: contraste WCAG 2.1 (≥4,5:1 texto, ≥3:1 bordas), status sempre com texto, idioma `pt-BR`, operação por teclado.
- **Itens do menu** (`MainWindow.xaml`): `rbDashboard` (Dashboard), `rbImoveis` (Imóveis), `rbClientes` (Clientes), `rbContratos` (Contratos), `rbFinanceiro` (Financeiro — não implementado). Elementos nomeados: `AreaConteudo` (ContentControl), `runNomeUsuario`.

---

## 10. Observações, pendências e pontos de atenção

- **Código legado/não usado**: `Views/ImovelFormWindow` (formulário antigo, só tem 6 campos; o cadastro atual é inline em `ImoveisView`); pacote `CommunityToolkit.Mvvm`; `Usuario.FotoCaminho` (sem uso); `Resources/logo1.png` e `Fotos` órfãs não referenciadas no `.csproj`.
- **Segurança**: senhas e respostas secretas em texto puro em `usuarios.json`. Sem hash e sem perfis de acesso.
- **Integridade**: sem chaves estrangeiras; excluir cliente/imóvel não verifica contratos vinculados. Nomes denormalizados (`ProprietarioNome`, `LocadorNome`, …) podem ficar desatualizados.
- **Persistência**: gravação apenas no encerramento; falhas de escrita são silenciosas.
- **Contrato de exemplo** (`contratos.json`) tem `LocatarioId = 0` e `LocadorId = 0` (só os nomes preenchidos) — o PDF cai no fallback usando apenas o nome.
- **`ImovelService.Atualizar`** repete algumas atribuições (`Suites`, `Banheiros`, `Cozinhas`, `Salas`) — inofensivo, mas redundante. `ImovelService.ObterAtivos` filtra por `Ativo`, porém não existe exclusão lógica (a exclusão remove o registro).
- **Comentários desatualizados**: em `ClienteService`/`ContratoService` aparecem trechos como "Jsonientes.json"/"Jsonntratos.json" (erro de digitação em comentário, sem efeito no código).
- **`.csproj`**: contém `Compile Remove="NovaPasta\**"` (pasta inexistente) e `Resource` apenas para `logo.png`, `logo2.png`, `logo3.png`.
- **Financeiro**: menu existe, tela ainda não implementada.
- **Versionamento**: arquivos de `.vs/` aparecem como modificados no git apesar do `.gitignore` (foram commitados antes); considere `git rm -r --cached SaciGestao/.vs`.
