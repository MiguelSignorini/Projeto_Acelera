using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using SaciGestao.Models;

// ===================================================================
// Model + Service + Converter de Cliente reunidos num arquivo só.
// Os namespaces continuam separados por dentro (é o que o resto do
// projeto espera pra encontrar cada classe).
// O ViewModel da tela fica em ClientesViewModel.cs.
// ===================================================================

namespace SaciGestao.Models
{
    public enum TipoCliente { Proprietário, Comprador, Inquilino }

    public enum StatusCliente { Ativo, EmAtendimento, Inativo }

    public class Cliente
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Rg { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }

        public string TelefonePrincipal { get; set; } = string.Empty;
        public string TelefoneSecundario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Nome do arquivo da foto dentro da pasta Fotos (null = sem foto, a tela mostra as iniciais)
        public string? FotoCaminho { get; set; }

        public TipoCliente Tipo { get; set; }
        public StatusCliente Status { get; set; } = StatusCliente.Ativo;

        public string Profissao { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;

        // Documentos e dados civis
        public string OrgaoEmissorRg { get; set; } = string.Empty;
        public string Nacionalidade { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;

        // Endereço de domicílio
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;

        // O enum não aceita espaço no nome, então "Em Atendimento" vem daqui pra mostrar na tela
        public string StatusTexto => Status switch
        {
            StatusCliente.EmAtendimento => "Em Atendimento",
            _ => Status.ToString()
        };

        // ---------- Textos prontos para a tela de detalhes ----------
        private const string NaoInformado = "Não informado";

        private static string OuNaoInformado(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? NaoInformado : valor;
        }

        // Duas letras para o "avatar" (primeira e última palavra do nome)
        public string Iniciais
        {
            get
            {
                var partes = (Nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 0) return "?";
                if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpperInvariant();
                return (partes[0].Substring(0, 1) + partes[partes.Length - 1].Substring(0, 1)).ToUpperInvariant();
            }
        }

        public string NascimentoTexto
        {
            get
            {
                if (!DataNascimento.HasValue) return NaoInformado;

                var nascimento = DataNascimento.Value;
                string data = nascimento.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

                int idade = DateTime.Today.Year - nascimento.Year;
                if (nascimento.Date > DateTime.Today.AddYears(-idade)) idade--;

                return idade >= 0 ? data + "  (" + idade + " anos)" : data;
            }
        }

        public string RgTexto => OuNaoInformado(Rg);
        public string TelefonePrincipalTexto => OuNaoInformado(TelefonePrincipal);
        public string TelefoneSecundarioTexto => OuNaoInformado(TelefoneSecundario);
        public string ProfissaoTexto => OuNaoInformado(Profissao);
        public string EmpresaTexto => OuNaoInformado(Empresa);
        public string OrgaoEmissorRgTexto => OuNaoInformado(OrgaoEmissorRg);
        public string NacionalidadeTexto => OuNaoInformado(Nacionalidade);
        public string EstadoCivilTexto => OuNaoInformado(EstadoCivil);
        public string RuaTexto => OuNaoInformado(Rua);
        public string NumeroTexto => OuNaoInformado(Numero);
        public string BairroTexto => OuNaoInformado(Bairro);
        public string CepTexto => OuNaoInformado(Cep);
        public string CidadeTexto => OuNaoInformado(Cidade);

        // Cria uma cópia, usada na edição (só grava se o usuário salvar)
        // Usado pelas listas de seleção (ComboBox) para mostrar o nome
        public override string ToString() => Nome;

        public Cliente Clonar()
        {
            return (Cliente)MemberwiseClone();
        }
    }
}

namespace SaciGestao.Service
{
    public class ClienteService
    {
        // Instância única: assim os dados continuam ao trocar de tela no menu
        public static ClienteService Instancia { get; } = new ClienteService();

        private readonly List<Cliente> _clientes = new List<Cliente>();
        private int _proximoId = 1;

        private ClienteService()
        {
            CarregarDoArquivo();
        }

        private const string Arquivo = "clientes.json";

        // Lê Jsonientes.json ao iniciar; só grava quando o sistema fecha (ver App.OnExit)
        private void CarregarDoArquivo()
        {
            var lista = SaciGestao.Service.Persistencia.Carregar<Cliente>(Arquivo);
            if (lista != null)
            {
                _clientes.AddRange(lista);
                _proximoId = _clientes.Max(c => c.Id) + 1;
                return;
            }

            CarregarExemplos();
        }

        public void Salvar()
        {
            SaciGestao.Service.Persistencia.Salvar(Arquivo, _clientes);
        }

        public List<Cliente> ObterTodos()
        {
            return _clientes.ToList();
        }

        public void Adicionar(Cliente cliente)
        {
            cliente.Id = _proximoId++;
            _clientes.Add(cliente);
        }

        public void Atualizar(Cliente editado)
        {
            var existente = _clientes.FirstOrDefault(c => c.Id == editado.Id);
            if (existente == null) return;

            existente.Nome = editado.Nome;
            existente.Cpf = editado.Cpf;
            existente.Rg = editado.Rg;
            existente.DataNascimento = editado.DataNascimento;
            existente.TelefonePrincipal = editado.TelefonePrincipal;
            existente.TelefoneSecundario = editado.TelefoneSecundario;
            existente.Email = editado.Email;
            existente.Tipo = editado.Tipo;
            existente.Status = editado.Status;
            existente.Profissao = editado.Profissao;
            existente.Empresa = editado.Empresa;
            existente.OrgaoEmissorRg = editado.OrgaoEmissorRg;
            existente.Nacionalidade = editado.Nacionalidade;
            existente.EstadoCivil = editado.EstadoCivil;
            existente.Rua = editado.Rua;
            existente.Numero = editado.Numero;
            existente.Bairro = editado.Bairro;
            existente.Cep = editado.Cep;
            existente.Cidade = editado.Cidade;
            existente.FotoCaminho = editado.FotoCaminho;
        }

        public void Excluir(int id)
        {
            _clientes.RemoveAll(c => c.Id == id);
        }

        private void CarregarExemplos()
        {
            Adicionar(new Cliente { Nome = "João Silva", Cpf = "123.456.789-10", TelefonePrincipal = "(11) 98765-4321", Email = "joao.silva@email.com", Tipo = TipoCliente.Proprietário, Status = StatusCliente.Ativo, Profissao = "Engenheiro", Rg = "12.345.678-9", OrgaoEmissorRg = "SSP/SP", Nacionalidade = "Brasileira", EstadoCivil = "Casado(a)", Rua = "Rua das Flores", Numero = "120", Bairro = "Centro", Cep = "01001-000", Cidade = "São Paulo" });
            Adicionar(new Cliente { Nome = "Maria Oliveira", Cpf = "234.567.891-02", TelefonePrincipal = "(11) 98765-1122", Email = "maria.oliveira@email.com", Tipo = TipoCliente.Comprador, Status = StatusCliente.EmAtendimento, Profissao = "Professora", Rg = "23.456.789-0", OrgaoEmissorRg = "SSP/SP", Nacionalidade = "Brasileira", EstadoCivil = "Solteiro(a)", Rua = "Av. Paulista", Numero = "1500", Bairro = "Bela Vista", Cep = "01310-200", Cidade = "São Paulo" });
            Adicionar(new Cliente { Nome = "Carlos Souza", Cpf = "345.678.912-03", TelefonePrincipal = "(11) 97654-3311", Email = "carlos.souza@email.com", Tipo = TipoCliente.Inquilino, Status = StatusCliente.Ativo, Profissao = "Autônomo" });
            Adicionar(new Cliente { Nome = "Fernanda Lima", Cpf = "456.789.123-04", TelefonePrincipal = "(11) 96543-2210", Email = "fernanda.lima@email.com", Tipo = TipoCliente.Proprietário, Status = StatusCliente.EmAtendimento, Empresa = "Lima Comércio" });
            Adicionar(new Cliente { Nome = "Roberto Alves", Cpf = "567.891.234-05", TelefonePrincipal = "(11) 95432-1109", Email = "roberto.alves@email.com", Tipo = TipoCliente.Comprador, Status = StatusCliente.Inativo, Profissao = "Contador" });
            Adicionar(new Cliente { Nome = "Patrícia Gomes", Cpf = "678.912.345-06", TelefonePrincipal = "(11) 94321-0098", Email = "patricia.gomes@email.com", Tipo = TipoCliente.Inquilino, Status = StatusCliente.Ativo, Empresa = "Gomes & Cia" });
        }
    }
}

namespace SaciGestao.Converters
{
    // Compara o filtro selecionado (no ViewModel) com o valor de cada chip (ConverterParameter)
    public class FiltroValorParaBoolConverter : IValueConverter
    {
        public object Convert(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            return valor?.ToString() == parametro?.ToString();
        }

        public object ConvertBack(object valor, Type tipoAlvo, object parametro, CultureInfo cultura)
        {
            return (valor is bool marcado && marcado) ? parametro : Binding.DoNothing;
        }
    }
}
