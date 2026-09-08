using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using MVVM_Bonus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace MVVM_Bonus.ViewModel
{
    /// <summary>Where the user is in the record-a-bonus flow.</summary>
    public enum InsertStage
    {
        /// <summary>Choosing evaluation points and amounts.</summary>
        Editing,

        /// <summary>Looking at the generated sheet, nothing written yet.</summary>
        Reviewing,

        /// <summary>Written to the database.</summary>
        Saved
    }

    /// <summary>A selectable month in the period picker.</summary>
    public class MonthOption
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class InsertViewModel : ObservableObject, IPageViewModel, ICommonCommandsModel
    {
        /// <summary>Base points that must all be awarded before the rest unlock.</summary>
        private const int RequiredBaseCount = 3;

        /// <summary>Amount columns available in Bonus_General (AmountA..AmountJ).</summary>
        private const int AmountColumnCount = 10;

        #region Fields
        private readonly DataBaseService _databaseService;
        private readonly IReportService _reportService;

        private ObservableCollection<BonusValuesModel> _baseBonusPoints;
        private ObservableCollection<BonusValuesModel> _additionalBonusPoints;
        private ObservableCollection<AdditionalBonusCategory> _customBonusCategories;

        private Person _worker;
        private MonthOption _selectedMonth;
        private int _selectedYear;
        private string _periodWarning;
        private decimal _currentBonusAmount;
        private InsertStage _stage;
        private FlowDocument _previewDocument;
        private BonusModel _pendingBonus;
        private string _statusMessage;
        private bool _isStatusError;
        private string _capacityWarning;

        private ICommand _reviewCommand;
        private ICommand _confirmSaveCommand;
        private ICommand _editAgainCommand;
        private ICommand _resetCommand;
        private ICommand _backButtonCommand;
        private ICommand _printButtonCommand;
        #endregion

        #region Constructors
        public InsertViewModel()
        {
            _databaseService = new DataBaseService();
            _reportService = new ReportService();

            Months = new ObservableCollection<MonthOption>(
                Enumerable.Range(1, 12).Select(m => new MonthOption
                {
                    Number = m,
                    Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                }));

            // Only month and year are ever stored, so a full date picker offered a
            // day choice that the app then discarded.
            Years = new ObservableCollection<int>(
                Enumerable.Range(DateTime.Today.Year - 3, 5).Reverse());

            _selectedYear = DateTime.Today.Year;
            _selectedMonth = Months.First(m => m.Number == DateTime.Today.Month);

            Messenger.Default.Register<Person>(
                this,
                Constants.MESSENGER_INSERT_VIEW,
                selectedPerson => GenerateBonusPoints(selectedPerson));
        }
        #endregion

        #region Period
        public ObservableCollection<MonthOption> Months { get; }

        public ObservableCollection<int> Years { get; }

        public MonthOption SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                OnPropertyChanged(nameof(PeriodText));
                CheckPeriod();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                OnPropertyChanged(nameof(PeriodText));
                CheckPeriod();
            }
        }

        /// <summary>
        /// The period exactly as it is stored, so what the user picks and what lands
        /// in the database cannot drift apart.
        /// </summary>
        public string PeriodText => $"{SelectedMonth?.Number ?? DateTime.Today.Month}/{SelectedYear}";

        /// <summary>
        /// Set when this employee already has a bonus for the chosen period. Checked
        /// as soon as the period changes rather than after the user has filled the
        /// whole form and pressed the finish button.
        /// </summary>
        public string PeriodWarning
        {
            get => _periodWarning;
            private set
            {
                _periodWarning = value;
                OnPropertyChanged(nameof(PeriodWarning));
                OnPropertyChanged(nameof(HasPeriodWarning));
            }
        }

        public bool HasPeriodWarning => !string.IsNullOrWhiteSpace(PeriodWarning);
        #endregion

        #region Bonus points
        /// <summary>Points 1-3. All three are required to unlock the rest.</summary>
        public ObservableCollection<BonusValuesModel> BaseBonusPoints
        {
            get
            {
                if (_baseBonusPoints == null)
                    _baseBonusPoints = new ObservableCollection<BonusValuesModel>();
                return _baseBonusPoints;
            }
        }

        /// <summary>Points 4 and up, locked until the base points are all awarded.</summary>
        public ObservableCollection<BonusValuesModel> AdditionalBonusPoints
        {
            get
            {
                if (_additionalBonusPoints == null)
                    _additionalBonusPoints = new ObservableCollection<BonusValuesModel>();
                return _additionalBonusPoints;
            }
        }

        public ObservableCollection<AdditionalBonusCategory> CustomBonusCategories
        {
            get
            {
                if (_customBonusCategories == null)
                    _customBonusCategories = new ObservableCollection<AdditionalBonusCategory>();
                return _customBonusCategories;
            }
        }

        /// <summary>Every point in stored order; used to build the record.</summary>
        private IEnumerable<BonusValuesModel> AllPoints =>
            BaseBonusPoints.Concat(AdditionalBonusPoints);

        public Person Worker
        {
            get => _worker;
            private set
            {
                _worker = value;
                OnPropertyChanged(nameof(Worker));
            }
        }

        /// <summary>How many of the three base points are awarded so far.</summary>
        public int AwardedBaseCount => BaseBonusPoints.Count(p => p.IsSelected);

        public int RequiredBase => RequiredBaseCount;

        /// <summary>
        /// True once all three base points are awarded. This rule was enforced in code
        /// and invisible on screen: the extra points simply sat at 40% opacity with
        /// nothing to say why, or what would change that.
        /// </summary>
        public bool IsUnlocked =>
            BaseBonusPoints.Count >= RequiredBaseCount && AwardedBaseCount >= RequiredBaseCount;

        public bool HasAdditionalPoints => AdditionalBonusPoints.Count > 0;

        public string UnlockHint
        {
            get
            {
                if (BaseBonusPoints.Count < RequiredBaseCount)
                    return "This employee has fewer than three evaluation points on file.";

                if (IsUnlocked)
                    return "All base points awarded. The additional points below are available.";

                int missing = RequiredBaseCount - AwardedBaseCount;

                return missing == 1
                    ? "Award the last base point to unlock the additional points below."
                    : $"Award all three base points to unlock the additional points below. {missing} still to go.";
            }
        }

        /// <summary>
        /// Warns when the number of evaluation points plus extra categories exceeds the
        /// ten amount columns in Bonus_General, in which case the categories overwrite
        /// the last points. Pre-existing behaviour, surfaced rather than changed.
        /// </summary>
        public string CapacityWarning
        {
            get => _capacityWarning;
            private set
            {
                _capacityWarning = value;
                OnPropertyChanged(nameof(CapacityWarning));
                OnPropertyChanged(nameof(HasCapacityWarning));
            }
        }

        public bool HasCapacityWarning => !string.IsNullOrWhiteSpace(CapacityWarning);
        #endregion

        #region Totals
        public decimal BaseTotal => BaseBonusPoints.Where(p => p.IsSelected).Sum(p => p.Amount);

        public decimal AdditionalTotal => AdditionalBonusPoints.Where(p => p.IsSelected).Sum(p => p.Amount);

        public decimal ExtrasTotal => CustomBonusCategories.Sum(c => c.Amount);

        public decimal CurrentBonusAmount
        {
            get => _currentBonusAmount;
            private set
            {
                _currentBonusAmount = value;
                OnPropertyChanged(nameof(CurrentBonusAmount));
                OnPropertyChanged(nameof(HasAmount));
            }
        }

        public bool HasAmount => CurrentBonusAmount > 0m;

        /// <summary>Awarded points out of the total available, for the footer summary.</summary>
        public string AwardedCountText
        {
            get
            {
                int awarded = AllPoints.Count(p => p.IsSelected);
                int total = BaseBonusPoints.Count + AdditionalBonusPoints.Count;
                return $"{awarded} of {total} points awarded";
            }
        }
        #endregion

        #region Stage
        public InsertStage Stage
        {
            get => _stage;
            private set
            {
                _stage = value;
                OnPropertyChanged(nameof(Stage));
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsReviewing));
                OnPropertyChanged(nameof(IsSaved));
                OnPropertyChanged(nameof(IsPreviewOpen));
            }
        }

        public bool IsEditing => Stage == InsertStage.Editing;

        public bool IsReviewing => Stage == InsertStage.Reviewing;

        public bool IsSaved => Stage == InsertStage.Saved;

        public bool IsPreviewOpen => Stage != InsertStage.Editing;

        public FlowDocument PreviewDocument
        {
            get => _previewDocument;
            private set
            {
                _previewDocument = value;
                OnPropertyChanged(nameof(PreviewDocument));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public bool IsStatusError
        {
            get => _isStatusError;
            private set { _isStatusError = value; OnPropertyChanged(nameof(IsStatusError)); }
        }
        #endregion

        #region Commands
        /// <summary>
        /// Builds the sheet for checking. Writes nothing: the old single "Finish and
        /// Preview" button inserted the row into Bonus_General first and showed the
        /// preview afterwards, so by the time a mistake was visible it was recorded,
        /// and the period lock then blocked a corrected re-entry.
        /// </summary>
        public ICommand ReviewCommand
        {
            get
            {
                if (_reviewCommand == null)
                {
                    _reviewCommand = new RelayCommand(
                        param => Review(),
                        param => IsEditing && HasAmount && !HasPeriodWarning);
                }
                return _reviewCommand;
            }
        }

        /// <summary>Writes the reviewed sheet to the database.</summary>
        public ICommand ConfirmSaveCommand
        {
            get
            {
                if (_confirmSaveCommand == null)
                {
                    _confirmSaveCommand = new RelayCommand(
                        param => ConfirmSave(),
                        param => IsReviewing && _pendingBonus != null);
                }
                return _confirmSaveCommand;
            }
        }

        /// <summary>Returns to editing without saving.</summary>
        public ICommand EditAgainCommand
        {
            get
            {
                if (_editAgainCommand == null)
                {
                    _editAgainCommand = new RelayCommand(
                        param => BackToEditing(),
                        param => IsReviewing);
                }
                return _editAgainCommand;
            }
        }

        public ICommand BtnResetCommandClicked
        {
            get
            {
                if (_resetCommand == null)
                {
                    _resetCommand = new RelayCommand(
                        param => ResetForm(),
                        param => IsEditing && HasAnyInput());
                }
                return _resetCommand;
            }
        }

        public ICommand BackButtonCommand
        {
            get
            {
                if (_backButtonCommand == null)
                    _backButtonCommand = new RelayCommand(param => BackToTeam());
                return _backButtonCommand;
            }
        }

        public ICommand PrintButtonCommand
        {
            get
            {
                if (_printButtonCommand == null)
                {
                    _printButtonCommand = new RelayCommand(
                        param => _reportService.PrintDocument(PreviewDocument, "Quality bonus"),
                        param => PreviewDocument != null && IsSaved);
                }
                return _printButtonCommand;
            }
        }
        #endregion

        #region Loading
        private void GenerateBonusPoints(Person thisPerson)
        {
            DetachHandlers();

            BaseBonusPoints.Clear();
            AdditionalBonusPoints.Clear();
            CustomBonusCategories.Clear();

            Worker = thisPerson;

            Stage = InsertStage.Editing;
            PreviewDocument = null;
            _pendingBonus = null;
            CurrentBonusAmount = 0m;
            StatusMessage = null;
            CapacityWarning = null;

            _selectedMonth = Months.First(m => m.Number == DateTime.Today.Month);
            _selectedYear = DateTime.Today.Year;
            OnPropertyChanged(nameof(SelectedMonth));
            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(PeriodText));

            if (Worker == null)
                return;

            AddCustomCategories(Worker.Role);
            LoadEvaluationPoints(Worker);

            CheckPeriod();
            RefreshTotals();
        }

        /// <summary>Extra amount boxes that depend on the employee's role.</summary>
        private void AddCustomCategories(string role)
        {
            if (!string.IsNullOrEmpty(role) && role.ToLower().Contains("velo"))
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

            foreach (AdditionalBonusCategory category in CustomBonusCategories)
                category.PropertyChanged += Category_PropertyChanged;
        }

        private void LoadEvaluationPoints(Person worker)
        {
            try
            {
                DataTable table = _databaseService.ExecuteQuery(
                    "SELECT * FROM Bv_General WHERE id = ?", worker.BvId);

                if (table.Rows.Count == 0)
                {
                    Report("No bonus values are configured for this employee's profile. Ask HR to check the default bonus values.",
                           isError: true);
                    return;
                }

                string folder = Path.Combine(Constants.CONTENT_CELL_ROOT,
                                             (worker.PathToContentCells ?? string.Empty).TrimStart('\\', '/'));

                if (!Directory.Exists(folder))
                {
                    Report($"Cannot find the evaluation points for this employee's role and language.\n{folder}",
                           isError: true);
                    return;
                }

                string[] files = Directory.GetFiles(folder, "*.txt", SearchOption.TopDirectoryOnly);

                if (files.Length == 0)
                {
                    Report("No evaluation points are set up for this employee's role and language.", isError: true);
                    return;
                }

                DataRow row = table.Rows[0];

                for (int i = 0; i < files.Length && i + 1 < table.Columns.Count; i++)
                {
                    var point = new BonusValuesModel
                    {
                        Item = File.ReadAllText(files[i]).Trim(),
                        Amount = row[i + 1] == DBNull.Value ? 0m : Convert.ToDecimal(row[i + 1]),
                        IsSelected = false,
                        // Base points start available; the rest unlock once all three are awarded.
                        IsEnabled = i < RequiredBaseCount,
                        Comment = string.Empty
                    };

                    point.PropertyChanged += Point_PropertyChanged;

                    if (i < RequiredBaseCount)
                        BaseBonusPoints.Add(point);
                    else
                        AdditionalBonusPoints.Add(point);
                }

                int slotsNeeded = BaseBonusPoints.Count + AdditionalBonusPoints.Count + CustomBonusCategories.Count;

                if (slotsNeeded > AmountColumnCount)
                {
                    CapacityWarning =
                        $"This role has {BaseBonusPoints.Count + AdditionalBonusPoints.Count} evaluation points "
                        + $"and {CustomBonusCategories.Count} extra categories, but the database stores only "
                        + $"{AmountColumnCount} amounts per bonus. The extra categories will overwrite the last "
                        + "evaluation points. Ask HR to review this profile.";
                }
            }
            catch (Exception ex)
            {
                Report($"Could not load the evaluation points: {ex.Message}", isError: true);
            }
        }
        #endregion

        #region Reacting to input
        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BonusValuesModel.IsSelected)
                || e.PropertyName == nameof(BonusValuesModel.Comment))
            {
                RefreshTotals();
            }
        }

        private void Category_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshTotals();
        }

        private void DetachHandlers()
        {
            foreach (BonusValuesModel point in AllPoints)
                point.PropertyChanged -= Point_PropertyChanged;

            foreach (AdditionalBonusCategory category in CustomBonusCategories)
                category.PropertyChanged -= Category_PropertyChanged;
        }

        private void RefreshTotals()
        {
            ApplyUnlockRule();

            CurrentBonusAmount = Math.Round(BaseTotal + AdditionalTotal + ExtrasTotal, 2,
                                            MidpointRounding.AwayFromZero);

            OnPropertyChanged(nameof(BaseTotal));
            OnPropertyChanged(nameof(AdditionalTotal));
            OnPropertyChanged(nameof(ExtrasTotal));
            OnPropertyChanged(nameof(AwardedBaseCount));
            OnPropertyChanged(nameof(IsUnlocked));
            OnPropertyChanged(nameof(UnlockHint));
            OnPropertyChanged(nameof(AwardedCountText));
        }

        /// <summary>
        /// Keeps the additional points locked until all three base points are awarded,
        /// and clears them again if a base point is taken back.
        /// </summary>
        private void ApplyUnlockRule()
        {
            bool unlocked = BaseBonusPoints.Count >= RequiredBaseCount
                            && BaseBonusPoints.Count(p => p.IsSelected) >= RequiredBaseCount;

            foreach (BonusValuesModel point in AdditionalBonusPoints)
            {
                if (point.IsEnabled == unlocked)
                    continue;

                point.IsEnabled = unlocked;

                if (!unlocked)
                    point.IsSelected = false;
            }
        }

        private bool HasAnyInput()
        {
            return AllPoints.Any(p => p.IsSelected || !string.IsNullOrWhiteSpace(p.Comment))
                   || CustomBonusCategories.Any(c => c.Amount != 0m || !string.IsNullOrWhiteSpace(c.Comment));
        }
        #endregion

        #region Period check
        /// <summary>
        /// Looks for an existing bonus for this employee and period. Runs on every
        /// period change so the clash is visible before any work is done.
        /// </summary>
        private void CheckPeriod()
        {
            if (Worker == null)
            {
                PeriodWarning = null;
                return;
            }

            try
            {
                object count = _databaseService.ExecuteScalar(
                    "SELECT COUNT(*) FROM Bonus_General WHERE Worker_id = ? AND Period = ?",
                    Worker.Id, PeriodText);

                PeriodWarning = Convert.ToInt32(count) > 0
                    ? $"{Worker.Name} already has a bonus recorded for {PeriodText}. Choose a different period."
                    : null;
            }
            catch (Exception ex)
            {
                PeriodWarning = $"Could not check whether a bonus already exists for {PeriodText}: {ex.Message}";
            }
        }
        #endregion

        #region Review, save, reset
        private void Review()
        {
            _pendingBonus = BuildBonusModel(PeriodText);
            PreviewDocument = _reportService.GenerateBonusReport(_pendingBonus);
            Stage = InsertStage.Reviewing;
            Report("Nothing has been saved yet. Check the sheet, then confirm.");
        }

        private void BackToEditing()
        {
            Stage = InsertStage.Editing;
            PreviewDocument = null;
            _pendingBonus = null;
            StatusMessage = null;
        }

        private void ConfirmSave()
        {
            if (_pendingBonus == null)
                return;

            // The period could have been taken since the review started.
            CheckPeriod();

            if (HasPeriodWarning)
            {
                Stage = InsertStage.Editing;
                Report(PeriodWarning, isError: true);
                return;
            }

            try
            {
                SaveBonus(_pendingBonus);
                Stage = InsertStage.Saved;
                Report($"Saved {_pendingBonus.Total:N2} € for {_pendingBonus.WorkerName}, period {_pendingBonus.Period}.");
            }
            catch (Exception ex)
            {
                Report($"Could not save the bonus: {ex.Message}", isError: true);
            }
        }

        private void ResetForm()
        {
            foreach (BonusValuesModel point in AllPoints)
            {
                point.IsSelected = false;
                point.Comment = string.Empty;
            }

            foreach (AdditionalBonusCategory category in CustomBonusCategories)
            {
                category.Amount = 0m;
                category.Comment = string.Empty;
            }

            Stage = InsertStage.Editing;
            PreviewDocument = null;
            _pendingBonus = null;
            StatusMessage = null;

            RefreshTotals();
        }

        private void BackToTeam()
        {
            DetachHandlers();

            BaseBonusPoints.Clear();
            AdditionalBonusPoints.Clear();
            CustomBonusCategories.Clear();

            PreviewDocument = null;
            _pendingBonus = null;
            Stage = InsertStage.Editing;
            StatusMessage = null;

            Mediator.Notify(Constants.MAIN_MENU_VIEW, "");
        }
        #endregion

        #region Persistence
        private BonusModel BuildBonusModel(string period)
        {
            List<decimal> values = Enumerable.Repeat(0m, AmountColumnCount).ToList();
            List<string> comments = Enumerable.Repeat(string.Empty, AmountColumnCount).ToList();
            List<string> labels = Enumerable.Repeat(string.Empty, AmountColumnCount).ToList();

            List<BonusValuesModel> points = AllPoints.ToList();

            for (int i = 0; i < points.Count && i < AmountColumnCount; i++)
            {
                values[i] = points[i].IsSelected ? points[i].Amount : 0m;
                comments[i] = points[i].Comment ?? string.Empty;
                labels[i] = points[i].Item ?? string.Empty;
            }

            // Extra categories occupy the trailing amount columns. Kept exactly as it
            // was so existing records stay readable; CapacityWarning reports the case
            // where this overwrites evaluation points.
            int categoryCount = CustomBonusCategories.Count;

            for (int i = 0; i < categoryCount; i++)
            {
                int index = Math.Max(0, Math.Min(AmountColumnCount - 1, AmountColumnCount - categoryCount + i));

                values[index] = CustomBonusCategories[i].Amount;
                comments[index] = CustomBonusCategories[i].Comment ?? string.Empty;
                labels[index] = CustomBonusCategories[i].CategoryName ?? string.Empty;
            }

            return new BonusModel
            {
                WorkerName = Worker?.Name,
                Total = CurrentBonusAmount,
                Amounts = values,
                Comments = comments,
                ItemLabels = labels,
                Period = period
            };
        }

        private void SaveBonus(BonusModel bonusModel)
        {
            const string query = @"INSERT INTO Bonus_General 
                (Worker_id, Period, Total, AmountA, AmountB, AmountC, AmountD, AmountE, AmountF, AmountG, AmountH, AmountI, AmountJ, CommentA, CommentB, CommentC, CommentD, CommentE, CommentF, CommentG, CommentH, CommentI, CommentJ) 
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            _databaseService.ExecuteNonQuery(query,
                Worker.Id,
                bonusModel.Period,
                bonusModel.Total,
                bonusModel.Amounts[0], bonusModel.Amounts[1], bonusModel.Amounts[2], bonusModel.Amounts[3], bonusModel.Amounts[4],
                bonusModel.Amounts[5], bonusModel.Amounts[6], bonusModel.Amounts[7], bonusModel.Amounts[8], bonusModel.Amounts[9],
                bonusModel.Comments[0], bonusModel.Comments[1], bonusModel.Comments[2], bonusModel.Comments[3], bonusModel.Comments[4],
                bonusModel.Comments[5], bonusModel.Comments[6], bonusModel.Comments[7], bonusModel.Comments[8], bonusModel.Comments[9]);
        }

        private void Report(string message, bool isError = false)
        {
            IsStatusError = isError;
            StatusMessage = message;
        }
        #endregion
    }
}
