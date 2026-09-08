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
        public string StatusText => $"{CompletedWorkers} / {TotalWorkers} Completed";
        public bool IsFullyCompleted => TotalWorkers > 0 && CompletedWorkers == TotalWorkers;
        public string ProgressColor => IsFullyCompleted ? "#4CAF50" : "#FF9800"; 
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
                _ = LoadStatisticsAsync();
            }
        }

        private string _exportStatus;
        public string ExportStatus
        {
            get { return _exportStatus; }
            set { _exportStatus = value; OnPropertyChanged(nameof(ExportStatus)); }
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
            }
        }

        public bool IsNotLoading => !IsLoading;

        private int _totalActiveWorkers;
        public int TotalActiveWorkers
        {
            get { return _totalActiveWorkers; }
            set { _totalActiveWorkers = value; OnPropertyChanged(nameof(TotalActiveWorkers)); }
        }

        private int _totalBonusesFound;
        public int TotalBonusesFound
        {
            get { return _totalBonusesFound; }
            set { _totalBonusesFound = value; OnPropertyChanged(nameof(TotalBonusesFound)); }
        }

        public ObservableCollection<TeamLeaderCompletionStatus> TeamLeaderStatuses { get; set; }

        public ICommand ExportExcelCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand ManageStaffCommand { get; private set; }
        public ICommand ConfigBonusCommand { get; private set; }

        public HrDashboardViewModel()
        {
            ExportExcelCommand = new RelayCommand(async x => await ExportToExcelAsync());
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
            ExportStatus = "Loading periods...";
            
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

                ExportStatus = "Ready.";
                if (AvailablePeriods.Count > 0)
                {
                    SelectedPeriod = AvailablePeriods[0];
                }
            }
            catch (Exception ex)
            {
                ExportStatus = "Error loading periods: " + ex.Message;
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
                foreach (var s in statuses)
                {
                    TeamLeaderStatuses.Add(s);
                    totalW += s.TotalWorkers;
                    totalB += s.CompletedWorkers;
                }
                
                TotalActiveWorkers = totalW;
                TotalBonusesFound = totalB;
            }
            catch (Exception ex)
            {
                ExportStatus = "Error loading stats: " + ex.Message;
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
                ExportStatus = "Please select a period.";
                return;
            }

            IsLoading = true;
            ExportStatus = "Generating Excel file...";
            
            try
            {
                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // Sanitize sheet name (Excel prohibits \ / ? * [ ] : and > 31 chars)
                        string safePeriod = Regex.Replace(SelectedPeriod, @"[\\/?*\[\]:]", "-");
                        if (safePeriod.Length > 20) safePeriod = safePeriod.Substring(0, 20);
                        
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

                ExportStatus = "Exported successfully!";
            }
            catch (Exception ex)
            {
                ExportStatus = "Export failed: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Logout(object obj)
        {
            Mediator.Notify(Constants.SIGN_OUT, "");
        }
    }
}
