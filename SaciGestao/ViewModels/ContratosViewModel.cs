using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class ContratosViewModel : ViewModelBase
    {
        private ObservableCollection<Contrato> _contratos = new ObservableCollection<Contrato>();
        public ObservableCollection<Contrato> Contratos
        {
            get => _contratos;
            set => SetProperty(ref _contratos, value);
        }

        private Contrato _contratoForm = new Contrato();
        public Contrato ContratoForm
        {
            get => _contratoForm;
            set
            {
                if (SetProperty(ref _contratoForm, value))
                {
                    OnPropertyChanged(nameof(TipoSelecionadoForm));
                    OnPropertyChanged(nameof(MostrarIndiceReajuste));
                    OnPropertyChanged(nameof(MostrarParcelas));
                }
            }
        }

        // Propriedade para controlar e notificar a mudança entre Locação e Venda na View
        public TipoContrato TipoSelecionadoForm
        {
            get => ContratoForm?.Tipo ?? TipoContrato.Locacao;
            set
            {
                if (ContratoForm != null && ContratoForm.Tipo != value)
                {
                    ContratoForm.Tipo = value;
                    OnPropertyChanged(nameof(TipoSelecionadoForm));
                    OnPropertyChanged(nameof(MostrarIndiceReajuste));
                    OnPropertyChanged(nameof(MostrarParcelas));
                }
            }
        }

        // Retorna verdadeiro se for Locação (usado para mostrar/ocultar o índice de reajuste)
        public bool MostrarIndiceReajuste => ContratoForm?.Tipo == TipoContrato.Locacao;

        // Retorna verdadeiro se for Venda (usado para mostrar/ocultar a quantidade de parcelas)
        public bool MostrarParcelas => ContratoForm?.Tipo == TipoContrato.Venda;

        private Contrato? _contratoDetalhe;
        public Contrato? ContratoDetalhe
        {
            get => _contratoDetalhe;
            set => SetProperty(ref _contratoDetalhe, value);
        }

        public int ImovelIdSelecionado
        {
            get => ContratoForm?.ImovelId ?? 0;
            set
            {
                if (ContratoForm != null && ContratoForm.ImovelId != value)
                {
                    ContratoForm.ImovelId = value;
                    OnPropertyChanged(nameof(ImovelIdSelecionado));

                    AoSelecionarImovel(value);
                }
            }
        }

        public List<Imovel> ListaImoveis
        {
            get
            {
                var imovelAtualId = ContratoForm?.ImovelId ?? 0;
                return ImovelService.Instancia.ObterAtivos()
                    .Where(i => i.Status == StatusImovel.Disponível || i.Status == StatusImovel.Reservado || i.Id == imovelAtualId)
                    .ToList();
            }
        }

        // --- FILTRO 1: Inquilinos e Compradores ---
        public List<Cliente> ListaLocatarios
        {
            get
            {
                var locatarioAtualId = ContratoForm?.LocatarioId ?? 0;
                return ClienteService.Instancia.ObterTodos()
                    .Where(c => (c.Status != StatusCliente.Inativo &&
                                 (c.Tipo == TipoCliente.Inquilino || c.Tipo == TipoCliente.Comprador))
                                || c.Id == locatarioAtualId)
                    .ToList();
            }
        }

        // --- FILTRO 2: Proprietários / Locadores ---
        public List<Cliente> ListaLocadores
        {
            get
            {
                var locadorAtualId = ContratoForm?.LocadorId ?? 0;
                return ClienteService.Instancia.ObterTodos()
                    .Where(c => (c.Status != StatusCliente.Inativo && c.Tipo == TipoCliente.Proprietário)
                                || c.Id == locadorAtualId)
                    .ToList();
            }
        }

        private string _busca = string.Empty;
        public string Busca
        {
            get => _busca;
            set
            {
                if (SetProperty(ref _busca, value))
                    Filtrar();
            }
        }

        private string _filtroTipo = "Todos";
        public string FiltroTipo
        {
            get => _filtroTipo;
            set
            {
                if (SetProperty(ref _filtroTipo, value))
                    Filtrar();
            }
        }

        public List<string> IndicesReajuste { get; } = new List<string> { "IGP-M/FGV", "IPCA", "INPC" };

        private string _erroForm = string.Empty;
        public string ErroForm { get => _erroForm; private set => SetProperty(ref _erroForm, value); }

        public int TotalAtivos => ContratoService.Instancia.ObterAtivos().Count(c => c.Status == StatusContrato.Ativo);
        public decimal ReceitaMensal => ContratoService.Instancia.ObterAtivos()
            .Where(c => c.Status == StatusContrato.Ativo && c.Tipo == TipoContrato.Locacao)
            .Sum(c => c.ValorMensal);
        public int TotalTodos => ContratoService.Instancia.ObterAtivos().Count;

        public int TotalLocacao => ContratoService.Instancia.ObterAtivos().Count(c => c.Tipo == TipoContrato.Locacao);
        public int TotalVenda => ContratoService.Instancia.ObterAtivos().Count(c => c.Tipo == TipoContrato.Venda);

        private bool _mostrandoLista = true;
        public bool MostrandoLista
        {
            get => _mostrandoLista;
            set => SetProperty(ref _mostrandoLista, value);
        }

        private bool _mostrandoFormulario = false;
        public bool MostrandoFormulario
        {
            get => _mostrandoFormulario;
            set => SetProperty(ref _mostrandoFormulario, value);
        }

        private bool _mostrandoDetalhes = false;
        public bool MostrandoDetalhes
        {
            get => _mostrandoDetalhes;
            set => SetProperty(ref _mostrandoDetalhes, value);
        }

        private string _tituloFormulario = "Novo contrato";
        public string TituloFormulario
        {
            get => _tituloFormulario;
            set => SetProperty(ref _tituloFormulario, value);
        }

        private bool _editandoExistente = false;
        public bool EditandoExistente
        {
            get => _editandoExistente;
            set => SetProperty(ref _editandoExistente, value);
        }

        public Array TiposContratoEnum => Enum.GetValues(typeof(TipoContrato));
        public Array StatusesContratoEnum => Enum.GetValues(typeof(StatusContrato));

        public ICommand NovoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VisualizarCommand { get; }
        public ICommand ExcluirCommand { get; }
        public ICommand SalvarFormCommand { get; }
        public ICommand CancelarFormCommand { get; }
        public ICommand FecharDetalhesCommand { get; }
        public ICommand GerarPdfCommand { get; }

        public ContratosViewModel()
        {
            NovoCommand = new RelayCommand(_ => Novo());
            EditarCommand = new RelayCommand(param => Editar(param as Contrato));
            VisualizarCommand = new RelayCommand(param => Visualizar(param as Contrato));
            ExcluirCommand = new RelayCommand(param => Excluir(param as Contrato));
            SalvarFormCommand = new RelayCommand(_ => Salvar());
            CancelarFormCommand = new RelayCommand(_ => VoltarParaLista());
            FecharDetalhesCommand = new RelayCommand(_ => VoltarParaLista());
            GerarPdfCommand = new RelayCommand(_ => GerarPdf());

            Filtrar();
        }

        // Último valor preenchido automaticamente a partir de um imóvel (para saber se o usuário já mudou o valor)
        private decimal _valorSugerido;

        private void AoSelecionarImovel(int imovelId)
        {
            if (imovelId <= 0) return;

            var imovel = ImovelService.Instancia.ObterAtivos().FirstOrDefault(i => i.Id == imovelId);
            if (imovel == null) return;

            ContratoForm.ImovelTitulo = imovel.Titulo;

            // Contrato novo: o tipo acompanha a finalidade do imóvel (imóvel de venda -> Venda, de aluguel -> Locação)
            if (ContratoForm.Id == 0)
            {
                var tipo = imovel.Finalidade == FinalidadeImovel.Venda ? TipoContrato.Venda : TipoContrato.Locacao;
                if (ContratoForm.Tipo != tipo)
                {
                    ContratoForm.Tipo = tipo;
                    OnPropertyChanged(nameof(TipoSelecionadoForm));
                    OnPropertyChanged(nameof(MostrarIndiceReajuste));
                    OnPropertyChanged(nameof(MostrarParcelas));
                }
            }

            // Sugere o valor do imóvel se o campo está vazio ou ainda com o valor sugerido pelo imóvel anterior
            if (ContratoForm.ValorMensal == 0 || ContratoForm.ValorMensal == _valorSugerido)
            {
                ContratoForm.ValorMensal = imovel.Valor;
            }
            _valorSugerido = imovel.Valor;

            ContratoForm.UC_Energia = imovel.MatriculaEdp;
            ContratoForm.UC_Agua = imovel.MatriculaSabesp;

            var todosClientes = ClienteService.Instancia.ObterTodos();
            var dono = todosClientes.FirstOrDefault(c => imovel.ProprietarioId > 0 && c.Id == imovel.ProprietarioId)
                      ?? todosClientes.FirstOrDefault(c => !string.IsNullOrWhiteSpace(imovel.ProprietarioNome) &&
                          string.Equals(c.Nome, imovel.ProprietarioNome, StringComparison.OrdinalIgnoreCase));

            if (dono != null)
            {
                ContratoForm.LocadorId = dono.Id;
                ContratoForm.LocadorNome = dono.Nome;
            }

            OnPropertyChanged(nameof(ListaLocadores));
            OnPropertyChanged(nameof(ContratoForm));
        }

        private void GerarPdf()
        {
            if (ContratoDetalhe != null)
            {
                PdfContratoService.GerarECompletar(ContratoDetalhe);
            }
        }

        private void Filtrar()
        {
            var lista = ContratoService.Instancia.ObterAtivos();

            if (FiltroTipo == "Locacao")
                lista = lista.Where(c => c.Tipo == TipoContrato.Locacao).ToList();
            else if (FiltroTipo == "Venda")
                lista = lista.Where(c => c.Tipo == TipoContrato.Venda).ToList();

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var termo = Busca.ToLower();
                lista = lista.Where(c => c.Codigo.ToLower().Contains(termo) ||
                                         c.ImovelTitulo.ToLower().Contains(termo) ||
                                         c.LocatarioNome.ToLower().Contains(termo) ||
                                         c.LocadorNome.ToLower().Contains(termo)).ToList();
            }

            Contratos = new ObservableCollection<Contrato>(lista);
            OnPropertyChanged(nameof(TotalLocacao));
            OnPropertyChanged(nameof(TotalVenda));
            OnPropertyChanged(nameof(TotalAtivos));
            OnPropertyChanged(nameof(TotalTodos));
            OnPropertyChanged(nameof(ReceitaMensal));
        }

        private void Novo()
        {
            ContratoForm = new Contrato();
            _valorSugerido = 0;
            ErroForm = string.Empty;
            TituloFormulario = "Novo contrato";
            EditandoExistente = false;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaLocatarios));
            OnPropertyChanged(nameof(ListaLocadores));
            OnPropertyChanged(nameof(ImovelIdSelecionado));
            OnPropertyChanged(nameof(TipoSelecionadoForm));
            OnPropertyChanged(nameof(MostrarIndiceReajuste));
            OnPropertyChanged(nameof(MostrarParcelas));

            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void Editar(Contrato? contrato)
        {
            if (contrato == null) return;
            ContratoForm = contrato.Clonar();
            _valorSugerido = ImovelService.Instancia.ObterPorId(contrato.ImovelId)?.Valor ?? 0;
            ErroForm = string.Empty;
            TituloFormulario = "Editar contrato";
            EditandoExistente = true;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaLocatarios));
            OnPropertyChanged(nameof(ListaLocadores));
            OnPropertyChanged(nameof(ImovelIdSelecionado));
            OnPropertyChanged(nameof(TipoSelecionadoForm));
            OnPropertyChanged(nameof(MostrarIndiceReajuste));
            OnPropertyChanged(nameof(MostrarParcelas));

            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void Visualizar(Contrato? contrato)
        {
            if (contrato == null) return;
            ContratoDetalhe = contrato;
            MostrandoLista = false;
            MostrandoFormulario = false;
            MostrandoDetalhes = true;
        }

        private void Excluir(Contrato? contrato)
        {
            if (contrato == null) return;

            int pagamentos = FinanceiroService.Instancia.QuantidadeDoContrato(contrato.Id);
            string aviso = pagamentos > 0
                ? $"\n\nAtenção: {pagamentos} pagamento(s) registrado(s) deste contrato também serão removidos do Financeiro."
                : string.Empty;

            if (!Views.Dialogo.Confirmar("Excluir contrato",
                    $"O contrato {contrato.Codigo} será excluído definitivamente. Essa ação não pode ser desfeita.{aviso}",
                    "Excluir"))
                return;

            ContratoService.Instancia.Excluir(contrato.Id);
            ContratoService.Instancia.Salvar(); // Garante persistência imediata na memória/disco[cite: 4, 5]
            Filtrar();
        }

        private void Salvar()
        {
            if (ContratoForm == null) return;

            // Validação rigorosa para não deixar campos importantes vazios
            if (ContratoForm.ImovelId <= 0 || ContratoForm.LocatarioId <= 0 || ContratoForm.LocadorId <= 0)
            {
                ErroForm = "Por favor, selecione o imóvel, o inquilino/comprador e o proprietário.";
                return;
            }

            if (ContratoForm.ValorMensal <= 0)
            {
                ErroForm = "Informe um valor válido maior que zero.";
                return;
            }

            if (ContratoForm.DiaVencimento < 1 || ContratoForm.DiaVencimento > 31)
            {
                ErroForm = "O dia de vencimento deve estar entre 1 e 31.";
                return;
            }

            if (ContratoForm.PrazoMeses < 1)
            {
                ErroForm = "O prazo do contrato deve ser de pelo menos 1 mês.";
                return;
            }

            if (ContratoForm.Tipo == TipoContrato.Locacao && string.IsNullOrWhiteSpace(ContratoForm.IndiceReajuste))
            {
                ErroForm = "Selecione o índice de reajuste para o contrato de locação.";
                return;
            }

            if (ContratoForm.Tipo == TipoContrato.Venda && ContratoForm.QuantidadeParcelas < 1)
            {
                ErroForm = "Informe a quantidade de parcelas (mínimo 1) para o contrato de venda.";
                return;
            }

            // Um imóvel só pode ter um contrato ativo por vez
            if (ContratoForm.Status == StatusContrato.Ativo)
            {
                var outro = ContratoService.Instancia.ContratoAtivoDoImovel(ContratoForm.ImovelId, ContratoForm.Id);
                if (outro != null)
                {
                    ErroForm = $"Este imóvel já tem o contrato ativo {outro.Codigo}. Encerre ou cancele esse contrato antes.";
                    return;
                }
            }

            // Com pagamentos registrados, mudar tipo, início ou parcelas deixaria esses pagamentos sem parcela correspondente
            if (ContratoForm.Id != 0 && FinanceiroService.Instancia.QuantidadeDoContrato(ContratoForm.Id) > 0)
            {
                var original = ContratoService.Instancia.ObterAtivos().FirstOrDefault(c => c.Id == ContratoForm.Id);
                if (original != null &&
                    (original.Tipo != ContratoForm.Tipo ||
                     original.DataInicio.Date != ContratoForm.DataInicio.Date ||
                     (original.Tipo == TipoContrato.Venda && original.QuantidadeParcelas != ContratoForm.QuantidadeParcelas)))
                {
                    ErroForm = "Este contrato já tem pagamentos registrados: o tipo, a data de início e a quantidade de parcelas " +
                               "não podem mudar. Desfaça os pagamentos no Financeiro antes, se precisar alterar.";
                    return;
                }
            }

            ErroForm = string.Empty;
            ContratoForm.DataFim = ContratoForm.DataInicio.AddMonths(Math.Max(ContratoForm.PrazoMeses, 0));

            var imovel = ListaImoveis.FirstOrDefault(i => i.Id == ContratoForm.ImovelId);
            if (imovel != null) ContratoForm.ImovelTitulo = imovel.Titulo;

            var locatario = ListaLocatarios.FirstOrDefault(c => c.Id == ContratoForm.LocatarioId);
            if (locatario != null) ContratoForm.LocatarioNome = locatario.Nome;

            var locador = ListaLocadores.FirstOrDefault(c => c.Id == ContratoForm.LocadorId);
            if (locador != null) ContratoForm.LocadorNome = locador.Nome;

            if (ContratoForm.Id == 0)
                ContratoService.Instancia.Adicionar(ContratoForm);
            else
                ContratoService.Instancia.Atualizar(ContratoForm);

            // Salva de imediato para garantir persistência consistente[cite: 4, 5]
            ContratoService.Instancia.Salvar();

            VoltarParaLista();
        }

        private void VoltarParaLista()
        {
            MostrandoFormulario = false;
            MostrandoDetalhes = false;
            MostrandoLista = true;
            Filtrar();
        }
    }
}