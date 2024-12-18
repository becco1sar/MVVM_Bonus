using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Logic;
using MVVM_Bonus.Services;
using System;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace MVVM_Bonus.ViewModel
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        DataBaseService _databaseService;
        string _teamLeaderUserName;
        TeamLeaderModel _selectedTeamLeader;
        public ICommand LoginCommand
        {
            get => new RelayCommand(x =>
            {
                if (IsTeamLeaderInDatabase(TeamLeaderUserName))
                {
                    Mediator.Notify(Constants.MAIN_MENU_VIEW, "");
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
            get
            {
                return _teamLeaderUserName;
            }
            set
            {
                _teamLeaderUserName = value;
                OnPropertyChanged(TeamLeaderUserName);
            }
        }
        public TeamLeaderModel SelectedTeamLeader
        {
            get
            {
                return _selectedTeamLeader;
            }
            set
            {
                _selectedTeamLeader = value;
                OnPropertyChanged(nameof(SelectedTeamLeader));
            }
        }
        private bool IsTeamLeaderInDatabase(string name)
        {
            try
            {
                var reader = _databaseService.GetCommand($"{Constants.SQL_GET_TEAMLEADER_QUERY} = '{name}'");
                if (reader.HasRows)
                {
                    var teamLeaderId = reader.GetOrdinal(Constants.SQL_ID_COLUMN_NAME);
                    var teamLeaderName = reader.GetOrdinal(Constants.SQL_NAME_COLUMN_NAME);
                    while (reader.Read())
                    {
                        SelectedTeamLeader = new TeamLeaderModel()
                        {
                            Name = reader.GetString(teamLeaderName),
                            Id = reader.GetInt32(teamLeaderId)
                        };
                    };       
                    return true;
                }
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (OleDbException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
 
            return false;
        }
    }
}