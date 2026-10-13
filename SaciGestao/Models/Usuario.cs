namespace SaciGestao.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;

        // Caminho do arquivo de imagem escolhido pelo usuário (não existe banco para salvar o arquivo em si)
        public string? FotoCaminho { get; set; }

        // Usados na recuperação de senha, já que não há e-mail/SMS disponível
        public string PerguntaSecreta { get; set; } = string.Empty;
        public string RespostaSecreta { get; set; } = string.Empty;
    }
}
