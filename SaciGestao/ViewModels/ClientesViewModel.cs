using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class ClientesViewModel : ViewModelBase
    {
        private const string Todos = "Todos";
        private static readonly string[] StatusTextos = { "Ativo", "Em Atendimento", "Inativo" };
        private static readonly string[] EstadosCivisTextos = { "Solteiro(a)", "Casado(a)", "Separado(a)", "Divorciado(a)", "Viúvo(a)", "União Estável" };

        private readonly ClienteService _service = ClienteService.Instancia;

        private string _busca = string.Empty;
        private string _filtroTipo = Todos;

        private int _totalTodos;
        private int _totalProprietarios;
        private int _totalCompradores;
        private int _totalInquilinos;

        // ---------- Dados mostrados na lista ----------
        public ObservableCollection<Cliente> Clientes { get; } = new ObservableCollection<Cliente>();

        public int TotalTodos { get => _totalTodos; private set => SetProperty(ref _totalTodos, value); }
        public int TotalProprietarios { get => _totalProprietarios; private set => SetProperty(ref _totalProprietarios, value); }
        public int TotalCompradores { get => _totalCompradores; private set => SetProperty(ref _totalCompradores, value); }
        public int TotalInquilinos { get => _totalInquilinos; private set => SetProperty(ref _totalInquilinos, value); }

        // ---------- Filtros (cada um refiltra a lista ao mudar) ----------
        public string Busca
        {
            get => _busca;
            set { if (SetProperty(ref _busca, value)) Filtrar(); }
        }

        public string FiltroTipo
        {
            get => _filtroTipo;
            set { if (value != null && SetProperty(ref _filtroTipo, value)) Filtrar(); }
        }

        // ---------- Controle de qual "tela" aparece: lista, detalhes ou formulário ----------
        // (igual ao padrão usado em Imóveis: nada de popup, troca o conteúdo na mesma tela)
        private bool _mostrandoFormulario;
        private bool _mostrandoDetalhes;
        public bool MostrandoLista => !_mostrandoFormulario && !_mostrandoDetalhes;
        public bool MostrandoFormulario => _mostrandoFormulario;
        public bool MostrandoDetalhes => _mostrandoDetalhes;

        private void AtualizarTela()
        {
            OnPropertyChanged(nameof(MostrandoLista));
            OnPropertyChanged(nameof(MostrandoFormulario));
            OnPropertyChanged(nameof(MostrandoDetalhes));
        }

        private void AbrirFormulario()
        {
            _mostrandoFormulario = true;
            _mostrandoDetalhes = false;
            AtualizarTela();
        }

        private void FecharFormulario()
        {
            _mostrandoFormulario = false;
            AtualizarTela();
        }

        private void AbrirDetalhes()
        {
            _mostrandoDetalhes = true;
            _mostrandoFormulario = false;
            AtualizarTela();
        }

        private void FecharDetalhes()
        {
            _mostrandoDetalhes = false;
            AtualizarTela();
        }

        // ---------- Cliente mostrado na tela de detalhes ----------
        private Cliente? _clienteDetalhe;
        public Cliente? ClienteDetalhe { get => _clienteDetalhe; private set => SetProperty(ref _clienteDetalhe, value); }

        // ---------- Dados do formulário (Novo / Editar) ----------
        private Cliente? _clienteEmEdicao;
        private string _tituloFormulario = "Novo cliente";
        private string _subtituloFormulario = "Preencha os dados do novo cliente.";
        private string _erroForm = string.Empty;

        public string TituloFormulario { get => _tituloFormulario; private set => SetProperty(ref _tituloFormulario, value); }
        public string SubtituloFormulario { get => _subtituloFormulario; private set => SetProperty(ref _subtituloFormulario, value); }
        public bool EditandoExistente => _clienteEmEdicao != null && _clienteEmEdicao.Id != 0;
        public string ErroForm { get => _erroForm; private set => SetProperty(ref _erroForm, value); }

        private string _nomeForm = string.Empty;
        private string _cpfForm = string.Empty;
        private string _rgForm = string.Empty;
        private DateTime? _dataNascimentoForm;
        private string _telefonePrincipalForm = string.Empty;
        private string _telefoneSecundarioForm = string.Empty;
        private string _emailForm = string.Empty;
        private TipoCliente _tipoForm = TipoCliente.Proprietário;
        private string _statusForm = "Ativo";
        private string _profissaoForm = string.Empty;
        private string _empresaForm = string.Empty;
        private string _orgaoEmissorRgForm = string.Empty;
        private string _nacionalidadeForm = string.Empty;
        private string _estadoCivilForm = string.Empty;
        private string _ruaForm = string.Empty;
        private string _numeroForm = string.Empty;
        private string _bairroForm = string.Empty;
        private string _cepForm = string.Empty;
        private string _cidadeForm = string.Empty;

        public string NomeForm { get => _nomeForm; set => SetProperty(ref _nomeForm, value); }
        public string CpfForm { get => _cpfForm; set => SetProperty(ref _cpfForm, value); }
        public string RgForm { get => _rgForm; set => SetProperty(ref _rgForm, value); }
        public DateTime? DataNascimentoForm { get => _dataNascimentoForm; set => SetProperty(ref _dataNascimentoForm, value); }
        public string TelefonePrincipalForm { get => _telefonePrincipalForm; set => SetProperty(ref _telefonePrincipalForm, value); }
        public string TelefoneSecundarioForm { get => _telefoneSecundarioForm; set => SetProperty(ref _telefoneSecundarioForm, value); }
        public string EmailForm { get => _emailForm; set => SetProperty(ref _emailForm, value); }
        public TipoCliente TipoForm { get => _tipoForm; set => SetProperty(ref _tipoForm, value); }
        public string StatusForm { get => _statusForm; set => SetProperty(ref _statusForm, value); }
        public string ProfissaoForm { get => _profissaoForm; set => SetProperty(ref _profissaoForm, value); }
        public string EmpresaForm { get => _empresaForm; set => SetProperty(ref _empresaForm, value); }
        public string OrgaoEmissorRgForm { get => _orgaoEmissorRgForm; set => SetProperty(ref _orgaoEmissorRgForm, value); }
        public string NacionalidadeForm { get => _nacionalidadeForm; set => SetProperty(ref _nacionalidadeForm, value); }
        public string EstadoCivilForm { get => _estadoCivilForm; set => SetProperty(ref _estadoCivilForm, value); }
        public string RuaForm { get => _ruaForm; set => SetProperty(ref _ruaForm, value); }
        public string NumeroForm { get => _numeroForm; set => SetProperty(ref _numeroForm, value); }
        public string BairroForm { get => _bairroForm; set => SetProperty(ref _bairroForm, value); }
        public string CepForm { get => _cepForm; set => SetProperty(ref _cepForm, value); }
        public string CidadeForm { get => _cidadeForm; set => SetProperty(ref _cidadeForm, value); }

        public List<TipoCliente> TiposEnum { get; } = Enum.GetValues(typeof(TipoCliente)).Cast<TipoCliente>().ToList();
        public List<string> StatusesForm { get; } = StatusTextos.ToList();
        public List<string> EstadosCivis { get; } = EstadosCivisTextos.ToList();

        // ---------- Comandos ----------
        public ICommand NovoCommand { get; }
        public ICommand VisualizarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand ExcluirCommand { get; }

        public ICommand FecharDetalhesCommand { get; }
        public ICommand EditarDoDetalheCommand { get; }
        public ICommand ExcluirDoDetalheCommand { get; }

        public ICommand SalvarFormCommand { get; }
        public ICommand CancelarFormCommand { get; }
        public ICommand ExcluirDoFormCommand { get; }

        public ClientesViewModel()
        {
            NovoCommand = new RelayCommand(_ => Novo());
            VisualizarCommand = new RelayCommand(p => Visualizar(p as Cliente));
            EditarCommand = new RelayCommand(p => Editar(p as Cliente));
            ExcluirCommand = new RelayCommand(p => Excluir(p as Cliente));

            FecharDetalhesCommand = new RelayCommand(_ => FecharDetalhes());
            EditarDoDetalheCommand = new RelayCommand(_ => EditarDoDetalhe());
            ExcluirDoDetalheCommand = new RelayCommand(_ => ExcluirDoDetalhe());

            SalvarFormCommand = new RelayCommand(_ => SalvarForm());
            CancelarFormCommand = new RelayCommand(_ => FecharFormulario());
            ExcluirDoFormCommand = new RelayCommand(_ => ExcluirDoForm());

            Recarregar();
        }

        // ---------- Ações da lista ----------
        private void Novo()
        {
            _clienteEmEdicao = new Cliente();
            CarregarFormulario(_clienteEmEdicao);
            TituloFormulario = "Novo cliente";
            SubtituloFormulario = "Preencha os dados do novo cliente.";
            ErroForm = string.Empty;
            OnPropertyChanged(nameof(EditandoExistente));
            AbrirFormulario();
        }

        private void Editar(Cliente? cliente)
        {
            if (cliente == null) return;

            _clienteEmEdicao = cliente.Clonar();   // edita a cópia; só grava se salvar
            CarregarFormulario(_clienteEmEdicao);
            TituloFormulario = "Editar cliente";
            SubtituloFormulario = "Atualize os dados do cliente.";
            ErroForm = string.Empty;
            OnPropertyChanged(nameof(EditandoExistente));
            AbrirFormulario();
        }

        private void Visualizar(Cliente? c)
        {
            if (c == null) return;

            ClienteDetalhe = c;
            AbrirDetalhes();
        }

        // ---------- Ações da tela de detalhes ----------
        private void EditarDoDetalhe()
        {
            var cliente = ClienteDetalhe;
            FecharDetalhes();
            Editar(cliente);
        }

        private void ExcluirDoDetalhe()
        {
            var cliente = ClienteDetalhe;
            if (cliente == null || !ConfirmarExclusao(cliente)) return;

            _service.Excluir(cliente.Id);
            Recarregar();
            FecharDetalhes();
        }

        // Qualquer cliente pode ser excluído, independente do status
        private static bool ConfirmarExclusao(Cliente c)
        {
            var resposta = MessageBox.Show(
                "Excluir definitivamente o cliente " + c.Nome + "?",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return resposta == MessageBoxResult.Yes;
        }

        private void Excluir(Cliente? c)
        {
            if (c == null || !ConfirmarExclusao(c)) return;

            _service.Excluir(c.Id);
            Recarregar();
        }

        // ---------- Ações do formulário ----------
        private void CarregarFormulario(Cliente c)
        {
            NomeForm = c.Nome;
            CpfForm = c.Cpf;
            RgForm = c.Rg;
            DataNascimentoForm = c.DataNascimento;
            TelefonePrincipalForm = c.TelefonePrincipal;
            TelefoneSecundarioForm = c.TelefoneSecundario;
            EmailForm = c.Email;
            TipoForm = c.Tipo;
            StatusForm = c.StatusTexto;
            ProfissaoForm = c.Profissao;
            EmpresaForm = c.Empresa;
            OrgaoEmissorRgForm = c.OrgaoEmissorRg;
            NacionalidadeForm = c.Nacionalidade;
            EstadoCivilForm = c.EstadoCivil;
            RuaForm = c.Rua;
            NumeroForm = c.Numero;
            BairroForm = c.Bairro;
            CepForm = c.Cep;
            CidadeForm = c.Cidade;
        }

        private void SalvarForm()
        {
            if (string.IsNullOrWhiteSpace(NomeForm) ||
                string.IsNullOrWhiteSpace(CpfForm) ||
                string.IsNullOrWhiteSpace(EmailForm))
            {
                ErroForm = "Preencha nome, CPF e e-mail.";
                return;
            }

            if (!EmailForm.Contains('@'))
            {
                ErroForm = "Informe um e-mail válido.";
                return;
            }

            if (_clienteEmEdicao == null) return;

            _clienteEmEdicao.Nome = NomeForm.Trim();
            _clienteEmEdicao.Cpf = CpfForm.Trim();
            _clienteEmEdicao.Rg = RgForm.Trim();
            _clienteEmEdicao.DataNascimento = DataNascimentoForm;
            _clienteEmEdicao.TelefonePrincipal = TelefonePrincipalForm.Trim();
            _clienteEmEdicao.TelefoneSecundario = TelefoneSecundarioForm.Trim();
            _clienteEmEdicao.Email = EmailForm.Trim();
            _clienteEmEdicao.Tipo = TipoForm;
            _clienteEmEdicao.Status = StatusDeTexto(StatusForm);
            _clienteEmEdicao.Profissao = ProfissaoForm.Trim();
            _clienteEmEdicao.Empresa = EmpresaForm.Trim();
            _clienteEmEdicao.OrgaoEmissorRg = (OrgaoEmissorRgForm ?? string.Empty).Trim();
            _clienteEmEdicao.Nacionalidade = (NacionalidadeForm ?? string.Empty).Trim();
            _clienteEmEdicao.EstadoCivil = (EstadoCivilForm ?? string.Empty).Trim();
            _clienteEmEdicao.Rua = (RuaForm ?? string.Empty).Trim();
            _clienteEmEdicao.Numero = (NumeroForm ?? string.Empty).Trim();
            _clienteEmEdicao.Bairro = (BairroForm ?? string.Empty).Trim();
            _clienteEmEdicao.Cep = (CepForm ?? string.Empty).Trim();
            _clienteEmEdicao.Cidade = (CidadeForm ?? string.Empty).Trim();

            if (EditandoExistente)
                _service.Atualizar(_clienteEmEdicao);
            else
                _service.Adicionar(_clienteEmEdicao);

            Recarregar();
            FecharFormulario();
        }

        private void ExcluirDoForm()
        {
            if (_clienteEmEdicao == null || !EditandoExistente) return;

            if (!ConfirmarExclusao(_clienteEmEdicao)) return;

            _service.Excluir(_clienteEmEdicao.Id);
            Recarregar();
            FecharFormulario();
        }

        private static StatusCliente StatusDeTexto(string? texto)
        {
            return texto switch
            {
                "Em Atendimento" => StatusCliente.EmAtendimento,
                "Inativo" => StatusCliente.Inativo,
                _ => StatusCliente.Ativo
            };
        }

        // ---------- Lista e filtros ----------
        private void Recarregar()
        {
            var todos = _service.ObterTodos();

            TotalTodos = todos.Count;
            TotalProprietarios = todos.Count(c => c.Tipo == TipoCliente.Proprietário);
            TotalCompradores = todos.Count(c => c.Tipo == TipoCliente.Comprador);
            TotalInquilinos = todos.Count(c => c.Tipo == TipoCliente.Inquilino);

            Filtrar();
        }

        private void Filtrar()
        {
            IEnumerable<Cliente> consulta = _service.ObterTodos();

            if (!string.IsNullOrWhiteSpace(_busca))
            {
                string t = _busca.Trim();
                consulta = consulta.Where(c => Contem(c.Nome, t) || Contem(c.Cpf, t));
            }

            if (_filtroTipo != Todos)
                consulta = consulta.Where(c => c.Tipo.ToString() == _filtroTipo);

            Clientes.Clear();
            foreach (var cliente in consulta) Clientes.Add(cliente);
        }

        private static bool Contem(string texto, string trecho)
        {
            return (texto ?? string.Empty).IndexOf(trecho, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
