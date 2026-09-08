using System;
using System.Collections.Generic;
using System.Linq;
using MVVM_Bonus.ViewModel;

namespace MVVM_Bonus.Services
{
    public class NavigationService : INavigationService
    {
        private readonly List<IPageViewModel> _pageViewModels;
        private IPageViewModel _currentViewModel;

        public event Action<IPageViewModel> CurrentViewModelChanged;

        public IPageViewModel CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    CurrentViewModelChanged?.Invoke(_currentViewModel);
                }
            }
        }

        public NavigationService(IEnumerable<IPageViewModel> pageViewModels)
        {
            _pageViewModels = pageViewModels?.ToList() ?? new List<IPageViewModel>();
            _currentViewModel = _pageViewModels.FirstOrDefault();
        }

        public void NavigateTo(IPageViewModel viewModel)
        {
            if (viewModel != null)
            {
                if (!_pageViewModels.Contains(viewModel))
                {
                    _pageViewModels.Add(viewModel);
                }
                CurrentViewModel = viewModel;
            }
        }

        public void NavigateTo<TViewModel>() where TViewModel : class, IPageViewModel
        {
            var targetVM = _pageViewModels.OfType<TViewModel>().FirstOrDefault();
            if (targetVM != null)
            {
                CurrentViewModel = targetVM;
            }
        }
    }
}
