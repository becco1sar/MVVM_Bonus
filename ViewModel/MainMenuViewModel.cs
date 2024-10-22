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
            Messenger.Default.Register<TeamLeaderModel>(this, "getTeamleader", action => _generatePersons(action));
            
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
        private void _generatePersons(TeamLeaderModel tl)
        {
            var reader = DataBaseHandler.GetCommand($"SELECT * FROM Workers WHERE Teamleader_id = {tl.Tl_Id} AND Active = 'Yes'");
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    ListPersons.Add(new Person
                    {
                        P_Id = Convert.ToInt32(reader.GetValue(0)),
                        P_Name = reader.GetValue(1).ToString(),
                        P_Language = reader.GetValue(3).ToString(),
                        P_Role = reader.GetValue(4).ToString(),
                        P_Contract = reader.GetValue(6).ToString(),
                        P_TeamLeaderID = Convert.ToInt32(reader.GetValue(5)),
                        P_BVID = Convert.ToInt32(reader.GetValue(7))});
                }
                _pathToContents();
            }
        }
        private void _pathToContents()
        {
            foreach(var person in ListPersons)
            {
                var reader = DataBaseHandler.GetCommand($"SELECT * FROM Contentcell WHERE Worker_Role = '{person.P_Role}' AND Worker_Language = '{person.P_Language}'");
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        person.P_PathToContentCells = reader.GetValue(3).ToString();
                    }
                }
            }                        
        }
        #endregion
    }
}
