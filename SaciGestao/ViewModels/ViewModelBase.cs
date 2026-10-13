using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaciGestao.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }

        // Troca o valor e avisa a tela. Retorna true se o valor realmente mudou.
        protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nome = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor)) return false;

            campo = valor;
            OnPropertyChanged(nome);
            return true;
        }
    }
}