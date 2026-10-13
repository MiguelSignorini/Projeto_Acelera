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

        private UsuarioService()
        {
            // Usuário de fábrica: sem banco de dados, alguém precisa existir para o primeiro login
            _usuarios.Add(new Usuario
            {
                Id = _proximoId++,
                NomeUsuario = "admin",
                Senha = "123",
                NomeCompleto = "Administrador",
                PerguntaSecreta = "Qual o nome do seu primeiro animal de estimação?",
                RespostaSecreta = "rex"
            });
        }

        public Usuario? ObterPorNomeUsuario(string nomeUsuario)
        {
            return _usuarios.FirstOrDefault(u =>
                string.Equals(u.NomeUsuario, nomeUsuario, StringComparison.OrdinalIgnoreCase));
        }

        // Confere usuário e senha; se bater, marca como logado e devolve o usuário
        public Usuario? Autenticar(string nomeUsuario, string senha)
        {
            var usuario = _usuarios.FirstOrDefault(u =>
                string.Equals(u.NomeUsuario, nomeUsuario, StringComparison.OrdinalIgnoreCase) &&
                u.Senha == senha);

            if (usuario != null) UsuarioLogado = usuario;
            return usuario;
        }

        public bool ValidarResposta(string nomeUsuario, string resposta)
        {
            var usuario = ObterPorNomeUsuario(nomeUsuario);
            if (usuario == null) return false;

            return string.Equals(
                usuario.RespostaSecreta.Trim(), resposta.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool RedefinirSenha(string nomeUsuario, string novaSenha)
        {
            var usuario = ObterPorNomeUsuario(nomeUsuario);
            if (usuario == null) return false;

            usuario.Senha = novaSenha;
            return true;
        }

        public void Sair()
        {
            UsuarioLogado = null;
        }
    }
}
