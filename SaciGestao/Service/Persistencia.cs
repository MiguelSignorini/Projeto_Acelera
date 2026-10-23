using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SaciGestao.Service
{
    // ============================================================
    // Persistência em JSON.
    //
    // Como funciona o salvamento do sistema:
    //   1. Ao iniciar, cada service lê o seu arquivo da pasta "Json" e guarda tudo em memória.
    //   2. Enquanto o sistema está aberto, tudo fica SOMENTE em memória.
    //   3. Ao fechar (botão Sair ou fechando a janela), o App chama Salvar() de cada service
    //      e os arquivos da pasta "Json" são reescritos.
    // ============================================================
    public static class Persistencia
    {
        private static readonly string _raiz = DescobrirRaiz();

        // Pasta onde ficam todos os arquivos .json (clientes.json, imoveis.json, ...)
        public static string PastaJson { get; } = Path.Combine(_raiz, "Json");

        // Pasta das fotos dos imóveis (o JSON guarda só o nome do arquivo de cada foto)
        public static string PastaFotos { get; } = Path.Combine(_raiz, "Fotos");

        private static readonly JsonSerializerOptions _opcoes = new JsonSerializerOptions
        {
            WriteIndented = true,                                  // JSON legível, com indentação
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // não escapa acentos (ex.: "Disponível")
            Converters = { new JsonStringEnumConverter() },        // enums gravados como texto
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                // Propriedades só de leitura (textos calculados para a tela) não vão para o arquivo
                Modifiers =
                {
                    info =>
                    {
                        if (info.Kind != JsonTypeInfoKind.Object) return;
                        foreach (var p in info.Properties.Where(p => p.Set == null).ToList())
                            info.Properties.Remove(p);
                    }
                }
            }
        };

        // Durante o desenvolvimento o programa roda em bin\Debug\...; sobe as pastas até achar o
        // projeto, assim o Json fica junto do código-fonte. Se não achar, usa a pasta do executável.
        private static string DescobrirRaiz()
        {
            var pasta = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && pasta != null; i++, pasta = pasta.Parent)
            {
                if (File.Exists(Path.Combine(pasta.FullName, "SaciGestao.csproj")))
                    return pasta.FullName;
            }
            return AppContext.BaseDirectory;
        }

        public static string CaminhoArquivo(string nomeArquivo) => Path.Combine(PastaJson, nomeArquivo);

        // Lê a lista do arquivo. Devolve null se o arquivo não existe, está vazio ou está corrompido.
        public static List<T>? Carregar<T>(string nomeArquivo)
        {
            string caminho = CaminhoArquivo(nomeArquivo);
            if (!File.Exists(caminho)) return null;

            try
            {
                var lista = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(caminho), _opcoes);
                return lista != null && lista.Count > 0 ? lista : null;
            }
            catch (Exception)
            {
                // Arquivo ilegível: guarda uma cópia para não perder nada e recomeça com os dados de exemplo
                try { File.Copy(caminho, caminho + ".corrompido", overwrite: true); } catch { }
                return null;
            }
        }

        public static void Salvar<T>(string nomeArquivo, IEnumerable<T> itens)
        {
            try
            {
                Directory.CreateDirectory(PastaJson);
                string caminho = CaminhoArquivo(nomeArquivo);
                string temporario = caminho + ".tmp";

                File.WriteAllText(temporario, JsonSerializer.Serialize(itens.ToList(), _opcoes));
                File.Move(temporario, caminho, overwrite: true);
            }
            catch (Exception)
            {
                // Sem permissão de escrita, por exemplo: o programa não deve travar ao fechar
            }
        }

        // O JSON guarda só o nome da foto; aqui vira o caminho completo para mostrar na tela.
        // Caminhos antigos (já completos) continuam funcionando.
        public static string CaminhoDaFoto(string foto)
        {
            return Path.IsPathRooted(foto) ? foto : Path.Combine(PastaFotos, foto);
        }
    }
}
