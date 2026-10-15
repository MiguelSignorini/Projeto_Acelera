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
            set => SetProperty(ref _contratoForm, value);
        }

        private Contrato? _contratoDetalhe;
        public Contrato? ContratoDetalhe
        {
            get => _contratoDetalhe;
            set => SetProperty(ref _contratoDetalhe, value);
        }

        // Listas para os ComboBoxes pegando do serviço original do grupo
        public List<Imovel> ListaImoveis => ImovelService.Instancia.ObterAtivos();
        public List<Cliente> ListaClientes => ClienteService.Instancia.ObterTodos().Where(c => c.Status == StatusCliente.Ativo).ToList();

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
        }

        private void Novo()
        {
            ContratoForm = new Contrato();
            TituloFormulario = "NOVO CONTRATO";
            EditandoExistente = false;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaClientes));

            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void Editar(Contrato? contrato)
        {
            if (contrato == null) return;
            ContratoForm = contrato.Clonar();
            TituloFormulario = "EDITAR CONTRATO";
            EditandoExistente = true;

            OnPropertyChanged(nameof(ListaImoveis));
            OnPropertyChanged(nameof(ListaClientes));

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
            Filtrar();
        }

        private void Salvar()
        {
            if (ContratoForm == null) return;

            var imovel = ListaImoveis.FirstOrDefault(i => i.Id == ContratoForm.ImovelId);
            if (imovel != null) ContratoForm.ImovelTitulo = imovel.Titulo;

            var locatario = ListaClientes.FirstOrDefault(c => c.Id == ContratoForm.LocatarioId);
            if (locatario != null) ContratoForm.LocatarioNome = locatario.Nome;

            var locador = ListaClientes.FirstOrDefault(c => c.Id == ContratoForm.LocadorId);
            if (locador != null) ContratoForm.LocadorNome = locador.Nome;

            if (ContratoForm.Id == 0)
                ContratoService.Instancia.Adicionar(ContratoForm);
            else
                ContratoService.Instancia.Atualizar(ContratoForm);

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