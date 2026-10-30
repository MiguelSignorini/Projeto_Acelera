using System;

namespace SaciGestao.Models
{
    public class MovimentacaoFinanceira
    {
        public int Id { get; set; }
        public DateTime Data { get; set; } = DateTime.Today;
        public string Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // Entrada (Locação), Entrada (Venda), Saída
        public decimal Valor { get; set; }
        public string Status { get; set; } = "Confirmado";

        public string DataTexto => Data.ToString("dd/MM/yyyy");
        public string ValorTexto => Valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    }
}