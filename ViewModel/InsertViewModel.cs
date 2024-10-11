using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MVVM_Bonus.ViewModel
{
    public class InsertViewModel : ObservableObject, IPageViewModel, ICommonCommandsModel
    {
        #region Fields
        private BonusValuesModel _selectedBonusPoint;
        private ObservableCollection<BonusValuesModel> _myBonusValuePoints;
        private ObservableCollection<BonusModel> _myBonusPoints;
        private OleDbDataReader _oleDbDReader;
        private Person _worker;

        private bool _isFirstBonusSelected;
        private DateTime _datePickerText;
        private decimal _additionalBonus;
        private string _additionalComment;
        private decimal _currentBonusAmount;
        int _total;

        ICommand _btnCommandClicked;
        ICommand _btnResetCommandClicked;
        ICommand _backButtonCommand;
        #endregion

        #region Properties
        public BonusValuesModel SelectedBonusPoint
        {
            get
            {
                return _selectedBonusPoint;
            }
            set
            {
                AddRemoveAmount();
                OnPropertyChanged(nameof(SelectedBonusPoint));
            }
        }
        public ObservableCollection<BonusValuesModel> MyBonusValuePoints
        {
            get
            {
                if (_myBonusValuePoints == null)
                {
                    MyBonusValuePoints = new ObservableCollection<BonusValuesModel>();
                }

                return _myBonusValuePoints;
            }
            set
            {
                _myBonusValuePoints = value;
                OnPropertyChanged(nameof(MyBonusValuePoints));
            }
        }
        public ObservableCollection<BonusModel> MyBonusPoints 
        {
            get
            {
                if (_myBonusPoints == null)
                    _myBonusPoints = new ObservableCollection<BonusModel>();
                return _myBonusPoints;
            }
            set
            {
                _myBonusPoints = value;
                OnPropertyChanged(nameof(MyBonusPoints));
            } 
        }
        public Person Worker
        {
            get
            {
                return _worker;
            }
            set
            {
                _worker = value;
                OnPropertyChanged(nameof(Worker));
            }
        }
        public string AdditionalComment 
        {
            get
            {
               return _additionalComment;
            }
            set
            {
                _additionalComment = value;
                OnPropertyChanged(nameof(AdditionalComment));
            }
        }


        public bool IsFirstBonusSelected
        {
            get
            {

                return _isFirstBonusSelected;
            }
            set
            {
                _isFirstBonusSelected = value;
                OnPropertyChanged(nameof(IsFirstBonusSelected));
            }
        }
        public DateTime DatePickerText
        {
            get
            {                                  
                return _datePickerText;
            }
            set
            {
                _datePickerText = value;
                OnPropertyChanged(nameof(DatePickerText));
            }
        }
        public decimal AdditionalBonus
        {
            get
            {
                return _additionalBonus;
            }
            set
            {
                _additionalBonus = value;
                AddRemoveAmount();
                OnPropertyChanged(nameof(AdditionalBonus));

            }
        }
        public decimal CurrentBonusAmount
        {
            get
            {
                if (_currentBonusAmount > 124)
                    _currentBonusAmount = Math.Round(_currentBonusAmount, 2, MidpointRounding.AwayFromZero);
                return _currentBonusAmount; 
            }
            set
            {
                _currentBonusAmount = value;
                OnPropertyChanged(nameof(CurrentBonusAmount));
            }
        }
        public ICommand BackButtonCommand
        {
            get
            {
                if (_backButtonCommand == null)
                { 
                    _backButtonCommand = new RelayCommand(
                        param => BackToGeneral());
                }

                return _backButtonCommand;
            }
        }
        public ICommand BtnResetCommandClicked
        {
            get
            {
                if (_btnResetCommandClicked == null)
                {
                    _btnResetCommandClicked = new RelayCommand(
                        param => ResetView(),
                        param => (MyBonusValuePoints.Where(x => x.IsSelected).Count() > 0) || (MyBonusValuePoints.Where(x => x.Comment != "").Count() > 0) || AdditionalBonus != 0 || CurrentBonusAmount != 0);
                }
                return _btnResetCommandClicked;
            }
        }
        static string currentDir = Directory.GetCurrentDirectory().Remove(2);

        #endregion

        #region Constructors
        public InsertViewModel()
        {
            Messenger.Default.Register<Person>(this, "InsertView", selectedPerson => GenerateBonusPoints(selectedPerson));
            DatePickerText = DateTime.Today;
        }
        #endregion

        #region Methods
        private void GenerateBonusPoints(Person thisPerson)
        {
            Worker = thisPerson;

            _oleDbDReader = DataBaseHandler.GetCommand($"Select * from Bv_General WHERE id = {Worker.P_BVID}");

            var files = Directory.GetFiles($@"O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0{Worker.P_PathToContentCells}", "*.txt", SearchOption.TopDirectoryOnly);
            _total = files.Count();
            while (_oleDbDReader.Read())
            {
                bool enabled = true;
                for (int i = 1; i < files.Count(); i++)
                {
                    if (i > 3)
                        enabled = false;
                    var content = File.ReadAllText(files[i - 1]);
              
                    MyBonusValuePoints.Add(new BonusValuesModel { Item = content, Amount = Convert.ToDecimal(_oleDbDReader.GetValue(i)), IsSelected = false, IsEnabled = enabled, Comment = string.Empty });
                }
            }

            CurrentBonusAmount = 0.00M;
        }
        private void EnableSecondBonus(bool enable)
        {
            for (int i = 3; i < MyBonusValuePoints.Count(); i++)
            {
                MyBonusValuePoints[i].IsEnabled = enable;
                MyBonusValuePoints[i].IsSelected = false;
            }
        }
        private void AddRemoveAmount()
        {
            if (MyBonusValuePoints[0].IsSelected && MyBonusValuePoints[1].IsSelected && MyBonusValuePoints[2].IsSelected && !IsFirstBonusSelected)
            {
                EnableSecondBonus(true);
                IsFirstBonusSelected = true;
            }
            else if ((!MyBonusValuePoints[0].IsSelected || !MyBonusValuePoints[1].IsSelected || !MyBonusValuePoints[2].IsSelected) && IsFirstBonusSelected)
            {
                EnableSecondBonus(false);
                IsFirstBonusSelected = false;

            }

            CurrentBonusAmount = 0.00M;
            CurrentBonusAmount += AdditionalBonus;

            foreach (var item in MyBonusValuePoints.Where(x => x.IsSelected))
                CurrentBonusAmount += item.Amount;
        }
        private void BackToGeneral()
        {
            MyBonusValuePoints.Clear();
            Mediator.Notify("GoToGeneral", "");
        }
        private bool _dateAlreadyExist(string date)
        {
            var reader = DataBaseHandler.GetCommand($"SELECT * FROM Bonus_General WHERE Worker_id = {Worker.P_Id} AND Period = '{date}'");
            if (reader.HasRows)
                return true;
            return false;
        }
        private void ResetView()
        {
            foreach (var bonus in MyBonusValuePoints)
            {
                bonus.IsSelected = false;
                bonus.Comment = string.Empty;
            }
            AdditionalBonus = 0.00M;
            CurrentBonusAmount = 0.00M;
        }
        #endregion

        //To Delete fromm her just a test
        public ICommand BtnCommandClicked
        {
            get
            {
                if (_btnCommandClicked == null)
                {
                    _btnCommandClicked = new RelayCommand(
                        param => ShowResult());
                }
                return _btnCommandClicked;
            }
        }
        public ICommand PrintButtonCommand { get => throw new NotImplementedException(); }

        private void ShowResult()                       
        {
            string date = $@"{DatePickerText.Month}\{DatePickerText.Year}";

            if (_dateAlreadyExist(date))
            {
                MessageBox.Show("There is already a bonus for this period");
                return;
            }

            string helloWorld = $"Report\nName: {Worker.P_Name}\nDate: {date}\n";
            string queryValues = "";
            string queryComments = "";
            string[] val = { "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "", "", "", "", "", "", "", "", "", "" };
           
            List<decimal> values = new List<decimal>
            {
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M,
                0.00M

            };
            List<string> comments = new List<string>
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                ""

            };

            var index = 0;

            foreach (var item in MyBonusValuePoints)
            {

                var amount = item.Amount;
                var comment = "";
                if (!item.IsSelected)
                    amount = 0.00M;
                if (item.Comment != string.Empty)
                    comment = item.Comment;
                helloWorld = helloWorld + $"{item.Item}: {amount}\n";
                val[index] = amount.ToString();
                val[index + 10] = comment;
                values[index] = amount;
                comments[index] = comment;
                index++;
            }
            
            for (int i = 0; i < 10; i++)
            {
                queryValues += $"'{values[i]}',";
                queryComments += $"'{comments[i]}',";    
            }

            helloWorld = helloWorld + $"Total: {CurrentBonusAmount}";
            //MessageBox.Show(helloWorld);
            val[_total - 1] = AdditionalBonus.ToString();
            values[_total - 1] = AdditionalBonus;
            comments[_total - 1] = AdditionalComment;
            DataBaseHandler.InsertCommand($"INSERT INTO Bonus_General " +
                $"(Worker_id,Period,Total,AmountA,AmountB,AmountC,AmountD,AmountE,AmountF,AmountG,AmountH,AmountI,AmountJ,CommentA,CommentB,CommentC,CommentD,CommentE,CommentF,CommentG,CommentH,CommentI,CommentJ) " +
                $"VALUES (" +
                    $"'{Worker.P_Id}', " +
                    $"'{date}'," +
                    $"'{CurrentBonusAmount}'," +
                    $"'{val[0]}'," +
                    $"'{val[1]}'," +
                    $"'{val[2]}'," +
                    $"'{val[3]}'," +
                    $"'{val[4]}'," +
                    $"'{val[5]}'," +
                    $"'{val[6]}'," +
                    $"'{val[7]}'," +
                    $"'{val[8]}'," +
                    $"'{val[9]}'," +
                    $"'{val[10]}'," +
                    $"'{val[11]}'," +
                    $"'{val[12]}'," +
                    $"'{val[13]}'," +
                    $"'{val[14]}'," +
                    $"'{val[15]}'," +
                    $"'{val[16]}'," +
                    $"'{val[17]}'," +
                    $"'{val[18]}'," +
                    $"'{val[19]}')");


            MyBonusPoints.Add(new BonusModel { WorkerName = Worker.P_Name, Total = CurrentBonusAmount, Amounts = values, Comments = comments, Period = date });
            CommonViewModel.PreviousViewModel = this;
            Mediator.Notify("GoToPrintingView", "");
            Messenger.Default.Send(Worker, "selectedPerson");
            Messenger.Default.Send(MyBonusPoints, "PrintView");
           

        }
    }
}
