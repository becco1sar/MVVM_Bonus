using MVVM_Bonus.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using System.Text;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region Fields
        IPageViewModel _currentPageViewModel;
        List<IPageViewModel> _listPageViewModel;
        TeamLeaderModel _teamLeader;
        #endregion
        #region Properties

        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                _currentPageViewModel = value;
                OnPropertyChanged(nameof(CurrentPageViewModel));
            }
        }
        public List<IPageViewModel> ListPageViewModel
        {
            get
            {
                if (_listPageViewModel == null)
                    _listPageViewModel = new List<IPageViewModel>();
                return _listPageViewModel;
            }
        }

        public TeamLeaderModel TeamLeader { get => _teamLeader; set => _teamLeader = value; }
        #endregion
        #region Constructors
        public MainViewModel()
        {
            Messenger.Default.Register<TeamLeaderModel>(this, Constants.MESSENGER_TEAMLEADER_IDENTIFICATION, x => TeamLeader = x);
            ListPageViewModel.Add(new LoginViewModel());
            ListPageViewModel.Add(new MainMenuViewModel());
            ListPageViewModel.Add(new BonusViewModel());
            ListPageViewModel.Add(new InsertViewModel());
            ListPageViewModel.Add(new PrintingViewModel());
            ListPageViewModel.Add(new HrDashboardViewModel());

            CurrentPageViewModel = ListPageViewModel[0];

            Mediator.Subscribe("LoginView", LogOut);
            Mediator.Subscribe(Constants.MAIN_MENU_VIEW, ShowGeneral);
            Mediator.Subscribe(Constants.HR_DASHBOARD_VIEW, ShowHrDashboard);
            Mediator.Subscribe("GetPersonsBonusView", ShowBonus);
            Mediator.Subscribe("InsertPersonsBonusView", ShowInsertBonus);
            Mediator.Subscribe("GoToPrintingView", ShowPrinting);

        }

        #endregion
        #region Methods
        private void ShowPrinting(object obj)
        {
            ChangeViewModel(ListPageViewModel[4]);
        }

        private void ShowInsertBonus(object obj)
        {
            ChangeViewModel(ListPageViewModel[3]);
        }

        private void ShowBonus(object obj)
        {
            ChangeViewModel(_listPageViewModel[2]);
        }

        private void LogOut(object obj)
        {
            throw new NotImplementedException();
        }

        private void ShowGeneral(object obj)
        {
            ChangeViewModel(_listPageViewModel[1]);
        }

        private void ShowHrDashboard(object obj)
        {
            ChangeViewModel(_listPageViewModel[5]);
        }

        private void ChangeViewModel(IPageViewModel pageViewModel)
        {
            if (pageViewModel == null)
                ListPageViewModel.Add(pageViewModel);

            CurrentPageViewModel = pageViewModel;
            ListPageViewModel.First(vm => vm == pageViewModel);
        }
        #endregion
    }
}
