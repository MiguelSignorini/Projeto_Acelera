using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
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

        // Foto do cliente: nome de um arquivo já salvo em Fotos, ou o caminho completo de uma
        // imagem recém-escolhida (só é copiada para a pasta Fotos quando o cliente é salvo)
        private string? _fotoForm;
        public string? FotoForm
        {
            get => _fotoForm;
            set
            {
                if (SetProperty(ref _fotoForm, value))
                    OnPropertyChanged(nameof(TemFotoForm));
            }
        }
        public bool TemFotoForm => !string.IsNullOrEmpty(FotoForm);

        public ICommand EscolherFotoCommand { get; }
        public ICommand RemoverFotoCommand { get; }

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

        public int TotalProprietarios => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Proprietário && c.Status != StatusCliente.Inativo);
        public int TotalCompradores => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Comprador && c.Status != StatusCliente.Inativo);
        public int TotalInquilinos => ClienteService.Instancia.ObterTodos().Count(c => c.Tipo == TipoCliente.Inquilino && c.Status != StatusCliente.Inativo);

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

        private string _tituloFormulario = "Novo cliente";
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
            ExcluirDoDetalheCommand = new RelayCommand(_ => { if (ExcluirCliente(ClienteDetalhe)) VoltarParaLista(); });
            ExcluirDoFormCommand = new RelayCommand(_ => { if (ExcluirPorId(_idEmEdicao)) VoltarParaLista(); });
            EscolherFotoCommand = new RelayCommand(_ => EscolherFoto());
            RemoverFotoCommand = new RelayCommand(_ => FotoForm = null);

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
                var termo = Busca.Trim();
                var termoDigitos = Digitos(termo);
                // CPF encontra digitado com ou sem pontos e traço
                lista = [.. lista.Where(c => c.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
                                         c.Cpf.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
                                         (termoDigitos.Length >= 3 && Digitos(c.Cpf).Contains(termoDigitos)))];
            }

            Clientes = new ObservableCollection<Cliente>(lista);

            // Contadores dos filtros (Proprietários / Compradores / Inquilinos) mudam ao cadastrar ou excluir
            OnPropertyChanged(nameof(TotalProprietarios));
            OnPropertyChanged(nameof(TotalCompradores));
            OnPropertyChanged(nameof(TotalInquilinos));
        }

        private void CarregarForm(Cliente c)
        {
            _idEmEdicao = c.Id;
            NomeForm = c.Nome;
            FotoForm = c.FotoCaminho;
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

        private void EscolherFoto()
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Selecione a foto do cliente",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
            };

            if (dialogo.ShowDialog() == true)
                FotoForm = dialogo.FileName;
        }

        private static string Digitos(string? texto) => new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

        // Confere os dois dígitos verificadores do CPF (recusa também 000.000.000-00, 111.111.111-11...)
        private static bool CpfValido(string cpf)
        {
            if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
            for (int t = 9; t < 11; t++)
            {
                int soma = 0;
                for (int i = 0; i < t; i++) soma += (cpf[i] - '0') * (t + 1 - i);
                int digito = soma * 10 % 11 % 10;
                if (cpf[t] - '0' != digito) return false;
            }
            return true;
        }

        // Confirma e exclui; devolve true se o cliente foi excluído
        private static bool ExcluirPorId(int id)
        {
            if (id == 0) return false;

            var cliente = ClienteService.Instancia.ObterTodos().FirstOrDefault(c => c.Id == id);
            string nome = cliente?.Nome ?? "este cliente";

            // Não deixa excluir quem ainda está ligado a um contrato ou é dono de um imóvel
            bool temContrato = ContratoService.Instancia.ClienteTemContrato(id);
            bool temImovel = ImovelService.Instancia.ObterAtivos().Any(i => i.ProprietarioId == id);
            if (temContrato || temImovel)
            {
                Views.Dialogo.Avisar("Exclusão bloqueada",
                    $"Não é possível excluir {nome}: ele está vinculado a "
                    + (temContrato && temImovel ? "contrato(s) e imóvel(is)" : temContrato ? "contrato(s)" : "imóvel(is)")
                    + ".\n\nExclua ou altere esses registros antes, ou marque o cliente como Inativo.",
                    Views.TipoDialogo.Atencao);
                return false;
            }

            if (!Views.Dialogo.Confirmar("Excluir cliente",
                    $"{nome} será excluído definitivamente. Essa ação não pode ser desfeita.", "Excluir"))
                return false;

            ClienteService.Instancia.Excluir(id);
            return true;
        }

        private void AbrirNovo()
        {
            CarregarForm(new Cliente());
            TituloFormulario = "Novo cliente";
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
            TituloFormulario = "Editar cliente";
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

        private bool ExcluirCliente(Cliente? cliente)
        {
            if (cliente == null || !ExcluirPorId(cliente.Id)) return false;
            FiltrarClientes();
            return true;
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

            // Documentos e contatos precisam estar completos (a tela já só deixa digitar números)
            var invalidos = new List<string>();
            if (Digitos(CpfForm).Length != 11) invalidos.Add("CPF (11 números)");
            if (Digitos(RgForm).Length != 9) invalidos.Add("RG (9 números)");
            if (Digitos(TelefonePrincipalForm).Length < 10) invalidos.Add("Telefone Principal (DDD + número)");
            if (!string.IsNullOrWhiteSpace(TelefoneSecundarioForm) && Digitos(TelefoneSecundarioForm).Length < 10)
                invalidos.Add("Telefone Secundário (DDD + número)");
            if (Digitos(CepForm).Length != 8) invalidos.Add("CEP (8 números)");
            // Dígitos verificadores: conferidos em CPF novo ou alterado (os exemplos antigos usam CPFs fictícios)
            string? cpfGravado = ClienteService.Instancia.ObterTodos().FirstOrDefault(c => c.Id == _idEmEdicao)?.Cpf;
            if (Digitos(CpfForm).Length == 11 && Digitos(CpfForm) != Digitos(cpfGravado) && !CpfValido(Digitos(CpfForm)))
                invalidos.Add("CPF (número inválido)");
            if (!System.Text.RegularExpressions.Regex.IsMatch(EmailForm.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                invalidos.Add("E-mail (ex.: nome@email.com)");
            if (DataNascimentoForm is DateTime nasc && (nasc.Date > DateTime.Today || nasc.Date < DateTime.Today.AddYears(-120)))
                invalidos.Add("Data de Nascimento (não pode ser no futuro)");

            if (invalidos.Count > 0)
            {
                ErroForm = "Confira os campos: " + string.Join(", ", invalidos) + ".";
                return;
            }

            // CPF não pode se repetir entre clientes
            string cpfDigitos = Digitos(CpfForm);
            var mesmoCpf = ClienteService.Instancia.ObterTodos()
                .FirstOrDefault(c => c.Id != _idEmEdicao && Digitos(c.Cpf) == cpfDigitos);
            if (mesmoCpf != null)
            {
                ErroForm = $"Já existe um cliente com este CPF: {mesmoCpf.Nome}.";
                return;
            }

            // Foto nova escolhida: copia para Fotos\clientes (o JSON guarda "clientes/<arquivo>")
            string? foto = FotoForm;
            if (!string.IsNullOrEmpty(foto) && Path.IsPathRooted(foto))
            {
                try
                {
                    Directory.CreateDirectory(Persistencia.PastaFotosClientes);
                    string nomeArquivo = Guid.NewGuid().ToString("N") + Path.GetExtension(foto);
                    File.Copy(foto, Path.Combine(Persistencia.PastaFotosClientes, nomeArquivo));
                    foto = Persistencia.SubpastaClientes + "/" + nomeArquivo;
                }
                catch (Exception ex)
                {
                    ErroForm = "Não foi possível salvar a foto: " + ex.Message;
                    return;
                }
            }

            var cliente = new Cliente
            {
                Id = _idEmEdicao,
                FotoCaminho = foto,
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
            {
                ClienteService.Instancia.Atualizar(cliente);
                // Contratos e imóveis guardam uma cópia do nome: mantém igual ao cadastro
                ContratoService.Instancia.AtualizarNomeCliente(cliente.Id, cliente.Nome);
                ImovelService.Instancia.AtualizarNomeProprietario(cliente.Id, cliente.Nome);
            }

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