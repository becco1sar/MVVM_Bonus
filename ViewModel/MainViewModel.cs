using MVVM_Bonus.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Bonus.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region Fields
        IPageViewModel _currentPageViewModel;
        List<IPageViewModel> _listPageViewModel;
        string teamLeaderName;
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

        public string TeamLeaderName { get => teamLeaderName; set => teamLeaderName = value; }
        #endregion
        #region Constructors
        public MainViewModel()
        {
            ListPageViewModel.Add(new LoginViewModel());
            ListPageViewModel.Add(new MainMenuViewModel());
            ListPageViewModel.Add(new BonusViewModel());
            ListPageViewModel.Add(new InsertViewModel());
            ListPageViewModel.Add(new InsertViewVeloViewModel());
            ListPageViewModel.Add(new PrintingViewModel());

            CurrentPageViewModel = ListPageViewModel[0];

            Mediator.Subscribe("LoginView", LogOut);
            Mediator.Subscribe("GoToGeneral", ShowGeneral);
            Mediator.Subscribe("GetPersonsBonusView", ShowBonus);
            Mediator.Subscribe("InsertPersonsBonusView", ShowInsertBonus);
            Mediator.Subscribe("GoToPrintingView", ShowPrinting);

            Messenger.Default.Register<TeamLeaderModel>(this, "getTeamleader", teamLeader => OnSelectedTeamLeader(teamLeader));
        }

        private void OnSelectedTeamLeader(TeamLeaderModel obj)
        {
            teamLeaderName = obj.Tl_Name;
        }
        #endregion
        #region Methods
        private void ShowPrinting(object obj)
        {
            ChangeViewModel(ListPageViewModel[5]);
        }

        private void ShowInsertBonus(object obj)
        {
            if(TeamLeaderName.ToLower() == "semuna")
                ChangeViewModel(ListPageViewModel[4]);
            else
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
