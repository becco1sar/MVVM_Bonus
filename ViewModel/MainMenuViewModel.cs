using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Data;
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
            get => _search;
            set
            {
                if (_search != value)
                {
                    _search = value;
                    OnPropertyChanged();
                    ApplyFilter(_search);
                }
            }
        }
        public Person SelectedWorker
        {
            get => _selectedWorker;
            set
            {
                _selectedWorker = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Person> FilteredListPerson
        {
            get
            {
                if (_filteredListPerson == null)
                    _filteredListPerson = new ObservableCollection<Person>();
                return _filteredListPerson;
            }
            set
            {
                _filteredListPerson = value;
                OnPropertyChanged();
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
                    Mediator.Notify("InsertPersonsBonusView", SelectedWorker);
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
        private void ApplyFilter(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                FilteredListPerson = new ObservableCollection<Person>(ListPersons);
            }
            else
            {
                var matches = ListPersons
                    .Where(x => x.Name != null && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredListPerson = new ObservableCollection<Person>(matches);
            }
        }

        private void ShowAll()
        {
            ApplyFilter(null);
        }
        private void GenerateWorkers(TeamLeaderModel tl)
        {
            if (tl == null) return;
            try
            {
                ListPersons.Clear();
                var table = _databaseService.ExecuteQuery(Constants.SQL_GET_WORKER_QUERY, tl.Id, Constants.ACTIVE_ROWS);
                foreach (DataRow row in table.Rows)
                {
                    Person person = new Person
                    {
                        Id = Convert.ToInt32(row[Constants.SQL_ID_COLUMN_NAME]),
                        Name = Convert.ToString(row[Constants.SQL_NAME_COLUMN_NAME]),
                        Language = Convert.ToString(row[Constants.SQL_LANGUAGE_COLUMN_NAME]),
                        Role = Convert.ToString(row[Constants.SQL_ROLE_COLUMN_NAME]),
                        Contract = Convert.ToString(row[Constants.SQL_CONTRACT_ID_COLUMN_NAME]),
                        TeamLeaderId = tl.Id,
                        BvId = Convert.ToInt32(row[Constants.SQL_BV_ID_COLUMN_NAME])
                    };
                    person.PathToContentCells = GetAttentionPoint(person);
                    ListPersons.Add(person);
                }
                ShowAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading workers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string GetAttentionPoint(Person person)
        {
            try
            {
                string query = "SELECT [Path] FROM Contentcell WHERE Worker_Role = ? AND Worker_Language = ?";
                var table = _databaseService.ExecuteQuery(query, person.Role, person.Language);
                if (table.Rows.Count > 0)
                {
                    return Convert.ToString(table.Rows[0]["Path"]);
                }
            }
            catch
            {
                // Fallback
            }
            return string.Empty;                   
        }
        #endregion
    }
}