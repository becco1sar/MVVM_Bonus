using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using ClosedXML.Excel;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class TeamLeaderCompletionStatus
    {
        public string TeamleaderName { get; set; }
        public int TotalWorkers { get; set; }
        public int CompletedWorkers { get; set; }

        public string StatusText => $"{CompletedWorkers} / {TotalWorkers}";

        public bool IsFullyCompleted => TotalWorkers > 0 && CompletedWorkers == TotalWorkers;

        /// <summary>0-1, for the row's progress bar.</summary>
        public double ProgressFraction =>
            TotalWorkers > 0 ? (double)CompletedWorkers / TotalWorkers : 0d;

        /// <summary>Percentage for display, e.g. "71%".</summary>
        public string ProgressPercentText =>
            TotalWorkers > 0
                ? ((int)Math.Round(ProgressFraction * 100)) + "%"
                : "—";

        /// <summary>
        /// What is still outstanding. The old row said only "3 / 7 Completed", which
        /// left the reader to work out that four people were missing.
        /// </summary>
        public string RemainingText
        {
            get
            {
                if (TotalWorkers == 0)
                    return "No active employees";

                int remaining = TotalWorkers - CompletedWorkers;

                if (remaining <= 0)
                    return "All bonuses submitted";

                return remaining == 1
                    ? "1 employee still missing a bonus"
                    : $"{remaining} employees still missing a bonus";
            }
        }

        // Colour deliberately absent: whether a row reads as done or outstanding is a
        // presentation decision, driven from IsFullyCompleted against the design
        // tokens in the view. A view model returning "#4CAF50" put two theme colours
        // outside the one place that defines them.
    }

    public class HrDashboardViewModel : ObservableObject, IPageViewModel
    {
        private string _selectedPeriod;
        public ObservableCollection<string> AvailablePeriods { get; set; }

        public string SelectedPeriod
        {
            get { return _selectedPeriod; }
            set 
            { 
                _selectedPeriod = value;
                OnPropertyChanged(nameof(SelectedPeriod));
                OnPropertyChanged(nameof(IsPeriodChosen));
                _ = LoadStatisticsAsync();
            }
        }

        private string _exportStatus;
        public string ExportStatus
        {
            get { return _exportStatus; }
            set { _exportStatus = value; OnPropertyChanged(nameof(ExportStatus)); }
        }

        private void SetStatus(string message, bool isError = false)
        {
            IsStatusError = isError;
            ExportStatus = message;
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set 
            { 
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsNotLoading));
                OnPropertyChanged(nameof(HasNoStatuses));
            }
        }

        public bool IsNotLoading => !IsLoading;

        private int _totalActiveWorkers;
        public int TotalActiveWorkers
        {
            get { return _totalActiveWorkers; }
            set
            {
                _totalActiveWorkers = value;
                OnPropertyChanged(nameof(TotalActiveWorkers));
                OnPropertyChanged(nameof(OverallFraction));
                OnPropertyChanged(nameof(OverallPercentText));
            }
        }

        private int _totalBonusesFound;
        public int TotalBonusesFound
        {
            get { return _totalBonusesFound; }
            set
            {
                _totalBonusesFound = value;
                OnPropertyChanged(nameof(TotalBonusesFound));
                OnPropertyChanged(nameof(OverallFraction));
                OnPropertyChanged(nameof(OverallPercentText));
            }
        }

        public ObservableCollection<TeamLeaderCompletionStatus> TeamLeaderStatuses { get; set; }

        /// <summary>Share of all active employees who already have a bonus this period.</summary>
        public double OverallFraction =>
            TotalActiveWorkers > 0 ? (double)TotalBonusesFound / TotalActiveWorkers : 0d;

        public string OverallPercentText =>
            TotalActiveWorkers > 0
                ? ((int)Math.Round(OverallFraction * 100)) + "%"
                : "—";

        /// <summary>Team leaders with at least one employee still outstanding.</summary>
        public int PendingTeamLeaderCount =>
            TeamLeaderStatuses?.Count(s => !s.IsFullyCompleted) ?? 0;

        public bool IsPeriodChosen => !string.IsNullOrEmpty(SelectedPeriod);

        public bool HasNoStatuses => !IsLoading && (TeamLeaderStatuses?.Count ?? 0) == 0;

        private bool _isStatusError;

        /// <summary>True when <see cref="ExportStatus"/> is reporting a failure.</summary>
        public bool IsStatusError
        {
            get { return _isStatusError; }
            private set { _isStatusError = value; OnPropertyChanged(nameof(IsStatusError)); }
        }

        public ICommand ExportExcelCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand ManageStaffCommand { get; private set; }
        public ICommand ConfigBonusCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public HrDashboardViewModel()
        {
            ExportExcelCommand = new RelayCommand(
                async x => await ExportToExcelAsync(),
                x => IsNotLoading && IsPeriodChosen);
            RefreshCommand = new RelayCommand(
                async x => await LoadStatisticsAsync(),
                x => IsNotLoading && IsPeriodChosen);
            LogoutCommand = new RelayCommand(Logout);
            ManageStaffCommand = new RelayCommand(x => Mediator.Notify(Constants.STAFF_MANAGEMENT_VIEW, ""));
            ConfigBonusCommand = new RelayCommand(x => Mediator.Notify(Constants.BONUS_CONFIG_VIEW, ""));
            AvailablePeriods = new ObservableCollection<string>();
            TeamLeaderStatuses = new ObservableCollection<TeamLeaderCompletionStatus>();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            IsLoading = true;
            SetStatus("Loading periods…");
            
            try
            {
                var periods = await Task.Run(() =>
                {
                    var list = new List<string>();
                    using (OleDbConnection connection = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                    {
                        connection.Open();
                        string query = "SELECT DISTINCT Period FROM [Bonus Workers Query] ORDER BY Period DESC";
                        using (OleDbCommand command = new OleDbCommand(query, connection))
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    list.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    return list;
                });

                AvailablePeriods.Clear();
                foreach (var p in periods) AvailablePeriods.Add(p);

                SetStatus(string.Empty);
                if (AvailablePeriods.Count > 0)
                {
                    SelectedPeriod = AvailablePeriods[0];
                }
            }
            catch (Exception ex)
            {
                SetStatus("Could not load the bonus periods: " + ex.Message, isError: true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            if (string.IsNullOrEmpty(SelectedPeriod)) return;

            IsLoading = true;
            try
            {
                var statuses = await Task.Run(() =>
                {
                    var result = new List<TeamLeaderCompletionStatus>();
                    
                    // 1. Get all active workers and their teamleaders
                    var workers = new List<(int WorkerId, int TlId, string TlName)>();
                    using (OleDbConnection connection = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                    {
                        connection.Open();
                        string queryWorkers = @"SELECT w.ID, t.ID, t.Name FROM Workers w INNER JOIN Teamleaders t ON w.Teamleader_id = t.ID WHERE w.Active = 'Yes'";
                        using (OleDbCommand cmd = new OleDbCommand(queryWorkers, connection))
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                workers.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
                            }
                        }

                        // 2. Get all worker IDs that have a bonus for the selected period
                        var workersWithBonus = new HashSet<int>();
                        string queryBonuses = "SELECT Worker_id FROM Bonus_General WHERE Period = @period";
                        using (OleDbCommand cmd = new OleDbCommand(queryBonuses, connection))
                        {
                            cmd.Parameters.AddWithValue("@period", SelectedPeriod);
                            using (OleDbDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        workersWithBonus.Add(reader.GetInt32(0));
                                }
                            }
                        }

                        // 3. Aggregate data
                        var grouped = workers.GroupBy(w => new { w.TlId, w.TlName });
                        foreach (var group in grouped)
                        {
                            result.Add(new TeamLeaderCompletionStatus
                            {
                                TeamleaderName = group.Key.TlName,
                                TotalWorkers = group.Count(),
                                CompletedWorkers = group.Count(w => workersWithBonus.Contains(w.WorkerId))
                            });
                        }
                    }
                    return result;
                });

                TeamLeaderStatuses.Clear();
                int totalW = 0;
                int totalB = 0;

                // Least complete first: this list exists so HR knows who to chase.
                foreach (var s in statuses
                            .OrderBy(x => x.IsFullyCompleted)
                            .ThenBy(x => x.ProgressFraction)
                            .ThenBy(x => x.TeamleaderName))
                {
                    TeamLeaderStatuses.Add(s);
                    totalW += s.TotalWorkers;
                    totalB += s.CompletedWorkers;
                }
                
                TotalActiveWorkers = totalW;
                TotalBonusesFound = totalB;
                OnPropertyChanged(nameof(PendingTeamLeaderCount));
                OnPropertyChanged(nameof(HasNoStatuses));
            }
            catch (Exception ex)
            {
                SetStatus("Could not load completion figures: " + ex.Message, isError: true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportToExcelAsync()
        {
            if (string.IsNullOrEmpty(SelectedPeriod))
            {
                SetStatus("Choose a period first.", isError: true);
                return;
            }

            IsLoading = true;
            SetStatus("Building the Excel file…");
            
            try
            {
                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        string safePeriod = SanitizePeriod(SelectedPeriod);
                        
                        var worksheet = workbook.Worksheets.Add("Bonuses " + safePeriod);
                        
                        using (OleDbConnection connection = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                        {
                            connection.Open();
                            string query = "SELECT * FROM [Bonus Workers Query] WHERE Period = @period";
                            using (OleDbCommand command = new OleDbCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@period", SelectedPeriod);
                                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                                {
                                    DataTable dt = new DataTable();
                                    adapter.Fill(dt);
                                    
                                    worksheet.Cell(1, 1).InsertTable(dt);
                                    worksheet.Columns().AdjustToContents();
                                }
                            }
                        }

                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string fileName = $"Bonus_Export_{safePeriod}.xlsx";
                        string filePath = Path.Combine(desktopPath, fileName);
                        
                        workbook.SaveAs(filePath);
                        
                        // Open the file on the UI thread side effects can happen if not careful, 
                        // but Process.Start is fine from background thread
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                });

                SetStatus($"Saved to your desktop as Bonus_Export_{SanitizePeriod(SelectedPeriod)}.xlsx and opened.");
            }
            catch (Exception ex)
            {
                SetStatus("Export failed: " + ex.Message, isError: true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Excel rejects \ / ? * [ ] : in sheet names and caps them at 31 characters.
        /// Shared with the status message so it can name the file that was written.
        /// </summary>
        private static string SanitizePeriod(string period)
        {
            if (string.IsNullOrEmpty(period))
                return "export";

            string safe = Regex.Replace(period, @"[\\/?*\[\]:]", "-");
            return safe.Length > 20 ? safe.Substring(0, 20) : safe;
        }

        private void Logout(object obj)
        {
            Mediator.Notify(Constants.SIGN_OUT, "");
        }
    }
}
