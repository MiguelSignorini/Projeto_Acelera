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
                }
            }
        }

        // Retorna verdadeiro se for Locação (usado para mostrar/ocultar o índice de reajuste)
        public bool MostrarIndiceReajuste => ContratoForm?.Tipo == TipoContrato.Locacao;

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
                    .Where(i => i.Status == StatusImovel.Disponível || i.Id == imovelAtualId)
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

        private string _tituloFormulario = "NOVO CONTRATO";
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

        private void AoSelecionarImovel(int imovelId)
        {
            if (imovelId <= 0) return;

            var imovel = ImovelService.Instancia.ObterAtivos().FirstOrDefault(i => i.Id == imovelId);
            if (imovel == null) return;

            ContratoForm.ImovelTitulo = imovel.Titulo;
            if (ContratoForm.ValorMensal == 0)
            {
                ContratoForm.ValorMensal = imovel.Valor;
            }

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
            ErroForm = string.Empty;
            TituloFormulario = "NOVO CONTRATO";
            EditandoExistente = false;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaLocatarios));
            OnPropertyChanged(nameof(ListaLocadores));
            OnPropertyChanged(nameof(ImovelIdSelecionado));
            OnPropertyChanged(nameof(TipoSelecionadoForm));
            OnPropertyChanged(nameof(MostrarIndiceReajuste));

            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void Editar(Contrato? contrato)
        {
            if (contrato == null) return;
            ContratoForm = contrato.Clonar();
            ErroForm = string.Empty;
            TituloFormulario = "EDITAR CONTRATO";
            EditandoExistente = true;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaLocatarios));
            OnPropertyChanged(nameof(ListaLocadores));
            OnPropertyChanged(nameof(ImovelIdSelecionado));
            OnPropertyChanged(nameof(TipoSelecionadoForm));
            OnPropertyChanged(nameof(MostrarIndiceReajuste));

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

            if (ContratoForm.Tipo == TipoContrato.Locacao && string.IsNullOrWhiteSpace(ContratoForm.IndiceReajuste))
            {
                ErroForm = "Selecione o índice de reajuste para o contrato de locação.";
                return;
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