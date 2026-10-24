using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class FaixaPreco
    {
        public string Texto { get; set; } = string.Empty;
        public decimal Min { get; set; }
        public decimal Max { get; set; }

        public override string ToString() => Texto;
    }

    public class ImoveisViewModel : ViewModelBase
    {
        private const string Todos = "Todos";
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        private readonly ImovelService _service = ImovelService.Instancia;
        private bool _carregando;

        // ============================================================
        // LISTA E FILTROS
        // ============================================================
        private string _busca = string.Empty;
        private string _tipo = Todos;
        private string _finalidade = Todos;
        private string _status = Todos;
        private string _bairro = Todos;
        private FaixaPreco _faixa;

        public ObservableCollection<Imovel> Imoveis { get; } = new ObservableCollection<Imovel>();
        public ObservableCollection<string> Bairros { get; } = new ObservableCollection<string>();
        public List<string> Tipos { get; }
        public List<string> Finalidades { get; }
        public List<string> Statuses { get; }
        public List<FaixaPreco> Faixas { get; }

        public string Busca
        {
            get => _busca;
            set { if (SetProperty(ref _busca, value)) Filtrar(); }
        }

        public string TipoSelecionado
        {
            get => _tipo;
            set { if (value != null && SetProperty(ref _tipo, value)) Filtrar(); }
        }

        public string FinalidadeSelecionada
        {
            get => _finalidade;
            set { if (value != null && SetProperty(ref _finalidade, value)) Filtrar(); }
        }

        public string StatusSelecionado
        {
            get => _status;
            set { if (value != null && SetProperty(ref _status, value)) Filtrar(); }
        }

        public string BairroSelecionado
        {
            get => _bairro;
            set { if (value != null && SetProperty(ref _bairro, value)) Filtrar(); }
        }

        public FaixaPreco FaixaSelecionada
        {
            get => _faixa;
            set { if (value != null && SetProperty(ref _faixa, value)) Filtrar(); }
        }

        // ============================================================
        // CONTROLE DE TELA (lista / formulário / detalhes na MESMA view)
        // ============================================================
        private bool _mostrandoFormulario;
        public bool MostrandoFormulario { get => _mostrandoFormulario; private set => SetProperty(ref _mostrandoFormulario, value); }

        private bool _mostrandoDetalhes;
        public bool MostrandoDetalhes { get => _mostrandoDetalhes; private set => SetProperty(ref _mostrandoDetalhes, value); }

        public bool MostrandoLista => !MostrandoFormulario && !MostrandoDetalhes;

        private void DefinirTela(bool formulario, bool detalhes)
        {
            _mostrandoFormulario = formulario;
            _mostrandoDetalhes = detalhes;
            OnPropertyChanged(nameof(MostrandoFormulario));
            OnPropertyChanged(nameof(MostrandoDetalhes));
            OnPropertyChanged(nameof(MostrandoLista));
        }

        // ============================================================
        // FORMULÁRIO (novo / editar)
        // ============================================================
        private int _idEmEdicao; // 0 = criando um imóvel novo

        private bool _editandoExistente;
        public bool EditandoExistente { get => _editandoExistente; private set => SetProperty(ref _editandoExistente, value); }

        private string _tituloFormulario = "Novo imóvel";
        public string TituloFormulario { get => _tituloFormulario; private set => SetProperty(ref _tituloFormulario, value); }

        private string _subtituloFormulario = "Preencha os dados do imóvel";
        public string SubtituloFormulario { get => _subtituloFormulario; private set => SetProperty(ref _subtituloFormulario, value); }

        private string _tituloForm = string.Empty;
        public string TituloForm { get => _tituloForm; set => SetProperty(ref _tituloForm, value); }

        private string _enderecoForm = string.Empty;
        public string EnderecoForm { get => _enderecoForm; set => SetProperty(ref _enderecoForm, value); }

        private string _numeroForm = string.Empty;
        public string NumeroForm { get => _numeroForm; set => SetProperty(ref _numeroForm, value); }

        private string _bairroForm = string.Empty;
        public string BairroForm { get => _bairroForm; set => SetProperty(ref _bairroForm, value); }

        private string _cidadeForm = string.Empty;
        public string CidadeForm { get => _cidadeForm; set => SetProperty(ref _cidadeForm, value); }

        private string _estadoForm = string.Empty;
        public string EstadoForm { get => _estadoForm; set => SetProperty(ref _estadoForm, value); }

        private string _cepForm = string.Empty;
        public string CepForm { get => _cepForm; set => SetProperty(ref _cepForm, value); }

        private string _valorForm = string.Empty;
        public string ValorForm { get => _valorForm; set => SetProperty(ref _valorForm, value); }

        private string _quartosForm = "0";
        public string QuartosForm { get => _quartosForm; set => SetProperty(ref _quartosForm, value); }

        private string _suitesForm = "0";
        public string SuitesForm { get => _suitesForm; set => SetProperty(ref _suitesForm, value); }

        private string _banheirosForm = "0";
        public string BanheirosForm { get => _banheirosForm; set => SetProperty(ref _banheirosForm, value); }

        private string _cozinhasForm = "1";
        public string CozinhasForm { get => _cozinhasForm; set => SetProperty(ref _cozinhasForm, value); }

        private string _salasForm = "1";
        public string SalasForm { get => _salasForm; set => SetProperty(ref _salasForm, value); }

        private string _vagasForm = "0";
        public string VagasForm { get => _vagasForm; set => SetProperty(ref _vagasForm, value); }

        private string _descricaoForm = string.Empty;
        public string DescricaoForm { get => _descricaoForm; set => SetProperty(ref _descricaoForm, value); }

        private string _matriculaEdpForm = string.Empty;
        public string MatriculaEdpForm { get => _matriculaEdpForm; set => SetProperty(ref _matriculaEdpForm, value); }

        private string _matriculaForm = string.Empty;
        public string MatriculaForm { get => _matriculaForm; set => SetProperty(ref _matriculaForm, value); }

        private string _iptuForm = string.Empty;
        public string IptuForm { get => _iptuForm; set => SetProperty(ref _iptuForm, value); }

        private string _matriculaSabespForm = string.Empty;
        public string MatriculaSabespForm { get => _matriculaSabespForm; set => SetProperty(ref _matriculaSabespForm, value); }

        // Dono: o usuário escolhe entre os clientes do tipo Proprietário
        private int _proprietarioIdForm;
        public int ProprietarioIdForm { get => _proprietarioIdForm; set => SetProperty(ref _proprietarioIdForm, value); }

        private List<Cliente> _listaProprietarios = new List<Cliente>();
        public List<Cliente> ListaProprietarios { get => _listaProprietarios; private set => SetProperty(ref _listaProprietarios, value); }

        // Só proprietários não inativos; mantém o dono atual na lista mesmo que tenha sido inativado
        private void CarregarProprietarios(int proprietarioAtualId)
        {
            ListaProprietarios = ClienteService.Instancia.ObterTodos()
                .Where(c => (c.Tipo == TipoCliente.Proprietário && c.Status != StatusCliente.Inativo)
                            || (proprietarioAtualId > 0 && c.Id == proprietarioAtualId))
                .OrderBy(c => c.Nome)
                .ToList();
        }

        private TipoImovel _tipoForm;
        public TipoImovel TipoForm { get => _tipoForm; set => SetProperty(ref _tipoForm, value); }

        private FinalidadeImovel _finalidadeForm;
        public FinalidadeImovel FinalidadeForm { get => _finalidadeForm; set => SetProperty(ref _finalidadeForm, value); }

        private StatusImovel _statusForm;
        public StatusImovel StatusForm { get => _statusForm; set => SetProperty(ref _statusForm, value); }

        private string _erroForm = string.Empty;
        public string ErroForm { get => _erroForm; private set => SetProperty(ref _erroForm, value); }

        public List<TipoImovel> TiposEnum { get; } = Enum.GetValues(typeof(TipoImovel)).Cast<TipoImovel>().ToList();
        public List<FinalidadeImovel> FinalidadesEnum { get; } = Enum.GetValues(typeof(FinalidadeImovel)).Cast<FinalidadeImovel>().ToList();
        public List<StatusImovel> StatusesEnum { get; } = Enum.GetValues(typeof(StatusImovel)).Cast<StatusImovel>().ToList();

        // ============================================================
        // DETALHES (guarda o próprio imóvel, a tela monta os campos)
        // ============================================================
        private Imovel? _imovelDetalhe;
        public Imovel? ImovelDetalhe { get => _imovelDetalhe; private set => SetProperty(ref _imovelDetalhe, value); }

        // ============================================================
        // FOTOS DO FORMULÁRIO
        // ============================================================
        public ObservableCollection<string> FotosForm { get; } = new ObservableCollection<string>();

        // As fotos escolhidas são copiadas para a pasta Fotos do projeto, assim continuam
        // existindo mesmo que o arquivo original seja movido ou apagado.

        private void AdicionarFotos()
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Selecione as fotos do imóvel",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
                Multiselect = true
            };

            if (dialogo.ShowDialog() != true) return;

            try
            {
                Directory.CreateDirectory(Persistencia.PastaFotos);

                foreach (var origem in dialogo.FileNames)
                {
                    string destino = Path.Combine(Persistencia.PastaFotos, Guid.NewGuid().ToString("N") + Path.GetExtension(origem));
                    File.Copy(origem, destino);
                    FotosForm.Add(Path.GetFileName(destino)); // o JSON guarda só o nome do arquivo
                }
            }
            catch (Exception ex)
            {
                ErroForm = "Não foi possível adicionar a foto: " + ex.Message;
            }
        }

        private void RemoverFoto(string? caminho)
        {
            if (caminho != null) FotosForm.Remove(caminho);
        }

        // Faz a foto escolhida virar a capa (primeira da lista)
        private void DefinirCapa(string? caminho)
        {
            if (caminho == null) return;
            int indice = FotosForm.IndexOf(caminho);
            if (indice > 0) FotosForm.Move(indice, 0);
        }

        // ============================================================
        // GALERIA DOS DETALHES
        // ============================================================
        private int _fotoIndice;

        public string? FotoAtual
        {
            get
            {
                var fotos = ImovelDetalhe?.Fotos;
                if (fotos == null || fotos.Count == 0) return null;
                return fotos[Math.Clamp(_fotoIndice, 0, fotos.Count - 1)];
            }
            set
            {
                var fotos = ImovelDetalhe?.Fotos;
                if (value == null || fotos == null) return;
                int i = fotos.IndexOf(value);
                if (i >= 0) { _fotoIndice = i; AtualizarGaleria(); }
            }
        }

        public bool TemFotoAtual => FotoAtual != null;
        public bool VariasFotos => (ImovelDetalhe?.Fotos.Count ?? 0) > 1;
        public string ContadorFotos => ImovelDetalhe == null || ImovelDetalhe.Fotos.Count == 0
            ? string.Empty
            : (_fotoIndice + 1) + " / " + ImovelDetalhe.Fotos.Count;

        private void AtualizarGaleria()
        {
            OnPropertyChanged(nameof(FotoAtual));
            OnPropertyChanged(nameof(ContadorFotos));
            OnPropertyChanged(nameof(VariasFotos));
            OnPropertyChanged(nameof(TemFotoAtual));
        }

        // Abre o popup com zoom; ao fechar, a galeria continua na foto em que o usuário parou
        private void AmpliarFoto()
        {
            var fotos = ImovelDetalhe?.Fotos;
            if (fotos == null || fotos.Count == 0) return;

            var principal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            var janela = new SaciGestao.Views.FotoZoomWindow(fotos, _fotoIndice) { Owner = principal };
            if (principal?.FindName("AreaConteudo") is FrameworkElement areaConteudo) janela.AjustarA(areaConteudo);
            janela.ShowDialog();

            _fotoIndice = janela.IndiceAtual;
            AtualizarGaleria();
        }

        private void MoverFoto(int passo)
        {
            int total = ImovelDetalhe?.Fotos.Count ?? 0;
            if (total < 2) return;
            _fotoIndice = (_fotoIndice + passo + total) % total;
            AtualizarGaleria();
        }

        // ============================================================
        // COMANDOS
        // ============================================================
        public ICommand AdicionarFotosCommand { get; }
        public ICommand RemoverFotoCommand { get; }
        public ICommand DefinirCapaCommand { get; }
        public ICommand AmpliarFotoCommand { get; }
        public ICommand FotoAnteriorCommand { get; }
        public ICommand ProximaFotoCommand { get; }
        public ICommand NovoCommand { get; }
        public ICommand VisualizarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand ExcluirCommand { get; }
        public ICommand LimparFiltrosCommand { get; }
        public ICommand SalvarFormCommand { get; }
        public ICommand CancelarFormCommand { get; }
        public ICommand ExcluirDoFormCommand { get; }
        public ICommand FecharDetalhesCommand { get; }
        public ICommand EditarDosDetalhesCommand { get; }

        public ImoveisViewModel()
        {
            Tipos = new List<string> { Todos }.Concat(Enum.GetNames(typeof(TipoImovel))).ToList();
            Finalidades = new List<string> { Todos }.Concat(Enum.GetNames(typeof(FinalidadeImovel))).ToList();
            Statuses = new List<string> { Todos }.Concat(Enum.GetNames(typeof(StatusImovel))).ToList();

            Faixas = new List<FaixaPreco>
            {
                new FaixaPreco { Texto = "Todas", Min = 0, Max = decimal.MaxValue },
                new FaixaPreco { Texto = "Até R$ 100 mil", Min = 0, Max = 100000 },
                new FaixaPreco { Texto = "R$ 100 mil a R$ 300 mil", Min = 100000, Max = 300000 },
                new FaixaPreco { Texto = "R$ 300 mil a R$ 500 mil", Min = 300000, Max = 500000 },
                new FaixaPreco { Texto = "Acima de R$ 500 mil", Min = 500000, Max = decimal.MaxValue }
            };
            _faixa = Faixas[0];

            AdicionarFotosCommand = new RelayCommand(_ => AdicionarFotos());
            RemoverFotoCommand = new RelayCommand(p => RemoverFoto(p as string));
            DefinirCapaCommand = new RelayCommand(p => DefinirCapa(p as string));
            AmpliarFotoCommand = new RelayCommand(_ => AmpliarFoto());
            FotoAnteriorCommand = new RelayCommand(_ => MoverFoto(-1));
            ProximaFotoCommand = new RelayCommand(_ => MoverFoto(1));
            NovoCommand = new RelayCommand(_ => Novo());
            VisualizarCommand = new RelayCommand(p => Visualizar(p as Imovel));
            EditarCommand = new RelayCommand(p => Editar(p as Imovel));
            ExcluirCommand = new RelayCommand(p => Excluir(p as Imovel));
            LimparFiltrosCommand = new RelayCommand(_ => LimparFiltros());
            SalvarFormCommand = new RelayCommand(_ => SalvarForm());
            CancelarFormCommand = new RelayCommand(_ => DefinirTela(false, false));
            ExcluirDoFormCommand = new RelayCommand(_ => ExcluirDoForm());
            FecharDetalhesCommand = new RelayCommand(_ => DefinirTela(false, false));
            EditarDosDetalhesCommand = new RelayCommand(_ => Editar(ImovelDetalhe));

            Recarregar();
        }

        // ---------- Abrir formulário ----------
        private void Novo()
        {
            _idEmEdicao = 0;
            EditandoExistente = false;
            TituloFormulario = "Novo imóvel";
            SubtituloFormulario = "Preencha os dados do imóvel";
            TituloForm = string.Empty;
            EnderecoForm = string.Empty;
            NumeroForm = string.Empty;
            BairroForm = string.Empty;
            CidadeForm = string.Empty;
            EstadoForm = string.Empty;
            CepForm = string.Empty;
            ValorForm = string.Empty;
            QuartosForm = "0";
            SuitesForm = "0";
            BanheirosForm = "0";
            CozinhasForm = "1";
            SalasForm = "1";
            VagasForm = "0";
            DescricaoForm = string.Empty;
            MatriculaEdpForm = string.Empty;
            MatriculaSabespForm = string.Empty;
            MatriculaForm = string.Empty;
            IptuForm = string.Empty;
            FotosForm.Clear();
            CarregarProprietarios(0);
            ProprietarioIdForm = 0;
            TipoForm = TipoImovel.Casa;
            FinalidadeForm = FinalidadeImovel.Venda;
            StatusForm = StatusImovel.Disponível;
            ErroForm = string.Empty;

            DefinirTela(formulario: true, detalhes: false);
        }

        private void Editar(Imovel? imovel)
        {
            if (imovel == null) return;

            _idEmEdicao = imovel.Id;
            EditandoExistente = true;
            TituloFormulario = "Editar imóvel";
            SubtituloFormulario = imovel.Codigo;
            TituloForm = imovel.Titulo;
            EnderecoForm = imovel.Endereco;
            NumeroForm = imovel.Numero;
            BairroForm = imovel.Bairro;
            CidadeForm = imovel.Cidade;
            EstadoForm = imovel.Estado;
            CepForm = imovel.Cep;
            ValorForm = imovel.Valor > 0 ? imovel.Valor.ToString("N2", PtBr) : string.Empty;
            QuartosForm = imovel.Quartos.ToString();
            SuitesForm = imovel.Suites.ToString();
            BanheirosForm = imovel.Banheiros.ToString();
            CozinhasForm = imovel.Cozinhas.ToString();
            SalasForm = imovel.Salas.ToString();
            VagasForm = imovel.Vagas.ToString();
            DescricaoForm = imovel.Descricao;
            MatriculaEdpForm = imovel.MatriculaEdp;
            MatriculaSabespForm = imovel.MatriculaSabesp;
            MatriculaForm = imovel.Matricula;
            IptuForm = imovel.Iptu;
            // Imóveis gravados antes do ProprietarioId existir: localiza o dono pelo nome
            int donoId = imovel.ProprietarioId;
            if (donoId == 0 && !string.IsNullOrWhiteSpace(imovel.ProprietarioNome))
            {
                donoId = ClienteService.Instancia.ObterTodos()
                    .FirstOrDefault(c => string.Equals(c.Nome, imovel.ProprietarioNome, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
            }
            FotosForm.Clear();
            foreach (var foto in imovel.Fotos) FotosForm.Add(foto);
            CarregarProprietarios(donoId);
            ProprietarioIdForm = donoId;
            TipoForm = imovel.Tipo;
            FinalidadeForm = imovel.Finalidade;
            StatusForm = imovel.Status;
            ErroForm = string.Empty;

            DefinirTela(formulario: true, detalhes: false);
        }

        private void SalvarForm()
        {
            if (string.IsNullOrWhiteSpace(TituloForm) ||
                string.IsNullOrWhiteSpace(EnderecoForm) ||
                string.IsNullOrWhiteSpace(BairroForm))
            {
                ErroForm = "Preencha título, endereço e bairro.";
                return;
            }

            if (!decimal.TryParse(ValorForm, NumberStyles.Number, PtBr, out decimal valor) || valor <= 0)
            {
                ErroForm = "Informe um valor maior que zero. Exemplo: 450.000,00";
                return;
            }

            if (!int.TryParse(QuartosForm, out int quartos) || quartos < 0 ||
                !int.TryParse(SuitesForm, out int suites) || suites < 0 ||
                !int.TryParse(BanheirosForm, out int banheiros) || banheiros < 0 ||
                !int.TryParse(CozinhasForm, out int cozinhas) || cozinhas < 0 ||
                !int.TryParse(SalasForm, out int salas) || salas < 0 ||
                !int.TryParse(VagasForm, out int vagas) || vagas < 0)
            {
                ErroForm = "Quartos, suítes, banheiros, cozinhas, salas e vagas devem ser números inteiros (0 ou mais).";
                return;
            }

            var proprietario = ListaProprietarios.FirstOrDefault(c => c.Id == ProprietarioIdForm);
            if (proprietario == null)
            {
                ErroForm = "Selecione o dono / proprietário do imóvel.";
                return;
            }

            var imovel = new Imovel
            {
                Id = _idEmEdicao,
                ProprietarioId = proprietario.Id,
                ProprietarioNome = proprietario.Nome,
                Fotos = FotosForm.ToList(),
                Titulo = TituloForm.Trim(),
                Endereco = EnderecoForm.Trim(),
                Numero = NumeroForm.Trim(),
                Bairro = BairroForm.Trim(),
                Cidade = CidadeForm.Trim(),
                Estado = EstadoForm.Trim().ToUpperInvariant(),
                Cep = CepForm.Trim(),
                Tipo = TipoForm,
                Finalidade = FinalidadeForm,
                Status = StatusForm,
                Valor = valor,
                Quartos = quartos,
                Suites = suites,
                Banheiros = banheiros,
                Cozinhas = cozinhas,
                Salas = salas,
                Vagas = vagas,
                Descricao = DescricaoForm.Trim(),
                MatriculaEdp = MatriculaEdpForm.Trim(),
                MatriculaSabesp = MatriculaSabespForm.Trim(),
                Matricula = MatriculaForm.Trim(),
                Iptu = IptuForm.Trim()
            };

            if (_idEmEdicao == 0)
                _service.Adicionar(imovel);
            else
                _service.Atualizar(imovel);

            DefinirTela(false, false);
            Recarregar();
        }

        private void ExcluirDoForm()
        {
            if (_idEmEdicao == 0) return;

            var resposta = MessageBox.Show(
                "Excluir este imóvel definitivamente? Essa ação não pode ser desfeita.",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes) return;

            _service.Excluir(_idEmEdicao);
            DefinirTela(false, false);
            Recarregar();
        }

        // ---------- Detalhes ----------
        private void Visualizar(Imovel? i)
        {
            if (i == null) return;

            ImovelDetalhe = i;
            _fotoIndice = 0;
            AtualizarGaleria();
            DefinirTela(false, true);
        }

        // ---------- Excluir a partir da lista ----------
        private void Excluir(Imovel? i)
        {
            if (i == null) return;

            var resposta = MessageBox.Show(
                "Excluir o imóvel " + i.Codigo + " definitivamente? Essa ação não pode ser desfeita.",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resposta == MessageBoxResult.Yes)
            {
                _service.Excluir(i.Id);
                Recarregar();
            }
        }

        private void LimparFiltros()
        {
            _carregando = true;
            _busca = string.Empty;
            _tipo = Todos;
            _finalidade = Todos;
            _status = Todos;
            _bairro = Todos;
            _faixa = Faixas[0];
            _carregando = false;

            OnPropertyChanged(nameof(Busca));
            OnPropertyChanged(nameof(TipoSelecionado));
            OnPropertyChanged(nameof(FinalidadeSelecionada));
            OnPropertyChanged(nameof(StatusSelecionado));
            OnPropertyChanged(nameof(BairroSelecionado));
            OnPropertyChanged(nameof(FaixaSelecionada));

            Filtrar();
        }

        // ---------- Lista e filtros ----------
        private void Recarregar()
        {
            _carregando = true;

            var bairros = _service.ObterAtivos()
                .Select(i => i.Bairro)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            Bairros.Clear();
            Bairros.Add(Todos);
            foreach (var b in bairros) Bairros.Add(b);

            if (!Bairros.Contains(_bairro)) _bairro = Todos;

            _carregando = false;
            OnPropertyChanged(nameof(BairroSelecionado));

            Filtrar();
        }

        private void Filtrar()
        {
            if (_carregando) return;

            IEnumerable<Imovel> consulta = _service.ObterAtivos();

            if (!string.IsNullOrWhiteSpace(_busca))
            {
                string t = _busca.Trim();
                consulta = consulta.Where(i =>
                    Contem(i.Codigo, t) || Contem(i.Titulo, t) ||
                    Contem(i.Endereco, t) || Contem(i.Bairro, t) || Contem(i.Cidade, t));
            }

            if (_tipo != Todos)
                consulta = consulta.Where(i => i.Tipo.ToString() == _tipo);

            if (_finalidade != Todos)
                consulta = consulta.Where(i => i.Finalidade.ToString() == _finalidade);

            if (_status != Todos)
                consulta = consulta.Where(i => i.Status.ToString() == _status);

            if (_bairro != Todos)
                consulta = consulta.Where(i => i.Bairro == _bairro);

            consulta = consulta.Where(i => i.Valor >= _faixa.Min && i.Valor <= _faixa.Max);

            Imoveis.Clear();
            foreach (var imovel in consulta) Imoveis.Add(imovel);
        }

        private static bool Contem(string texto, string trecho)
        {
            return (texto ?? string.Empty).IndexOf(trecho, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}