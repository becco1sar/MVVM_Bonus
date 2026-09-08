using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using MVVM_Bonus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MVVM_Bonus.ViewModel
{
	public class InsertViewModel : ObservableObject, IPageViewModel, ICommonCommandsModel
	{
		#region Fields
		private ObservableCollection<AdditionalBonusCategory> _customBonusCategories;
		private BonusValuesModel _selectedBonusPoint;
		private ObservableCollection<BonusValuesModel> _myBonusValuePoints;
		private ObservableCollection<BonusModel> _myBonusPoints;
		private Person _worker;
		private readonly DataBaseService _databaseService;
		private bool _isFirstBonusSelected;
		private DateTime _datePickerText;
		private decimal _additionalBonus;
		private string _additionalComment;
		private decimal _currentBonusAmount;
		private int _total;
		private bool _isPreviewPanelOpen;
		private FlowDocument _previewDocument;
		private BonusModel _lastGeneratedBonusModel;

		private ICommand _btnCommandClicked;
		private ICommand _btnResetCommandClicked;
		private ICommand _backButtonCommand;
		private ICommand _printButtonCommand;
		private ICommand _btnPreviewCommand;
		private ICommand _closePreviewCommand;
		#endregion

		#region Properties
		public ObservableCollection<AdditionalBonusCategory> CustomBonusCategories
		{
			get
			{
				if (_customBonusCategories == null)
					_customBonusCategories = new ObservableCollection<AdditionalBonusCategory>();
				return _customBonusCategories;
			}
			set
			{
				_customBonusCategories = value;
				OnPropertyChanged(nameof(CustomBonusCategories));
			}
		}

		public BonusValuesModel SelectedBonusPoint
		{
			get => _selectedBonusPoint;
			set
			{
				_selectedBonusPoint = value;
				RefreshCurrentAmount();
				OnPropertyChanged(nameof(SelectedBonusPoint));
			}
		}

		public ObservableCollection<BonusValuesModel> MyBonusValuePoints
		{
			get
			{
				if (_myBonusValuePoints == null)
					_myBonusValuePoints = new ObservableCollection<BonusValuesModel>();
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
			get => _worker;
			set
			{
				_worker = value;
				OnPropertyChanged(nameof(Worker));
			}
		}

		public string AdditionalComment
		{
			get => _additionalComment;
			set
			{
				_additionalComment = value;
				OnPropertyChanged(nameof(AdditionalComment));
				RefreshCurrentAmount();
			}
		}

		public bool IsFirstBonusSelected
		{
			get => _isFirstBonusSelected;
			set
			{
				_isFirstBonusSelected = value;
				OnPropertyChanged(nameof(IsFirstBonusSelected));
			}
		}

		public DateTime DatePickerText
		{
			get => _datePickerText;
			set
			{
				_datePickerText = value;
				OnPropertyChanged(nameof(DatePickerText));
			}
		}

		public decimal AdditionalBonus
		{
			get => _additionalBonus;
			set
			{
				_additionalBonus = value;
				OnPropertyChanged(nameof(AdditionalBonus));
				RefreshCurrentAmount();
			}
		}

		public decimal CurrentBonusAmount
		{
			get => _currentBonusAmount;
			set
			{
				_currentBonusAmount = value > 124
						? Math.Round(value, 2, MidpointRounding.AwayFromZero)
						: value;

				OnPropertyChanged(nameof(CurrentBonusAmount));
			}
		}

		public bool IsPreviewPanelOpen
		{
			get => _isPreviewPanelOpen;
			set
			{
				_isPreviewPanelOpen = value;
				OnPropertyChanged(nameof(IsPreviewPanelOpen));
			}
		}

		public FlowDocument PreviewDocument
		{
			get => _previewDocument;
			set
			{
				_previewDocument = value;
				OnPropertyChanged(nameof(PreviewDocument));
			}
		}

		public ICommand BackButtonCommand
		{
			get
			{
				if (_backButtonCommand == null)
					_backButtonCommand = new RelayCommand(param => BackToGeneral());
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
							param => MyBonusValuePoints.Any(x => x.IsSelected || !string.IsNullOrWhiteSpace(x.Comment)) ||
											 CustomBonusCategories.Any(c => c.Amount != 0 || !string.IsNullOrWhiteSpace(c.Comment)) ||
											 CurrentBonusAmount != 0);
				}
				return _btnResetCommandClicked;
			}
		}

		public ICommand BtnCommandClicked
		{
			get
			{
				if (_btnCommandClicked == null)
					_btnCommandClicked = new RelayCommand(param => FinishAndPreview());
				return _btnCommandClicked;
			}
		}

		public ICommand PrintButtonCommand
		{
			get
			{
				if (_printButtonCommand == null)
					_printButtonCommand = new RelayCommand(param => PrintPreviewDocument(), param => PreviewDocument != null);
				return _printButtonCommand;
			}
		}

		public ICommand BtnPreviewCommand
		{
			get
			{
				if (_btnPreviewCommand == null)
					_btnPreviewCommand = new RelayCommand(param => RefreshPreview(), param => _lastGeneratedBonusModel != null);
				return _btnPreviewCommand;
			}
		}

		public ICommand ClosePreviewCommand
		{
			get
			{
				if (_closePreviewCommand == null)
					_closePreviewCommand = new RelayCommand(param => ClosePreview());
				return _closePreviewCommand;
			}
		}
		#endregion

		#region Constructors
		private readonly IReportService _reportService;

		public InsertViewModel()
		{
			_reportService = new ReportService();
			Messenger.Default.Register<Person>(this, "InsertView", selectedPerson => GenerateBonusPoints(selectedPerson));
			DatePickerText = DateTime.Today;
			_databaseService = new DataBaseService();
		}
		#endregion

		#region Methods
		private void GenerateBonusPoints(Person thisPerson)
		{
			Worker = thisPerson;

			foreach (var item in MyBonusValuePoints)
				item.PropertyChanged -= BonusValue_PropertyChanged;

			MyBonusValuePoints.Clear();
			CustomBonusCategories.Clear();

			CurrentBonusAmount = 0.00M;
			AdditionalBonus = 0.00M;
			AdditionalComment = string.Empty;
			IsFirstBonusSelected = false;
			IsPreviewPanelOpen = false;
			PreviewDocument = null;
			_lastGeneratedBonusModel = null;

			// Setup dynamic bonus categories depending on worker role
			if (Worker != null && Worker.Role != null && Worker.Role.ToLower().Contains("velo"))
			{
				CustomBonusCategories.Add(new AdditionalBonusCategory { CategoryName = "Productiviteit" });
				CustomBonusCategories.Add(new AdditionalBonusCategory { CategoryName = "Wachtdienst" });
				CustomBonusCategories.Add(new AdditionalBonusCategory { CategoryName = "Flexpremies" });
				CustomBonusCategories.Add(new AdditionalBonusCategory { CategoryName = "Additional Bonus" });
			}
			else
			{
				CustomBonusCategories.Add(new AdditionalBonusCategory { CategoryName = "Additional Bonus" });
			}

			foreach (var category in CustomBonusCategories)
			{
				category.PropertyChanged += (s, e) => RefreshCurrentAmount();
			}

			var table = _databaseService.ExecuteQuery("SELECT * FROM Bv_General WHERE id = ?", Worker.BvId);

			var files = Directory.GetFiles(
					$@"O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0{Worker.PathToContentCells}",
					"*.txt",
					SearchOption.TopDirectoryOnly);

			_total = files.Count();

			if (table.Rows.Count > 0)
			{
				var row = table.Rows[0];
				for (int i = 1; i < files.Count() + 1; i++)
				{
					bool enabled = i <= 3;
					string content = File.ReadAllText(files[i - 1]);

					var bonusValue = new BonusValuesModel
					{
						Item = content,
						Amount = Convert.ToDecimal(row[i]),
						IsSelected = false,
						IsEnabled = enabled,
						Comment = string.Empty
					};

					bonusValue.PropertyChanged += BonusValue_PropertyChanged;
					MyBonusValuePoints.Add(bonusValue);
				}
			}

			RefreshCurrentAmount();
		}

		private void BonusValue_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BonusValuesModel.IsSelected) ||
					e.PropertyName == nameof(BonusValuesModel.Comment))
			{
				RefreshCurrentAmount();
			}
		}

		private void EnableSecondBonus(bool enable)
		{
			for (int i = 3; i < MyBonusValuePoints.Count; i++)
			{
				MyBonusValuePoints[i].IsEnabled = enable;

				if (!enable)
					MyBonusValuePoints[i].IsSelected = false;
			}
		}

		private void RefreshCurrentAmount()
		{
			if (MyBonusValuePoints.Count >= 3)
			{
				bool firstThreeSelected =
						MyBonusValuePoints[0].IsSelected &&
						MyBonusValuePoints[1].IsSelected &&
						MyBonusValuePoints[2].IsSelected;

				if (firstThreeSelected && !IsFirstBonusSelected)
				{
					EnableSecondBonus(true);
					IsFirstBonusSelected = true;
				}
				else if (!firstThreeSelected && IsFirstBonusSelected)
				{
					EnableSecondBonus(false);
					IsFirstBonusSelected = false;
				}
			}

			decimal total = CustomBonusCategories.Sum(c => c.Amount);

			foreach (var item in MyBonusValuePoints.Where(x => x.IsSelected))
				total += item.Amount;

			CurrentBonusAmount = total;
		}

		private void BackToGeneral()
		{
			foreach (var item in MyBonusValuePoints)
				item.PropertyChanged -= BonusValue_PropertyChanged;

			MyBonusValuePoints.Clear();
			CustomBonusCategories.Clear();
			PreviewDocument = null;
			IsPreviewPanelOpen = false;
			_lastGeneratedBonusModel = null;

			Mediator.Notify(Constants.MAIN_MENU_VIEW, "");
		}

		private bool DateAlreadyExist(string date)
		{
			var count = _databaseService.ExecuteScalar(
					"SELECT COUNT(*) FROM Bonus_General WHERE Worker_id = ? AND Period = ?", Worker.Id, date);

			return Convert.ToInt32(count) > 0;
		}

		private void ResetView()
		{
			foreach (var bonus in MyBonusValuePoints)
			{
				bonus.IsSelected = false;
				bonus.Comment = string.Empty;
			}

			foreach (var category in CustomBonusCategories)
			{
				category.Amount = 0.00M;
				category.Comment = string.Empty;
			}

			AdditionalBonus = 0.00M;
			AdditionalComment = string.Empty;
			CurrentBonusAmount = 0.00M;
			IsPreviewPanelOpen = false;
			PreviewDocument = null;
			_lastGeneratedBonusModel = null;
		}

		private void FinishAndPreview()
		{
			string date = $@"{DatePickerText.Month}/{DatePickerText.Year}";

			if (DateAlreadyExist(date))
			{
				MessageBox.Show("There is already a bonus for this period");
				return;
			}

			BonusModel bonusModel = BuildBonusModel(date);
			SaveBonus(bonusModel);

			_lastGeneratedBonusModel = bonusModel;
			PreviewDocument = _reportService.GenerateBonusReport(bonusModel);
			IsPreviewPanelOpen = true;
		}

		private BonusModel BuildBonusModel(string date)
		{
			List<decimal> values = Enumerable.Repeat(0.00M, 10).ToList();
			List<string> comments = Enumerable.Repeat(string.Empty, 10).ToList();

			for (int i = 0; i < MyBonusValuePoints.Count && i < 10; i++)
			{
				values[i] = MyBonusValuePoints[i].IsSelected ? MyBonusValuePoints[i].Amount : 0.00M;
				comments[i] = string.IsNullOrWhiteSpace(MyBonusValuePoints[i].Comment)
						? string.Empty
						: MyBonusValuePoints[i].Comment;
			}

			int catCount = CustomBonusCategories.Count;
			for (int i = 0; i < catCount; i++)
			{
				int index = Math.Max(0, Math.Min(9, 10 - catCount + i));
				values[index] = CustomBonusCategories[i].Amount;
				comments[index] = CustomBonusCategories[i].Comment ?? string.Empty;
			}

			return new BonusModel
			{
				WorkerName = Worker.Name,
				Total = CurrentBonusAmount,
				Amounts = values,
				Comments = comments,
				Period = date
			};
		}

		private void SaveBonus(BonusModel bonusModel)
		{
			string query = @"INSERT INTO Bonus_General 
				(Worker_id, Period, Total, AmountA, AmountB, AmountC, AmountD, AmountE, AmountF, AmountG, AmountH, AmountI, AmountJ, CommentA, CommentB, CommentC, CommentD, CommentE, CommentF, CommentG, CommentH, CommentI, CommentJ) 
				VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

			_databaseService.ExecuteNonQuery(query,
				Worker.Id,
				bonusModel.Period,
				bonusModel.Total,
				bonusModel.Amounts[0], bonusModel.Amounts[1], bonusModel.Amounts[2], bonusModel.Amounts[3], bonusModel.Amounts[4],
				bonusModel.Amounts[5], bonusModel.Amounts[6], bonusModel.Amounts[7], bonusModel.Amounts[8], bonusModel.Amounts[9],
				bonusModel.Comments[0], bonusModel.Comments[1], bonusModel.Comments[2], bonusModel.Comments[3], bonusModel.Comments[4],
				bonusModel.Comments[5], bonusModel.Comments[6], bonusModel.Comments[7], bonusModel.Comments[8], bonusModel.Comments[9]
			);
		}

		private void RefreshPreview()
		{
			if (_lastGeneratedBonusModel == null)
				return;

			PreviewDocument = _reportService.GenerateBonusReport(_lastGeneratedBonusModel);
			IsPreviewPanelOpen = true;
		}

		private void PrintPreviewDocument()
		{
			_reportService.PrintDocument(PreviewDocument, "Bonus Print");
		}

		public void ClosePreview()
		{
			IsPreviewPanelOpen = false;
		}
		#endregion

		#region Helper Class
		private class PreviewDetailRow
		{
			public string DetailPoint { get; set; }
			public string Comment { get; set; }
			public decimal Amount { get; set; }
		}
		#endregion
	}
}