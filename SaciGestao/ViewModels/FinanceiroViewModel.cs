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

            ReceitaTotal = receitaCalculada;
            TotalEntradas = receitaCalculada;
            TotalSaidas = 0;
        }
    }
}