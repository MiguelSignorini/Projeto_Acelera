# SaciGestao

Sistema desktop de gestão imobiliária da **Sacilotti Empreendimentos Imobiliários**, desenvolvido como projeto acadêmico.
Cadastro de imóveis (com fotos e CIB), clientes e contratos de locação e venda, módulo financeiro de pagamentos e um dashboard de acompanhamento.

> Projeto feito **somente em C# / XAML**, sem banco de dados: os dados são guardados em arquivos **JSON**.

---

## Tecnologias

| Item | Detalhe |
|------|---------|
| Linguagem | C# (.NET 10) |
| Interface | WPF (XAML) |
| Arquitetura | MVVM (Models, ViewModels, Views, Services) |
| Persistência | Arquivos JSON (`System.Text.Json`) |
| PDF | PDFsharp-MigraDoc |

## Como executar

1. Instale o **.NET 10 SDK** (Windows) e, de preferência, o Visual Studio 2022/2026.
2. Abra `SaciGestao/SaciGestao.slnx` e execute (**F5**), ou pelo terminal:

```bash
cd SaciGestao
dotnet run
```

**Logins de teste:** `admin`, `carolina` ou `enari` · senha `123`

---

## Funcionalidades

- **Login** com recuperação de senha por pergunta secreta. Login e sistema ficam na mesma janela.
- **Dashboard** com saudação e atalhos (novo cliente, imóvel ou contrato), indicadores (receita do mês, taxa de ocupação, carteira à venda e vendas fechadas), contratos de locação a vencer, funil comercial, gráfico de imóveis por status, imóveis para oferecer e avisos (renovações, reservas, imóveis sem foto, sem proprietário ou sem CIB).
- **Imóveis**
  - Lista em cards com foto de capa, preço, status e filtros (busca — inclusive pelo CIB —, tipo, finalidade, status, faixa de preço para venda e bairro).
  - **Fotos ilimitadas** por imóvel: adicione quantas quiser, escolha a capa e remova a qualquer momento.
  - Tela de detalhes com galeria, setas, contador e miniaturas; **clique na foto para ampliar** (zoom pela roda do mouse, botões + e −, duplo clique, arrastar e setas).
  - O **dono** do imóvel é escolhido entre os clientes do tipo *Proprietário*.
  - **Percentual da empresa** (comissão na venda ou taxa no aluguel) com o ganho estimado.
  - **CIB — Cadastro Imobiliário Brasileiro** ("CPF do imóvel" da Receita Federal): campo opcional e único, mostrado nos detalhes, nos contratos e no PDF.
- **Clientes**: cadastro completo (documentos, contato, endereço e foto opcional), filtros por tipo, busca por nome ou CPF e tela de detalhes. CPF, e-mail e data de nascimento são validados e o CPF não pode se repetir.
- **Contratos** de locação e venda
  - Ao escolher o imóvel, o **tipo do contrato**, o **valor**, o **proprietário** e as matrículas de energia/água são preenchidos automaticamente.
  - Venda à vista ou **parcelada** (o valor total é dividido nas parcelas mensais).
  - O status do imóvel muda sozinho para *Alugado* ou *Vendido* e volta para *Disponível* ao encerrar, cancelar ou excluir o contrato. Um imóvel só pode ter um contrato ativo.
  - Exportação do contrato em **PDF** (locação ou compra e venda), com botões para abrir o arquivo ou a pasta.
- **Financeiro**
  - Indicadores de receita na conta, a receber (pendente), ganho da empresa e repasse aos proprietários, com gráfico de recebido por mês.
  - Lista de **contratos ativos** com o progresso das parcelas; o pop-up de cada contrato mostra todas as parcelas, pagas e a vencer.
  - Tabela de pagamentos com filtros (período, tipo, situação e busca).
  - **Registrar pagamento** de qualquer parcela pendente (inclusive adiantada), com a parte da empresa e o repasse calculados na hora. Registrar já significa que o valor foi recebido; também é possível desfazer.
- **Configurações** (clique na foto do usuário): perfil (foto, nome, cargo, e-mail, telefone), troca de senha, pergunta de recuperação e sair da conta.
- **Avisos e confirmações** em janelas no visual do sistema (exclusões, PDF gerado, erros).

---

## Como os dados são salvos

O salvamento segue sempre este fluxo:

1. **Ao iniciar**, o sistema lê os arquivos da pasta `Json/` e carrega tudo na memória.
2. **Enquanto está aberto**, tudo fica **somente na memória** (nada é gravado a cada alteração).
3. **Ao fechar**:
   - pelo botão **Sair** (ou o X da janela / desligamento do Windows), os dados finais são gravados em JSON novos na pasta `Json/Comparacao/`. Os arquivos de `Json/` (entrada) **não são alterados**, então dá para comparar o antes (`Json/`) com o depois (`Json/Comparacao/`);
   - pelo botão **Salvar** do menu, os dados são gravados direto nos arquivos principais de `Json/` e o sistema fecha.

Se o sistema for encerrado de forma abrupta (queda de energia, processo finalizado pelo gerenciador de tarefas), as alterações feitas naquela sessão **não são salvas**.

### Arquivos

| Arquivo | Conteúdo |
|---------|----------|
| `SaciGestao/Json/imoveis.json` | Imóveis (inclui os nomes dos arquivos de foto e o CIB) |
| `SaciGestao/Json/clientes.json` | Clientes |
| `SaciGestao/Json/contratos.json` | Contratos |
| `SaciGestao/Json/pagamentos.json` | Pagamentos registrados no Financeiro |
| `SaciGestao/Json/usuarios.json` | Usuários do sistema (senhas guardadas como hash) |
| `SaciGestao/Json/Comparacao/*.json` | Mesmos arquivos, com os dados no momento em que o sistema foi fechado pelo "Sair" |
| `SaciGestao/Fotos/` | Fotos dos imóveis; `Fotos/clientes/` e `Fotos/usuarios/` guardam as fotos de clientes e usuários (as imagens escolhidas são copiadas para cá) |

- Se um arquivo não existir (primeira execução), o sistema cria os dados de exemplo e os grava ao fechar.
- Se um arquivo estiver corrompido, uma cópia dele é guardada como `*.json.corrompido` e o sistema recomeça com os dados de exemplo.
- Para **restaurar os dados de exemplo**, feche o sistema e apague os arquivos da pasta `Json/`. Para recomeçar uma apresentação, basta apagar `Json/Comparacao/`.

---

## Estrutura do projeto

```
Projeto_Acelera/
├── README.md
├── ESTRUTURA_DO_PROJETO.md   Documentação técnica detalhada
└── SaciGestao/
    ├── App.xaml(.cs)          Inicialização e salvamento ao fechar
    ├── MainWindow.xaml(.cs)   Janela única: login, menu lateral e área de conteúdo
    ├── Models/                Cliente, Imovel, Contrato, Pagamento, Usuario e modelos do Financeiro
    ├── Service/               Regras e dados em memória, Financeiro, PDF e Persistencia (JSON)
    ├── ViewModels/            Lógica de cada tela (MVVM)
    ├── Views/                 Telas XAML (Login, Dashboard, Imóveis, Clientes, Contratos, Financeiro, Configurações)
    ├── Helpers/               Máscaras de digitação e layout adaptável
    ├── Converters/            Conversores de foto e de textos
    ├── Resources/             Tema (cores e estilos) e logos
    ├── Json/                  Arquivos de dados
    └── Fotos/                 Fotos dos imóveis, clientes e usuários
```

Detalhes de cada camada, regras de negócio e validações estão em [`ESTRUTURA_DO_PROJETO.md`](ESTRUTURA_DO_PROJETO.md).

## Observações

- As senhas e respostas secretas ficam guardadas como **hash** (SHA-256 com sal) no `usuarios.json`; não é possível ler a senha no arquivo.
- O sistema não tem perfis de acesso: todos os usuários têm as mesmas permissões.

## Design e acessibilidade (W3C / WCAG 2.1)

- **Tema único** em `SaciGestao/Resources/Tema.xaml`: cores e estilos (botões, campos, tabelas, cartões) compartilhados por todas as telas. Vermelho escuro da marca para ações principais, verde para positivo, âmbar para atenção e vermelho vivo **somente para perigo** (excluir, erro, cancelado).
- Contraste mínimo de **4,5:1** para textos e **3:1** para bordas de campos e ícones (WCAG 1.4.3 e 1.4.11).
- A informação nunca depende só da cor: status sempre aparecem com texto.
- Idioma da interface declarado como `pt-BR` e botões só com ícone têm nome acessível (leitores de tela).
- Todo o sistema é operável pelo teclado (os cards de imóveis têm botão "Ver detalhes").
- Layout responsivo: o menu lateral vira só ícones ou gaveta (☰) em janelas estreitas, e as grades e tabelas se reorganizam conforme a largura.
