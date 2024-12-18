using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using MVVM_Bonus.Services;
using System;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Input;

namespace MVVM_Bonus.ViewModel
{
    public class BonusViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        string _search;
        
        Person _currentPerson;
        BonusModel _BonusPoint;
        BonusModel _selectedBonusModel;
        DataBaseService _databaseService;
        ObservableCollection<BonusModel> _listBonusModels;
        ObservableCollection<BonusModel> _filteredListBonusModels;

        ICommand _backButtonCommand;
        ICommand _btnDetailViewCommand;
        ICommand _btnPrintCommand;
        #endregion
        #region Properties
        public string Search
        {
            get
            {
                FilteredListBonusModels = ListBonusModels;
                if (_search != null)
                {
                    _filterByDate(_search);
                }

                return _search;
            }
            set
            {
                _search = value;
                OnPropertyChanged(nameof(Search));
            }
        }
        public Person CurrentPerson
        {
            get
            {
                return _currentPerson;
            }
            set
            {
                _currentPerson = value;
                OnPropertyChanged(nameof(CurrentPerson));
            }
        }
        public BonusModel BonusPoint 
        {
            get
            {
                return _BonusPoint;
            }
            set
            {
                _BonusPoint = value;
                OnPropertyChanged(nameof(BonusPoint));
            }
        }
        public ObservableCollection<BonusModel> ListBonusModels 
        {
            get
            {
                if (_listBonusModels == null)
                    _listBonusModels = new ObservableCollection<BonusModel>();
                return _listBonusModels;
            }
            set
            {
                _listBonusModels = value;
                OnPropertyChanged(nameof(ListBonusModels));
            }
        }
        public ObservableCollection<BonusModel> FilteredListBonusModels
        {
            get
            {
                if (_filteredListBonusModels == null)
                    _filteredListBonusModels = new ObservableCollection<BonusModel>();
                return _filteredListBonusModels;
            }
            set
            {
                _filteredListBonusModels = value;
                OnPropertyChanged(nameof(FilteredListBonusModels));
            }
        }
        public ICommand BackButtonCommand
        {
            get
            {
                if(_backButtonCommand == null)
                {
                    _backButtonCommand = new RelayCommand(
                        param => _backToGeneral()
                        ) ;
                }
                return _backButtonCommand;
            }
        }

        public ICommand BtnDetailViewCommand 
        {
            get
            {
                if(_btnDetailViewCommand == null)
                {
                    _btnDetailViewCommand = new RelayCommand(e => 
                    { 
                        CommonViewModel.PreviousViewModel = new BonusViewModel(); 
                        Mediator.Notify("GoToPrintingView", ""); 
                        Messenger.Default.Send(CurrentPerson, "selectedPerson"); 
                        Messenger.Default.Send(SelectedBonusModel, "PrintView"); 
                        Messenger.Default.Send(false, "PrintAndClose"); 
                    });

                }
                return _btnDetailViewCommand;
            }
         
        }

        public ICommand BtnPrintCommand 
        {
            get
            {
                if(_btnPrintCommand == null)
                {
                    _btnPrintCommand = new RelayCommand(e =>
                    {
                        CommonViewModel.PreviousViewModel = new BonusViewModel();
                        Mediator.Notify("GoToPrintingView", "");
                        Messenger.Default.Send(CurrentPerson, "selectedPerson");
                        Messenger.Default.Send(SelectedBonusModel, "PrintView");
                        Messenger.Default.Send(true, "PrintAndClose");
                    });
                }
               return _btnPrintCommand;
            }

        }

        public BonusModel SelectedBonusModel 
        {
            get
            { return _selectedBonusModel; }
            set
            {
                _selectedBonusModel = value;
                OnPropertyChanged(nameof(SelectedBonusModel));
            }
        }
        #endregion
        #region Constructors
        public BonusViewModel()
        {
            Messenger.Default.Register<Person>(this, "GetView", callback => _generateListOfBonus(callback));
            _databaseService = new DataBaseService();
        }
        #endregion
        #region Methods
        private void _backToGeneral()
        {
            ListBonusModels.Clear();    
            Mediator.Notify(Constants.MAIN_MENU_VIEW, ""); 
        }
        private void _filterByDate(string dt)
        {

            FilteredListBonusModels = ListBonusModels;
            if (ListBonusModels.Where(x => x.Period.Contains(dt)).Count() != 0)
            {
                FilteredListBonusModels = new ObservableCollection<BonusModel>();
                foreach (var item in ListBonusModels.Where(x => x.Period.Contains(dt)))
                    FilteredListBonusModels.Add(item);
            }
        }

        private void _generateListOfBonus(Person person)
        {
            CurrentPerson = person;
            OleDbDataReader reader = _databaseService.GetCommand($"SELECT * FROM Bonus_General WHERE Worker_id = {CurrentPerson.P_Id}");
            BonusModel thisBonusModel;
            if (reader.HasRows)
                while (reader.Read())
                {
                    ListBonusModels.Add(new BonusModel { WorkerName = CurrentPerson.P_Name, Total = Convert.ToDecimal(reader.GetValue(3)), Period = reader.GetValue(2).ToString() });
                    for (int i = 0; i < 10; i++)
                    {
                        thisBonusModel = ListBonusModels[ListBonusModels.Count() - 1];
                        thisBonusModel.Amounts.Add(Convert.ToDecimal(reader.GetValue(i + 4)));
                        thisBonusModel.Comments.Add(reader.GetValue(i + 14).ToString());
                    }
                }

            FilteredListBonusModels = ListBonusModels;
        }
        #endregion
    }
}
