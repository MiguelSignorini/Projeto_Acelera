using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class ClientesViewModel : ViewModelBase
    {
        private ObservableCollection<Cliente> _clientes = [];
        public ObservableCollection<Cliente> Clientes
        {
            get => _clientes;
            set => SetProperty(ref _clientes, value);
        }

        private Cliente _clienteForm = new();
        public Cliente ClienteForm
        {
            get => _clienteForm;
            set => SetProperty(ref _clienteForm, value);
        }

        private Cliente? _clienteDetalhe;
        public Cliente? ClienteDetalhe
        {
            get => _clienteDetalhe;
            set => SetProperty(ref _clienteDetalhe, value);
        }

        private string _busca = string.Empty;
        public string Busca
        {
            get => _busca;
            set
            {
                if (SetProperty(ref _busca, value))
                    FiltrarClientes();
            }
        }

        private string _filtroTipo = "Todos";
        public string FiltroTipo
        {
            get => _filtroTipo;
            set
            {
                if (SetProperty(ref _filtroTipo, value))
                    FiltrarClientes();
            }
        }

        public static int TotalProprietarios => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Proprietário && c.Status != StatusCliente.Inativo);
        public static int TotalCompradores => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Comprador && c.Status != StatusCliente.Inativo);
        public static int TotalInquilinos => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Inquilino && c.Status != StatusCliente.Inativo);

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

        private string _tituloFormulario = "NOVO CLIENTE";
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

        public ICommand NovoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VisualizarCommand { get; }
        public ICommand ExcluirCommand { get; }
        public ICommand SalvarFormCommand { get; }
        public ICommand CancelarFormCommand { get; }
        public ICommand FecharDetalhesCommand { get; }
        public ICommand EditarDoDetalheCommand { get; }
        public ICommand ExcluirDoDetalheCommand { get; }
        public ICommand ExcluirDoFormCommand { get; }

        public ClientesViewModel()
        {
            NovoCommand = new RelayCommand(_ => AbrirNovo());
            EditarCommand = new RelayCommand(param => AbrirEditar(param as Cliente));
            VisualizarCommand = new RelayCommand(param => AbrirVisualizar(param as Cliente));
            ExcluirCommand = new RelayCommand(param => ExcluirCliente(param as Cliente));
            SalvarFormCommand = new RelayCommand(_ => SalvarCliente());
            CancelarFormCommand = new RelayCommand(_ => VoltarParaLista());
            FecharDetalhesCommand = new RelayCommand(_ => VoltarParaLista());
            EditarDoDetalheCommand = new RelayCommand(_ => AbrirEditar(ClienteDetalhe));
            ExcluirDoDetalheCommand = new RelayCommand(_ => { ExcluirCliente(ClienteDetalhe); VoltarParaLista(); });
            ExcluirDoFormCommand = new RelayCommand(_ => { ExcluirCliente(ClienteForm); VoltarParaLista(); });

            FiltrarClientes();
        }

        private void FiltrarClientes()
        {
            var lista = ClienteService.Instancia.ObterTodos();

            if (FiltroTipo == "Proprietário")
                lista = [.. lista.Where(c => c.Tipo == TipoCliente.Proprietário)];
            else if (FiltroTipo == "Comprador")
                lista = [.. lista.Where(c => c.Tipo == TipoCliente.Comprador)];
            else if (FiltroTipo == "Inquilino")
                lista = [.. lista.Where(c => c.Tipo == TipoCliente.Inquilino)];

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var termo = Busca.ToLower();
                lista = [.. lista.Where(c => c.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
                                         c.Cpf.Contains(termo, StringComparison.CurrentCultureIgnoreCase))];
            }

            Clientes = new ObservableCollection<Cliente>(lista);
        }

        private void AbrirNovo()
        {
            ClienteForm = new Cliente();
            TituloFormulario = "NOVO CLIENTE";
            EditandoExistente = false;
            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void AbrirEditar(Cliente? cliente)
        {
            if (cliente == null) return;
            ClienteForm = cliente;
            TituloFormulario = "EDITAR CLIENTE";
            EditandoExistente = true;
            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void AbrirVisualizar(Cliente? cliente)
        {
            if (cliente == null) return;
            ClienteDetalhe = cliente;
            MostrandoLista = false;
            MostrandoFormulario = false;
            MostrandoDetalhes = true;
        }

        private void ExcluirCliente(Cliente? cliente)
        {
            if (cliente == null) return;
            ClienteService.Instancia.Excluir(cliente.Id);
            FiltrarClientes();
        }

        private void SalvarCliente()
        {
            if (ClienteForm.Id == 0)
                ClienteService.Instancia.Adicionar(ClienteForm);
            else
                ClienteService.Instancia.Atualizar(ClienteForm);

            VoltarParaLista();
        }

        private void VoltarParaLista()
        {
            MostrandoFormulario = false;
            MostrandoDetalhes = false;
            MostrandoLista = true;
            FiltrarClientes();
        }
    }
}