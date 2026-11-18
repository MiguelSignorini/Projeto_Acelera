using System;
using System.Collections.Generic;
using System.Linq;
using SaciGestao.Models;

namespace SaciGestao.Service
{
    public class UsuarioService
    {
        // Instância única: os usuários (e quem está logado) continuam os mesmos durante toda a execução
        public static UsuarioService Instancia { get; } = new UsuarioService();

        private readonly List<Usuario> _usuarios = new List<Usuario>();
        private int _proximoId = 1;

        // Usuário autenticado no momento (null = ninguém logado)
        public Usuario? UsuarioLogado { get; private set; }

        private const string Arquivo = "usuarios.json";

        private UsuarioService()
        {
            // Lê Json\usuarios.json ao iniciar; só grava quando o sistema fecha (ver App.OnExit)
            var lista = Persistencia.Carregar<Usuario>(Arquivo);
            if (lista != null && lista.Count > 0) // sem nenhum usuário ninguém entraria: recria o admin
            {
                _usuarios.AddRange(lista);
                _proximoId = _usuarios.Max(u => u.Id) + 1;

                // Arquivos antigos guardavam senha e resposta em texto puro: converte para hash ao carregar
                // (as senhas continuam as mesmas para o usuário; o JSON passa a guardar só o hash ao salvar)
                foreach (var u in _usuarios)
                {
                    if (!EhHash(u.Senha)) u.Senha = Proteger(u.Senha);
                    if (!EhHash(u.RespostaSecreta) && !string.IsNullOrWhiteSpace(u.RespostaSecreta))
                        u.RespostaSecreta = ProtegerResposta(u.RespostaSecreta);
                }
                return;
            }

            // Usuário de fábrica: alguém precisa existir para o primeiro login (senha 123, resposta "rex")
            _usuarios.Add(new Usuario
            {
                Id = _proximoId++,
                NomeUsuario = "admin",
                Senha = Proteger("123"),
                NomeCompleto = "Administrador",
                PerguntaSecreta = "Qual o nome do seu primeiro animal de estimação?",
                RespostaSecreta = ProtegerResposta("rex")
            });
        }

        // ============================================================
        // SENHAS: guardadas como hash SHA-256 com "sal" aleatório, nunca em texto puro.
        // Formato: sha256$<sal em base64>$<hash em hexadecimal>
        // ============================================================
        private const string PrefixoHash = "sha256$";

        private static bool EhHash(string? valor) => valor != null && valor.StartsWith(PrefixoHash, StringComparison.Ordinal);

        public static string Proteger(string texto)
        {
            byte[] sal = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            return PrefixoHash + Convert.ToBase64String(sal) + "$" + Calcular(sal, texto);
        }

        // Resposta secreta: ignora maiúsculas e espaços nas pontas (como antes)
        public static string ProtegerResposta(string resposta) => Proteger(NormalizarResposta(resposta));

        private static string NormalizarResposta(string resposta) => (resposta ?? string.Empty).Trim().ToLowerInvariant();

        private static string Calcular(byte[] sal, string texto)
        {
            byte[] dados = sal.Concat(System.Text.Encoding.UTF8.GetBytes(texto ?? string.Empty)).ToArray();
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(dados));
        }

        // Confere um texto digitado com o valor guardado (aceita também texto puro de arquivo antigo)
        private static bool Conferir(string? guardado, string texto)
        {
            if (string.IsNullOrEmpty(guardado)) return false;
            if (!EhHash(guardado)) return guardado == texto;

            var partes = guardado.Substring(PrefixoHash.Length).Split('$');
            if (partes.Length != 2) return false;
            try
            {
                byte[] sal = Convert.FromBase64String(partes[0]);
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(Calcular(sal, texto)),
                    System.Text.Encoding.ASCII.GetBytes(partes[1].ToUpperInvariant()));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool ConferirSenha(Usuario usuario, string senha) => Conferir(usuario.Senha, senha);

        public void Salvar()
        {
            Persistencia.Salvar(Arquivo, _usuarios);
        }

        public Usuario? ObterPorNomeUsuario(string nomeUsuario)
        {
            return _usuarios.FirstOrDefault(u =>
                string.Equals(u.NomeUsuario, nomeUsuario, StringComparison.OrdinalIgnoreCase));
        }

        // Confere usuário e senha; se bater, marca como logado e devolve o usuário
        public Usuario? Autenticar(string nomeUsuario, string senha)
        {
            var usuario = ObterPorNomeUsuario(nomeUsuario);
            if (usuario == null || !ConferirSenha(usuario, senha)) return null;

            UsuarioLogado = usuario;
            return usuario;
        }

        public bool ValidarResposta(string nomeUsuario, string resposta)
        {
            var usuario = ObterPorNomeUsuario(nomeUsuario);
            if (usuario == null) return false;

            return Conferir(usuario.RespostaSecreta, NormalizarResposta(resposta)) ||
                   Conferir(usuario.RespostaSecreta, (resposta ?? string.Empty).Trim()); // texto puro antigo
        }

        public bool RedefinirSenha(string nomeUsuario, string novaSenha)
        {
            var usuario = ObterPorNomeUsuario(nomeUsuario);
            if (usuario == null) return false;

            usuario.Senha = Proteger(novaSenha);
            return true;
        }

        public void Sair()
        {
            UsuarioLogado = null;
        }
    }
}
