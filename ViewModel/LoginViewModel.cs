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
        ICommand _loginCommand;
        string _loginName;
        TeamLeaderModel _selectedTeamLeader;
        public static string User;
        ObservableCollection<TeamLeaderModel> teamLeaderModels;

        public LoginViewModel()
        {
        }

        public ICommand LoginCommand
        {
            get
            {
                if(_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(x =>
                    {
                        if (IsTeamLeaderInDatabase(LoginName))
                        {
                            //SelectedTeamLeader = teamLeaderModels.Where(x => x.Tl_Name == LoginName).First();
                            Mediator.Notify("GoToGeneral", "");
                            Messenger.Default.Send(SelectedTeamLeader, "getTeamleader");

                        }
                        else
                        {
                            MessageBox.Show("Teamleader not found try again");
                        }

                    });
                }
                    
           

                return _loginCommand;
            }
        }

        private void ShowName(string loginName)
        {
           MessageBox.Show(loginName);
        }

        public string LoginName
        {
            get
            {
                return _loginName;
            }
            set
            {
                _loginName = value;
                OnPropertyChanged(LoginName);
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
                var reader = DataBaseHandler.GetCommand($"SELECT * FROM Teamleaders WHERE username = '{name}'");
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        SelectedTeamLeader = new TeamLeaderModel()
                        {
                            Tl_Id = Convert.ToInt32(reader.GetValue(0)),
                            Tl_Name = reader.GetValue(1).ToString()

                        };
                    }
                    User = LoginName;

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
        public ObservableCollection<TeamLeaderModel> TeamLeaderModels
        {
            get
            {
                if (teamLeaderModels == null)
                    teamLeaderModels = new ObservableCollection<TeamLeaderModel>();
                return teamLeaderModels;
            }
            set
            {
                teamLeaderModels = value;
                OnPropertyChanged(nameof(TeamLeaderModels));
            }
        }
    }
}