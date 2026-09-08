using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    /// <summary>
    /// Shell view model. Owns the single page host and the navigation route table.
    /// </summary>
    /// <remarks>
    /// Pages are addressed by their route token (see <see cref="Constants"/>) rather than
    /// by their position in a list, and they are created on first navigation instead of at
    /// start-up. That keeps the login screen from waiting on database work belonging to
    /// pages the user may never open.
    /// </remarks>
    public class MainViewModel : ObservableObject
    {
        #region Fields
        private readonly Dictionary<string, Func<IPageViewModel>> _factories =
            new Dictionary<string, Func<IPageViewModel>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IPageViewModel> _pages =
            new Dictionary<string, IPageViewModel>(StringComparer.OrdinalIgnoreCase);

        private IPageViewModel _currentPageViewModel;
        private string _currentRoute;
        private TeamLeaderModel _teamLeader;
        #endregion

        #region Properties
        public IPageViewModel CurrentPageViewModel
        {
            get => _currentPageViewModel;
            private set
            {
                if (_currentPageViewModel == value)
                    return;

                _currentPageViewModel = value;
                OnPropertyChanged(nameof(CurrentPageViewModel));
            }
        }

        /// <summary>Route token of the page currently on screen.</summary>
        public string CurrentRoute
        {
            get => _currentRoute;
            private set
            {
                if (_currentRoute == value)
                    return;

                _currentRoute = value;
                OnPropertyChanged(nameof(CurrentRoute));
                OnPropertyChanged(nameof(IsSignedIn));
            }
        }

        public TeamLeaderModel TeamLeader
        {
            get => _teamLeader;
            private set
            {
                _teamLeader = value;
                OnPropertyChanged(nameof(TeamLeader));
                OnPropertyChanged(nameof(SignedInAs));
            }
        }

        /// <summary>Name to show in the title bar, empty while on the login screen.</summary>
        public string SignedInAs => TeamLeader?.Name ?? string.Empty;

        public bool IsSignedIn =>
            !string.Equals(CurrentRoute, Constants.LOGIN_VIEW, StringComparison.OrdinalIgnoreCase);
        #endregion

        #region Constructors
        public MainViewModel()
        {
            Messenger.Default.Register<TeamLeaderModel>(
                this,
                Constants.MESSENGER_TEAMLEADER_IDENTIFICATION,
                teamLeader => TeamLeader = teamLeader);

            // Every page is reachable by exactly one token, and every token routes.
            AddRoute(Constants.LOGIN_VIEW, () => new LoginViewModel());
            AddRoute(Constants.MAIN_MENU_VIEW, () => new MainMenuViewModel());
            AddRoute(Constants.GET_BONUS_VIEW, () => new BonusViewModel());
            AddRoute(Constants.INSERT_BONUS_VIEW, () => new InsertViewModel());
            AddRoute(Constants.PRINTING_VIEW, () => new PrintingViewModel());
            AddRoute(Constants.HR_DASHBOARD_VIEW, () => new HrDashboardViewModel());
            AddRoute(Constants.STAFF_MANAGEMENT_VIEW, () => new StaffManagementViewModel());
            AddRoute(Constants.BONUS_CONFIG_VIEW, () => new BonusConfigViewModel());

            Mediator.Subscribe(Constants.SIGN_OUT, _ => SignOut());

            NavigateTo(Constants.LOGIN_VIEW);
        }
        #endregion

        #region Methods
        private void AddRoute(string token, Func<IPageViewModel> factory)
        {
            _factories[token] = factory;
            Mediator.Subscribe(token, _ => NavigateTo(token));
        }

        /// <summary>
        /// Shows the page registered under <paramref name="token"/>, creating it if this is
        /// the first time it has been requested.
        /// </summary>
        private void NavigateTo(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (!_pages.TryGetValue(token, out IPageViewModel page))
            {
                if (!_factories.TryGetValue(token, out Func<IPageViewModel> factory))
                    return;

                page = factory();
                _pages[token] = page;
            }

            CurrentRoute = token;
            CurrentPageViewModel = page;
        }

        /// <summary>
        /// Returns to the login screen and drops every page built for the previous session,
        /// so the next user never sees the last one's team or bonus data.
        /// </summary>
        private void SignOut()
        {
            foreach (KeyValuePair<string, IPageViewModel> entry in _pages.ToList())
            {
                if (string.Equals(entry.Key, Constants.LOGIN_VIEW, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Pages subscribe to the Messenger in their constructors; without this the
                // discarded instances keep receiving messages meant for their replacements.
                Messenger.Default.Unregister(entry.Value);
                _pages.Remove(entry.Key);
            }

            TeamLeader = null;

            NavigateTo(Constants.LOGIN_VIEW);
            (CurrentPageViewModel as LoginViewModel)?.Reset();
        }
        #endregion
    }
}
