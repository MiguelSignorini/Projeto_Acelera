# SaciGestao

Sistema desktop de gestão imobiliária da **Sacilotti Empreendimentos Imobiliários**, desenvolvido como projeto acadêmico.
Cadastro de imóveis (com fotos), clientes e contratos, com um dashboard de acompanhamento.

> Projeto feito **somente em C# / XAML**, sem banco de dados: os dados são guardados em arquivos **JSON**.

---

## Tecnologias

| Item | Detalhe |
|------|---------|
| Linguagem | C# (.NET 10) |
| Interface | WPF (XAML) |
| Arquitetura | MVVM (Models, ViewModels, Views, Services) |
| Persistência | Arquivos JSON (`System.Text.Json`) |
| Pacote | CommunityToolkit.Mvvm |

## Como executar

1. Instale o **.NET 10 SDK** (Windows) e, de preferência, o Visual Studio 2022/2026.
2. Abra `SaciGestao/SaciGestao.slnx` e execute (**F5**), ou pelo terminal:

```bash
cd SaciGestao
dotnet run
```

**Login de teste:** usuário `admin` · senha `123`

---

## Funcionalidades

- **Login** com recuperação de senha por pergunta secreta.
- **Dashboard** com faixa de boas-vindas e atalhos, indicadores (imóveis, clientes, contratos, receita mensal), gráfico de rosca de imóveis por status, barras por bairro e por tipo de cliente, resumo do portfólio, avisos (contratos vencendo, imóveis sem foto, reservas) e listas de contratos recentes e imóveis disponíveis.
- **Imóveis**
  - Lista em cards (4 por linha) com foto de capa, preço, status e filtros (busca, tipo, finalidade, status, faixa de preço e bairro).
  - **Fotos ilimitadas** por imóvel: adicione quantas quiser, escolha a capa e remova a qualquer momento.
  - Tela de detalhes com galeria de tamanho fixo (a foto aparece inteira, sem esticar), setas, contador e miniaturas.
  - **Clique na foto para ampliar**: popup com zoom (roda do mouse, botões + e −, duplo clique), arrastar para mover e setas para trocar de foto.
  - O **dono** do imóvel é escolhido entre os clientes do tipo *Proprietário*.
- **Clientes**: cadastro completo (documentos, contato, endereço), filtros por tipo e tela de detalhes.
- **Contratos** de locação e venda
  - Ao escolher o imóvel, o **proprietário** e as matrículas de energia/água são preenchidos automaticamente.
  - O status do imóvel muda sozinho para *Alugado* ou *Vendido* e volta para *Disponível* ao excluir o contrato.
  - Exportação do contrato em **PDF**.

---

## Como os dados são salvos

O salvamento segue sempre este fluxo:

1. **Ao iniciar**, o sistema lê os arquivos da pasta `Json/` e carrega tudo na memória.
2. **Enquanto está aberto**, tudo fica **somente na memória** (nada é gravado a cada alteração).
3. **Ao fechar** (botão *Sair*, botão X da janela ou desligamento do Windows), o sistema grava as informações novas nos arquivos JSON.

Se o sistema for encerrado de forma abrupta (queda de energia, processo finalizado pelo gerenciador de tarefas), as alterações feitas naquela sessão **não são salvas**.

### Arquivos

| Arquivo | Conteúdo |
|---------|----------|
| `SaciGestao/Json/imoveis.json` | Imóveis (inclui os nomes dos arquivos de foto) |
| `SaciGestao/Json/clientes.json` | Clientes |
| `SaciGestao/Json/contratos.json` | Contratos |
| `SaciGestao/Json/usuarios.json` | Usuários do sistema |
| `SaciGestao/Fotos/` | Fotos dos imóveis (as imagens escolhidas são copiadas para cá) |

- Se um arquivo não existir (primeira execução), o sistema cria os dados de exemplo e o grava ao fechar.
- Se um arquivo estiver corrompido, uma cópia dele é guardada como `*.json.corrompido` e o sistema recomeça com os dados de exemplo.
- Para **restaurar os dados de exemplo**, feche o sistema e apague os arquivos da pasta `Json/`.

---

## Estrutura do projeto

```
Projeto_Acelera/
├── README.md
└── SaciGestao/
    ├── App.xaml(.cs)          Inicialização e salvamento ao fechar
    ├── MainWindow.xaml(.cs)   Janela principal com o menu lateral
    ├── Models/                Cliente, Imovel, Contrato, Usuario
    ├── Service/               Regras e dados em memória + Persistencia (JSON)
    ├── ViewModels/            Lógica de cada tela (MVVM)
    ├── Views/                 Telas XAML (Login, Dashboard, Imóveis, Clientes, Contratos)
    ├── Converters/            Conversor de caminho de foto em imagem
    ├── Resources/             Logos
    ├── Json/                  Arquivos de dados (gerados ao fechar o sistema)
    └── Fotos/                 Fotos dos imóveis
```

## Observações

- As senhas ficam em texto puro no `usuarios.json` por simplicidade acadêmica; em um sistema real devem ser guardadas com hash.
- O menu **Financeiro** ainda não foi implementado.

## Design e acessibilidade (W3C / WCAG 2.1)

- **Paleta única** em `SaciGestao/Resources/Tema.xaml`: azul para ações principais, verde para positivo, âmbar para atenção e **vermelho somente para perigo** (excluir, erro, cancelado).
- Contraste mínimo de **4,5:1** para textos e **3:1** para bordas de campos e ícones (WCAG 1.4.3 e 1.4.11).
- A informação nunca depende só da cor: status sempre aparecem com texto.
- Idioma da interface declarado como `pt-BR` e botões só com ícone têm nome acessível (leitores de tela).
- Todo o sistema é operável pelo teclado (os cards de imóveis têm botão "Ver detalhes").
