using System;
using System.Collections.Generic;
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

        private int _idEmEdicao; // 0 = criando um cliente novo

        private string _subtituloFormulario = "Preencha os dados do cliente";
        public string SubtituloFormulario
        {
            get => _subtituloFormulario;
            set => SetProperty(ref _subtituloFormulario, value);
        }

        private string _erroForm = string.Empty;
        public string ErroForm
        {
            get => _erroForm;
            set => SetProperty(ref _erroForm, value);
        }

        private DateTime? _dataNascimentoForm;
        public DateTime? DataNascimentoForm
        {
            get => _dataNascimentoForm;
            set => SetProperty(ref _dataNascimentoForm, value);
        }

        public static IReadOnlyList<string> Nacionalidades { get; } =
            ["Brasileira", "Argentina", "Boliviana", "Chilena", "Colombiana", "Paraguaia", "Peruana", "Uruguaia",
             "Venezuelana", "Portuguesa", "Italiana", "Espanhola", "Alemã", "Francesa", "Americana", "Japonesa", "Chinesa", "Outra"];

        public static IReadOnlyList<string> EstadosCivis { get; } =
            ["Solteiro(a)", "Casado(a)", "Divorciado(a)", "Viúvo(a)", "União Estável"];

        private string? _estadoCivilForm;
        public string? EstadoCivilForm
        {
            get => _estadoCivilForm;
            set => SetProperty(ref _estadoCivilForm, value);
        }

        public static IReadOnlyList<TipoCliente> TiposEnum { get; } = Enum.GetValues<TipoCliente>();

        private TipoCliente _tipoForm;
        public TipoCliente TipoForm
        {
            get => _tipoForm;
            set => SetProperty(ref _tipoForm, value);
        }

        public static IReadOnlyList<StatusCliente> StatusesForm { get; } = Enum.GetValues<StatusCliente>();

        private StatusCliente _statusForm = StatusCliente.Ativo;
        public StatusCliente StatusForm
        {
            get => _statusForm;
            set => SetProperty(ref _statusForm, value);
        }

        private string _nomeForm = string.Empty;
        public string NomeForm
        {
            get => _nomeForm;
            set => SetProperty(ref _nomeForm, value);
        }

        private string _cpfForm = string.Empty;
        public string CpfForm
        {
            get => _cpfForm;
            set => SetProperty(ref _cpfForm, value);
        }

        private string _rgForm = string.Empty;
        public string RgForm
        {
            get => _rgForm;
            set => SetProperty(ref _rgForm, value);
        }

        private string _orgaoEmissorRgForm = string.Empty;
        public string OrgaoEmissorRgForm
        {
            get => _orgaoEmissorRgForm;
            set => SetProperty(ref _orgaoEmissorRgForm, value);
        }

        private string _nacionalidadeForm = string.Empty;
        public string NacionalidadeForm
        {
            get => _nacionalidadeForm;
            set => SetProperty(ref _nacionalidadeForm, value);
        }

        private string _telefonePrincipalForm = string.Empty;
        public string TelefonePrincipalForm
        {
            get => _telefonePrincipalForm;
            set => SetProperty(ref _telefonePrincipalForm, value);
        }

        private string _telefoneSecundarioForm = string.Empty;
        public string TelefoneSecundarioForm
        {
            get => _telefoneSecundarioForm;
            set => SetProperty(ref _telefoneSecundarioForm, value);
        }

        private string _emailForm = string.Empty;
        public string EmailForm
        {
            get => _emailForm;
            set => SetProperty(ref _emailForm, value);
        }

        private string _ruaForm = string.Empty;
        public string RuaForm
        {
            get => _ruaForm;
            set => SetProperty(ref _ruaForm, value);
        }

        private string _numeroForm = string.Empty;
        public string NumeroForm
        {
            get => _numeroForm;
            set => SetProperty(ref _numeroForm, value);
        }

        private string _bairroForm = string.Empty;
        public string BairroForm
        {
            get => _bairroForm;
            set => SetProperty(ref _bairroForm, value);
        }

        private string _cepForm = string.Empty;
        public string CepForm
        {
            get => _cepForm;
            set => SetProperty(ref _cepForm, value);
        }

        private string _cidadeForm = string.Empty;
        public string CidadeForm
        {
            get => _cidadeForm;
            set => SetProperty(ref _cidadeForm, value);
        }

        private string _profissaoForm = string.Empty;
        public string ProfissaoForm
        {
            get => _profissaoForm;
            set => SetProperty(ref _profissaoForm, value);
        }

        private string _empresaForm = string.Empty;
        public string EmpresaForm
        {
            get => _empresaForm;
            set => SetProperty(ref _empresaForm, value);
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
            ExcluirDoFormCommand = new RelayCommand(_ => { ExcluirPorId(_idEmEdicao); VoltarParaLista(); });

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

        private void CarregarForm(Cliente c)
        {
            _idEmEdicao = c.Id;
            NomeForm = c.Nome;
            CpfForm = c.Cpf;
            RgForm = c.Rg;
            OrgaoEmissorRgForm = c.OrgaoEmissorRg;
            NacionalidadeForm = c.Nacionalidade;
            TelefonePrincipalForm = c.TelefonePrincipal;
            TelefoneSecundarioForm = c.TelefoneSecundario;
            EmailForm = c.Email;
            RuaForm = c.Rua;
            NumeroForm = c.Numero;
            BairroForm = c.Bairro;
            CepForm = c.Cep;
            CidadeForm = c.Cidade;
            ProfissaoForm = c.Profissao;
            EmpresaForm = c.Empresa;
            DataNascimentoForm = c.DataNascimento;
            EstadoCivilForm = string.IsNullOrWhiteSpace(c.EstadoCivil) ? null : c.EstadoCivil;
            TipoForm = c.Tipo;
            StatusForm = c.Status;
            ErroForm = string.Empty;
        }

        private static void ExcluirPorId(int id)
        {
            if (id == 0) return;
            ClienteService.Instancia.Excluir(id);
        }

        private void AbrirNovo()
        {
            CarregarForm(new Cliente());
            TituloFormulario = "NOVO CLIENTE";
            SubtituloFormulario = "Preencha os dados do cliente";
            EditandoExistente = false;
            MostrandoLista = false;
            MostrandoDetalhes = false;
            MostrandoFormulario = true;
        }

        private void AbrirEditar(Cliente? cliente)
        {
            if (cliente == null) return;
            CarregarForm(cliente);
            TituloFormulario = "EDITAR CLIENTE";
            SubtituloFormulario = cliente.Nome;
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
            // Todos os campos são obrigatórios, menos o telefone secundário
            var faltando = new List<string>();
            void Exige(string? valor, string nome) { if (string.IsNullOrWhiteSpace(valor)) faltando.Add(nome); }

            Exige(NomeForm, "Nome Completo");
            Exige(CpfForm, "CPF");
            Exige(RgForm, "RG");
            Exige(OrgaoEmissorRgForm, "Órgão Emissor do RG");
            if (DataNascimentoForm == null) faltando.Add("Data de Nascimento");
            Exige(NacionalidadeForm, "Nacionalidade");
            Exige(EstadoCivilForm, "Estado Civil");
            Exige(TelefonePrincipalForm, "Telefone Principal");
            Exige(EmailForm, "E-mail");
            Exige(RuaForm, "Rua");
            Exige(NumeroForm, "Número");
            Exige(BairroForm, "Bairro");
            Exige(CepForm, "CEP");
            Exige(CidadeForm, "Cidade");
            Exige(ProfissaoForm, "Cargo / Profissão");
            Exige(EmpresaForm, "Empresa");

            if (faltando.Count > 0)
            {
                ErroForm = "Preencha os campos obrigatórios: " + string.Join(", ", faltando) + ".";
                return;
            }

            var cliente = new Cliente
            {
                Id = _idEmEdicao,
                Nome = NomeForm.Trim(),
                Cpf = CpfForm.Trim(),
                Rg = RgForm.Trim(),
                OrgaoEmissorRg = OrgaoEmissorRgForm.Trim(),
                Nacionalidade = NacionalidadeForm.Trim(),
                TelefonePrincipal = TelefonePrincipalForm.Trim(),
                TelefoneSecundario = TelefoneSecundarioForm.Trim(),
                Email = EmailForm.Trim(),
                Rua = RuaForm.Trim(),
                Numero = NumeroForm.Trim(),
                Bairro = BairroForm.Trim(),
                Cep = CepForm.Trim(),
                Cidade = CidadeForm.Trim(),
                Profissao = ProfissaoForm.Trim(),
                Empresa = EmpresaForm.Trim(),
                DataNascimento = DataNascimentoForm,
                EstadoCivil = EstadoCivilForm ?? string.Empty,
                Tipo = TipoForm,
                Status = StatusForm
            };

            if (cliente.Id == 0)
                ClienteService.Instancia.Adicionar(cliente);
            else
                ClienteService.Instancia.Atualizar(cliente);

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