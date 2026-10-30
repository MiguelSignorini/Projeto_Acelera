using System.Collections.ObjectModel;
using System.Linq;
using SaciGestao.Models;
using SaciGestao.Service;

namespace SaciGestao.ViewModels
{
    public class FinanceiroViewModel : ViewModelBase
    {
        private ObservableCollection<MovimentacaoFinanceira> _movimentacoes = new();
        public ObservableCollection<MovimentacaoFinanceira> Movimentacoes
        {
            get => _movimentacoes;
            set => SetProperty(ref _movimentacoes, value);
        }

        private decimal _receitaTotal;
        public decimal ReceitaTotal
        {
            get => _receitaTotal;
            set => SetProperty(ref _receitaTotal, value);
        }

        private decimal _totalEntradas;
        public decimal TotalEntradas
        {
            get => _totalEntradas;
            set => SetProperty(ref _totalEntradas, value);
        }

        private decimal _totalSaidas;
        public decimal TotalSaidas
        {
            get => _totalSaidas;
            set => SetProperty(ref _totalSaidas, value);
        }

        private decimal _totalLocacao;
        public decimal TotalLocacao
        {
            get => _totalLocacao;
            set => SetProperty(ref _totalLocacao, value);
        }

        private decimal _totalVenda;
        public decimal TotalVenda
        {
            get => _totalVenda;
            set => SetProperty(ref _totalVenda, value);
        }

        public int QuantidadeMovimentacoes => Movimentacoes.Count;

        public FinanceiroViewModel()
        {
            CarregarDadosFinanceiros();
        }

        public void CarregarDadosFinanceiros()
        {
            Movimentacoes.Clear();
            var listaMovimentacoes = FinanceiroService.Instancia.ObterMovimentacoes();

            decimal receitaCalculada = 0;

            foreach (var mov in listaMovimentacoes)
            {
                Movimentacoes.Add(mov);
                receitaCalculada += mov.Valor;
            }

            TotalLocacao = listaMovimentacoes.Where(m => m.Tipo.Contains("Locação")).Sum(m => m.Valor);
            TotalVenda = listaMovimentacoes.Where(m => m.Tipo.Contains("Venda")).Sum(m => m.Valor);
            OnPropertyChanged(nameof(QuantidadeMovimentacoes));

            ReceitaTotal = receitaCalculada;
            TotalEntradas = receitaCalculada;
            TotalSaidas = 0;
        }
    }
}