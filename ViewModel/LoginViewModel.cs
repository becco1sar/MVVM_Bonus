using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Services;
using MVVM_Bonus.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace MVVM_Bonus.ViewModel
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {

        string _teamLeaderUserName;
        TeamLeaderModel _selectedTeamLeader;

        public ICommand LoginCommand
        {
            get => new RelayCommand(x =>
            {
                if (IsTeamLeaderInDatabase(TeamLeaderUserName))
                {
                    //SelectedTeamLeader = teamLeaderModels.Where(x => x.Tl_Name == LoginName).First();
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
                var reader = DataBaseHandler.GetCommand($"{Constants.TEAMLEADER_SQL_QUERY} = {name}'");
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        SelectedTeamLeader = new TeamLeaderModel()
                        {
                            Id = reader.GetOrdinal(Constants.TEAMLEADER_SQL_ID_COLUMN).ToString(),
                            Name= reader.GetOrdinal(Constants.TEAMLEADER_SQL_NAME_COLUMN).ToString()
                        };
                    }         
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