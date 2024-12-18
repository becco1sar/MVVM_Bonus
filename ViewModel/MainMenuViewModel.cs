using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using MVVM_Bonus.ViewModel;
using MVVM_Bonus.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using System.Linq;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class MainMenuViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        string _search;
        Person _selectedWorker;
        ObservableCollection<Person> _listPersons;
        ObservableCollection<Person> _filteredListPerson;
        ICommand _getPersonsBonus;
        ICommand _insertPersonsBonus;
        DataBaseService _databaseService;
        #endregion
        #region Properties
        public string Search
        {
            get
            {
                if (_search != null)
                    FilteredSearch(_search);
                else
                    ShowAll();
                return _search;
            }
            set
            {
                _search = value;
                OnPropertyChanged(nameof(Search));
            }
        }
        public Person SelectedWorker
        {
            get
            {
                return _selectedWorker;
            }
            set
            {
                _selectedWorker = value;
                OnPropertyChanged(nameof(SelectedWorker));
            }
        }
        public ObservableCollection<Person> ListPersons
        {
            get
            {
                if (_listPersons == null)
                    _listPersons = new ObservableCollection<Person>();
                return _listPersons;
            }
            set
            {
                _listPersons = value;
                OnPropertyChanged(nameof(ListPersons));
            }
        }
        public ObservableCollection<Person> FilteredListPerson
        {
            get
            {
                if (_filteredListPerson == null)
                    _filteredListPerson = new();
                return _filteredListPerson;
            }
            set
            {
                _filteredListPerson = value;
                OnPropertyChanged(nameof(FilteredListPerson));
            }
        }
        public ICommand GetPersonsBonus
        {
            get
            {

                return _getPersonsBonus ?? (new RelayCommand(x =>
                {
                    if (SelectedWorker == null)
                    {
                        MessageBox.Show("Please select a worker first !");
                        return;
                    }

                    Mediator.Notify("GetPersonsBonusView", "");
                    Messenger.Default.Send(SelectedWorker, "GetView");

                }));
            }
        }
        public ICommand InsertPersonsBonus
        {
            get
            {

                return _insertPersonsBonus ?? (new RelayCommand(x =>
                {
                    if (SelectedWorker == null)
                    {
                        MessageBox.Show("Please select a worker first !");
                        return;
                    }
                    Mediator.Notify("InsertPersonsBonusView", "");
                    Messenger.Default.Send(SelectedWorker, "InsertView");
                }));
            }

        }
        #endregion
        #region Constructors
        public MainMenuViewModel()
        {
            Messenger.Default.Register<TeamLeaderModel>(this, Constants.MESSENGER_TEAMLEADER_IDENTIFICATION, action => GenerateWorkers(action));   
            _databaseService = new DataBaseService();
        }
        #endregion
        #region Methods
        private void FilteredSearch(string search)
        {
            FilteredListPerson = ListPersons;
            if (ListPersons.Where(x => x.P_Name.Contains(search.ToUpper())).Count() != 0)
            {
                FilteredListPerson = new ObservableCollection<Person>();
                foreach (var person in ListPersons.Where(x => x.P_Name.Contains(search.ToUpper())))
                    FilteredListPerson.Add(person);
            }
        }
        private void ShowAll()
        {
            FilteredListPerson = ListPersons;
        }
        private void GenerateWorkers(TeamLeaderModel tl)
        {
            var teamLeaderId = tl.Id;
            //TODO Try except if null it exexucutes
            var reader = _databaseService.GetCommand($"{Constants.SQL_GET_WORKER_QUERY} = {teamLeaderId} AND Active = '{Constants.ACTIVE_ROWS}'");
            if (reader.HasRows)
            {
                int id = reader.GetOrdinal(Constants.SQL_ID_COLUMN_NAME);
                int name = reader.GetOrdinal(Constants.SQL_NAME_COLUMN_NAME);
                int lang = reader.GetOrdinal(Constants.SQL_LANGUAGE_COLUMN_NAME);
                int role = reader.GetOrdinal(Constants.SQL_ROLE_COLUMN_NAME);    
                int contractId = reader.GetOrdinal(Constants.SQL_CONTRACT_ID_COLUMN_NAME);
                int bvId = reader.GetOrdinal(Constants.SQL_BV_ID_COLUMN_NAME);
                
                while (reader.Read())
                {
                    Person person = new Person();
                    person.P_Id = reader.GetInt32(id);
                    person.P_Name = reader.GetString(name);
                    person.P_Language = reader.GetString(lang);
                    person.P_Role = reader.GetString(role);
                    person.P_Contract = reader.GetString(contractId);
                    person.P_TeamLeaderID = teamLeaderId;
                    person.P_BVID = reader.GetInt32(bvId);
                    person.P_PathToContentCells = GetAttentionPoint(person);
                    ListPersons.Add(person);
                }
         
            }
        }
        
        private string GetAttentionPoint(Person person)
        {

            var reader = _databaseService.GetCommand($"SELECT * FROM Contentcell WHERE Worker_Role = '{person.P_Role}' AND Worker_Language = '{person.P_Language}'");
            int pathToContent = reader.GetOrdinal("Path");
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    return reader.GetString(pathToContent);
                }
            }
            return "";                   
        }
        #endregion
    }
}