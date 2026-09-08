using System;
using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Services;
using System.Windows;
using System.Windows.Input;

namespace MVVM_Bonus.ViewModel
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private readonly DataBaseService _databaseService;
        private string _teamLeaderUserName;
        private string _errorMessage;
        private TeamLeaderModel _selectedTeamLeader;
        private ICommand _loginCommand;
        #endregion

        #region Constructors
        public LoginViewModel()
        {
            _databaseService = new DataBaseService();
        }
        #endregion

        #region Properties
        public string TeamLeaderUserName
        {
            get => _teamLeaderUserName;
            set
            {
                _teamLeaderUserName = value;
                OnPropertyChanged();

                // Typing is the user's answer to the last failure; clear it as they retype.
                ErrorMessage = null;
            }
        }

        /// <summary>
        /// Sign-in failure text shown inline under the field. Empty when there is nothing
        /// to report; a message box for a wrong username is heavier than the mistake.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage == value)
                    return;

                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public TeamLeaderModel SelectedTeamLeader
        {
            get => _selectedTeamLeader;
            private set
            {
                _selectedTeamLeader = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(
                        param => SignIn(),
                        param => !string.IsNullOrWhiteSpace(TeamLeaderUserName));
                }
                return _loginCommand;
            }
        }
        #endregion

        #region Methods
        /// <summary>Clears the form so the next user starts from a blank screen.</summary>
        public void Reset()
        {
            _teamLeaderUserName = string.Empty;
            OnPropertyChanged(nameof(TeamLeaderUserName));
            SelectedTeamLeader = null;
            ErrorMessage = null;
        }

        private void SignIn()
        {
            if (!TryFindTeamLeader(TeamLeaderUserName))
                return;

            bool isHr = string.Equals(
                SelectedTeamLeader.Role,
                Constants.ROLE_HR,
                StringComparison.OrdinalIgnoreCase);

            Mediator.Notify(isHr ? Constants.HR_DASHBOARD_VIEW : Constants.MAIN_MENU_VIEW, "");

            // Sent after navigation so the destination page exists to receive it.
            Messenger.Default.Send(SelectedTeamLeader, Constants.MESSENGER_TEAMLEADER_IDENTIFICATION);
        }

        private bool TryFindTeamLeader(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                ErrorMessage = "Enter your team leader name to sign in.";
                return false;
            }

            try
            {
                var table = _databaseService.ExecuteQuery(
                    Constants.SQL_GET_TEAMLEADER_QUERY,
                    userName.Trim());

                if (table.Rows.Count == 0)
                {
                    ErrorMessage = $"No team leader called \"{userName.Trim()}\". Check the spelling and try again.";
                    return false;
                }

                var row = table.Rows[0];

                SelectedTeamLeader = new TeamLeaderModel
                {
                    Name = Convert.ToString(row[Constants.SQL_NAME_COLUMN_NAME]),
                    Id = Convert.ToInt32(row[Constants.SQL_ID_COLUMN_NAME]),
                    Role = row.Table.Columns.Contains(Constants.SQL_ROLE_COLUMN_NAME)
                           && row[Constants.SQL_ROLE_COLUMN_NAME] != DBNull.Value
                        ? Convert.ToString(row[Constants.SQL_ROLE_COLUMN_NAME])
                        : string.Empty
                };

                ErrorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                // The database lives on a network share, so "cannot reach it" is the common
                // case and worth naming separately from a bad username.
                ErrorMessage = "Could not reach the bonus database. Check your network connection.";

                MessageBox.Show(
                    $"Could not reach the bonus database.\n\n{ex.Message}",
                    "Connection problem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }
        #endregion
    }
}
