using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class PrintingViewModel : ObservableObject, IPageViewModel
    {
        private BonusModel _myBonusPoints;
        private ObservableCollection<BonusDetailModel> _myBonusDetails;
        private ObservableCollection<string> _listDetailPoints;
        private Person _conceredPerson;

        static string currentDir = Directory.GetCurrentDirectory().Remove(2);
        private string _period;
        private string _workerName;
        private decimal _total;
        private bool printAndClose;
        

        private ICommand _btnPrintPreview;
        private ICommand _btnBackButton;
        private IPageViewModel _previousViewModel;
        public PrintingViewModel()
        {
            Messenger.Default.Register<Person>(this, "selectedPerson", person => SavePerson(person));
            Messenger.Default.Register<BonusModel>(this, "PrintView", bonusPoints => GenerateBonusPoints(bonusPoints));
            Messenger.Default.Register<IPageViewModel>(this, "PreviousView", view => StorePreviousView(view));
        }

        private string StorePreviousView(IPageViewModel view)
        {
            string back = Constants.MAIN_MENU_VIEW;
            if (view.GetType() == typeof(InsertViewModel))
                back = "GetPersonsBonusView";
            return back;

        }

        private void SavePerson(Person person)
        {
            ConceredPerson = person;
            ListDetailPoints = DetailPoints.GenerateDetailPoints(@$"O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0{ConceredPerson.PathToContentCells}");
        }

        public BonusModel MyBonusPoints 
        {
            get
            {
                if(_myBonusPoints == null)
                    _myBonusPoints = new BonusModel();

                return _myBonusPoints;
            }
            set
            {
                _myBonusPoints = value;
                OnPropertyChanged(nameof(MyBonusPoints));
            }
        }
        public ObservableCollection<BonusDetailModel> MyBonusDetails 
        {
            get
            {
                if (_myBonusDetails == null)
                    _myBonusDetails = new ObservableCollection<BonusDetailModel>();
    
               return _myBonusDetails;
            }
            set
            {
                _myBonusDetails = value;
                OnPropertyChanged(nameof(MyBonusDetails));
            }
        }

        public string Period 
        {
            get
            {
               return _period;
            }
            set
            {
                _period = value;
                OnPropertyChanged(nameof(Period));
            } 
        }
        public string WorkerName 
        {
            get
            {
                return _workerName;
            }
            set
            {
                _workerName = value;
                OnPropertyChanged(nameof(WorkerName));
            }        }

        public ObservableCollection<string> ListDetailPoints 
        {
            get
            {

                if (_listDetailPoints == null)
                    _listDetailPoints = new();
                return _listDetailPoints;
            }
            set
            {
                _listDetailPoints = value;
                OnPropertyChanged(nameof(DetailPoints));
            }
        }
        public Person ConceredPerson 
        {
            get
            {
                if (_conceredPerson == null)
                    _conceredPerson = new();
                return _conceredPerson;
            }
            set
            {
                _conceredPerson = value;
                OnPropertyChanged(nameof(ConceredPerson));
            }
        }

        public ICommand BtnPrintPreview 
        {
            get
            {
                if(_btnPrintPreview == null)
                {
                    _btnPrintPreview = new RelayCommand(param => ShowPrintPreview((Visual)param));
                }
                return _btnPrintPreview;
            }      
        }
        public ICommand BtnBackButton 
        {
            get
            {             
                return _btnBackButton = CommonViewModel.Return; 
            }
        }
        private void Goto(string v)
        {
        
            Mediator.Notify(v, "");
        }

        public IPageViewModel PreviousViewModel { get => _previousViewModel; set => _previousViewModel = value; }
        public decimal Total
        {
            get
            {
                return _total;
            }
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }

        private void ShowPrintPreview(Visual x)
        {
            PrintDialog pd = new();
            Grid g = (Grid)x;
            pd.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
            pd.PrintVisual(g, "Printing");            
        }

        private void GenerateBonusPoints(BonusModel bvm)
        {            
            MyBonusDetails = new ObservableCollection<BonusDetailModel>();
            MyBonusPoints = bvm;
     
            WorkerName = bvm.WorkerName;
            Period = bvm.Period;
            Total = bvm.Total;            

            try
            {
                for (int i = 0; i < ListDetailPoints.Count(); i++)
                {
                    MyBonusDetails.Add(new BonusDetailModel { DetailPoint = ListDetailPoints[i], Amount = bvm.Amounts[i], Comment = bvm.Comments[i] });
                }
            }catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }
}
