using System;
using MVVM_Bonus.ViewModel;

namespace MVVM_Bonus.Services
{
    public interface INavigationService
    {
        IPageViewModel CurrentViewModel { get; }
        event Action<IPageViewModel> CurrentViewModelChanged;

        void NavigateTo(IPageViewModel viewModel);
        void NavigateTo<TViewModel>() where TViewModel : class, IPageViewModel;
    }
}
