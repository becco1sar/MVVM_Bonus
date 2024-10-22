using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using MVVM_Bonus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MVVM_Bonus.ViewModel
{
    public class InsertViewVeloViewModel : ObservableObject, IPageViewModel, ICommonCommandsModel
    {
        #region Fields
        private BonusValuesModel _selectedBonusPoint;
        private ObservableCollection<BonusValuesModel> _myBonusValuePoints;
        private BonusModel _myBonusPoints;
        private OleDbDataReader _oleDbDReader;
        private Person _worker;

        private bool _isFirstBonusSelected;
        private DateTime _datePickerText;
        private decimal _productiviteitBonus;
        private string _productiviteitComment;
        private decimal _additionalBonus;
        private string _additionalComment;
        private decimal _wachtDienstBonus;
        private string _wachtDienstComment;
        private decimal _flexBonus;
        private string _flexBonusComment;
        private decimal _currentBonusAmount;
        int total;

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
                _addRemoveAmount();
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
        public BonusModel MyBonusPoints
        {
            get
            {
                if (_myBonusPoints == null)
                    _myBonusPoints = new BonusModel();
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
                if (_datePickerText.ToString() == "01/01/0001 00:00:00")
                {
                    _datePickerText = DateTime.Today;
                }


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
                _addRemoveAmount();
                OnPropertyChanged(nameof(AdditionalBonus));

            }
        }

        public decimal WachtDienstBonus
        {
            get
            {
                return _wachtDienstBonus;
            }
            set
            {
                _wachtDienstBonus = value;
                _addRemoveAmount();
                OnPropertyChanged(nameof(WachtDienstBonus));

            }
        }
        public string WachtDienstComment
        {
            get
            {
                return _wachtDienstComment;
            }
            set
            {
                _wachtDienstComment = value;
                OnPropertyChanged(nameof(WachtDienstComment));
            }
        }
        public decimal FlexBonus
        {
            get
            {
                return _flexBonus;
            }
            set
            {
                _flexBonus = value;
                _addRemoveAmount();
                OnPropertyChanged(nameof(FlexBonus));

            }
        }
        public string FlexBonusComment
        {
            get
            {
                return _flexBonusComment;
            }
            set
            {
                _flexBonusComment = value;
                OnPropertyChanged(nameof(FlexBonusComment));
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
                        param => _backToGeneral());
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
                        param => _resetView(),
                        param => (MyBonusValuePoints.Where(x => x.IsSelected).Count() > 0) || (MyBonusValuePoints.Where(x => x.Comment != "").Count() > 0) || AdditionalBonus != 0 || CurrentBonusAmount != 0);
                }
                return _btnResetCommandClicked;
            }
        }
        static string currentDir = Directory.GetCurrentDirectory().Remove(2);

        #endregion

        #region Constructors
        public InsertViewVeloViewModel()
        {
            Messenger.Default.Register<Person>(this, "InsertView", selectedPerson => _generateBonusPoints(selectedPerson));


        }
        #endregion

        #region Methods
        private void _generateBonusPoints(Person thisPerson)
        {
            Worker = thisPerson;

            _oleDbDReader = DataBaseHandler.GetCommand($"Select * from Bv_General WHERE id = {Worker.P_BVID}");

            var files = Directory.GetFiles($@"O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0{Worker.P_PathToContentCells}", "*.txt", SearchOption.TopDirectoryOnly);
            total = files.Count();
            while (_oleDbDReader.Read())
            {
                bool enabled = true;
                for (int i = 0; i < files.Count() - 4; i++)
                {
      
                    var content = File.ReadAllText(files[i]);
                    MyBonusValuePoints.Add(new BonusValuesModel { Item = content, Amount = Convert.ToDecimal(_oleDbDReader.GetValue(i + 1)), IsSelected = false, IsEnabled = enabled, Comment = string.Empty });
                }
            }

            CurrentBonusAmount = 0.00M;
        }
        private void _enableSecondBonus(bool enable)
        {
            for (int i = 2; i < MyBonusValuePoints.Count(); i++)
            {
                MyBonusValuePoints[i].IsEnabled = enable;
                MyBonusValuePoints[i].IsSelected = false;
            }
        }
        private void _addRemoveAmount()
        {
            if (MyBonusValuePoints[0].IsSelected && MyBonusValuePoints[1].IsSelected && !IsFirstBonusSelected)
            {
                _enableSecondBonus(true);
                IsFirstBonusSelected = true;
            }
            else if ((!MyBonusValuePoints[0].IsSelected || !MyBonusValuePoints[1].IsSelected ) && IsFirstBonusSelected)
            {
                _enableSecondBonus(false);
                IsFirstBonusSelected = false;

            }

            CurrentBonusAmount = 0.00M;
            CurrentBonusAmount += ProductiviteitBonus;
            CurrentBonusAmount += AdditionalBonus;
            CurrentBonusAmount += WachtDienstBonus;
            CurrentBonusAmount += FlexBonus;

            foreach (var item in MyBonusValuePoints.Where(x => x.IsSelected))
                CurrentBonusAmount += item.Amount;
        }
        private void _backToGeneral()
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
        private void _resetView()
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
        public decimal ProductiviteitBonus
        {
            get
            {
                return _productiviteitBonus;
            }
            set 
            {
                _productiviteitBonus = value;
                _addRemoveAmount();
                OnPropertyChanged(nameof(ProductiviteitBonus));
            }
        }
        public string ProductiviteitComment 
        {
            get
            {
                return _productiviteitComment;
            }
            set
            {
                _productiviteitComment = value;
                
                OnPropertyChanged(nameof(ProductiviteitComment));
            }
        }

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
            val[total - 3] = ProductiviteitBonus.ToString();
            values[total - 3] = ProductiviteitBonus;
            comments[total - 3] = ProductiviteitComment;
            val[total - 3] = WachtDienstBonus.ToString();
            values[total - 3] = WachtDienstBonus;
            comments[total - 3] = WachtDienstComment;
            val[total - 2] = FlexBonus.ToString();
            values[total - 2] = FlexBonus;
            comments[total - 2] = FlexBonusComment;
            val[total - 1] = AdditionalBonus.ToString();
            values[total - 1] = AdditionalBonus;
            comments[total - 1] = AdditionalComment;
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


            MyBonusPoints = new BonusModel { WorkerName = Worker.P_Name, Total = CurrentBonusAmount, Amounts = values, Comments = comments, Period = date };
            CommonViewModel.PreviousViewModel = this;
            Mediator.Notify("GoToPrintingView", "");
            Messenger.Default.Send(Worker, "selectedPerson");
            Messenger.Default.Send(MyBonusPoints, "PrintView");


        }
    }
}