using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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

        private string _bairroForm = string.Empty;
        public string BairroForm { get => _bairroForm; set => SetProperty(ref _bairroForm, value); }

        private string _valorForm = string.Empty;
        public string ValorForm { get => _valorForm; set => SetProperty(ref _valorForm, value); }

        private string _quartosForm = "0";
        public string QuartosForm { get => _quartosForm; set => SetProperty(ref _quartosForm, value); }

        private string _vagasForm = "0";
        public string VagasForm { get => _vagasForm; set => SetProperty(ref _vagasForm, value); }

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
        // COMANDOS
        // ============================================================
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
            BairroForm = string.Empty;
            ValorForm = string.Empty;
            QuartosForm = "0";
            VagasForm = "0";
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
            BairroForm = imovel.Bairro;
            ValorForm = imovel.Valor > 0 ? imovel.Valor.ToString("N2", PtBr) : string.Empty;
            QuartosForm = imovel.Quartos.ToString();
            VagasForm = imovel.Vagas.ToString();
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
                !int.TryParse(VagasForm, out int vagas) || vagas < 0)
            {
                ErroForm = "Quartos e vagas devem ser números inteiros (0 ou mais).";
                return;
            }

            var imovel = new Imovel
            {
                Id = _idEmEdicao,
                Titulo = TituloForm.Trim(),
                Endereco = EnderecoForm.Trim(),
                Bairro = BairroForm.Trim(),
                Tipo = TipoForm,
                Finalidade = FinalidadeForm,
                Status = StatusForm,
                Valor = valor,
                Quartos = quartos,
                Vagas = vagas
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
                    Contem(i.Endereco, t) || Contem(i.Bairro, t));
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
