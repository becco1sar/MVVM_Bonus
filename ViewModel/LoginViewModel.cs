using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Services;
using System;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Input;


namespace MVVM_Bonus.ViewModel
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        DataBaseService _databaseService;
        string _teamLeaderUserName;
        TeamLeaderModel _selectedTeamLeader;

        public LoginViewModel()
        {
            _databaseService = new DataBaseService();
        }

        public ICommand LoginCommand
        {
            get => new RelayCommand(x =>
            {
                if (IsTeamLeaderInDatabase(TeamLeaderUserName))
                {
                    if (SelectedTeamLeader.Role != null && SelectedTeamLeader.Role.ToUpper() == "HR")
                    {
                        Mediator.Notify(Constants.HR_DASHBOARD_VIEW, "");
                    }
                    else
                    {
                        Mediator.Notify(Constants.MAIN_MENU_VIEW, "");
                    }
                    Messenger.Default.Send(SelectedTeamLeader, Constants.MESSENGER_TEAMLEADER_IDENTIFICATION);
                }
                else
                {
                    MessageBox.Show("Teamleader not found try again");
                }
            });
        }
        public string TeamLeaderUserName
        {
            get => _teamLeaderUserName;
            set
            {
                _teamLeaderUserName = value;
                OnPropertyChanged();
            }
        }
        public TeamLeaderModel SelectedTeamLeader
        {
            get => _selectedTeamLeader;
            set
            {
                _selectedTeamLeader = value;
                OnPropertyChanged();
            }
        }
        private bool IsTeamLeaderInDatabase(string name)
        {
            try
            {
                var table = _databaseService.ExecuteQuery(Constants.SQL_GET_TEAMLEADER_QUERY, name);
                if (table.Rows.Count > 0)
                {
                    var row = table.Rows[0];
                    SelectedTeamLeader = new TeamLeaderModel()
                    {
                        Name = Convert.ToString(row[Constants.SQL_NAME_COLUMN_NAME]),
                        Id = Convert.ToInt32(row[Constants.SQL_ID_COLUMN_NAME]),
                        Role = row.Table.Columns.Contains(Constants.SQL_ROLE_COLUMN_NAME) && row[Constants.SQL_ROLE_COLUMN_NAME] != DBNull.Value 
                            ? Convert.ToString(row[Constants.SQL_ROLE_COLUMN_NAME]) 
                            : string.Empty
                    };
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }
    }
}