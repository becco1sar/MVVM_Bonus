using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Input;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    /// <summary>
    /// One row of BV_General: the maximum amount payable for each of the ten
    /// evaluation points, for the group of employees pointed at it by Workers.BV_Id.
    /// </summary>
    public class BonusConfigEntity
    {
        public int Id { get; set; }
        public decimal A { get; set; }
        public decimal B { get; set; }
        public decimal C { get; set; }
        public decimal D { get; set; }
        public decimal E { get; set; }
        public decimal F { get; set; }
        public decimal G { get; set; }
        public decimal H { get; set; }
        public decimal I { get; set; }
        public decimal J { get; set; }

        /// <summary>Active employees currently on this profile.</summary>
        public int WorkerCount { get; set; }

        public string DisplayName => $"Profile {Id}";

        /// <summary>
        /// Who this profile affects. "Profile ID: 1" on its own told an HR user
        /// nothing about what changing it would do.
        /// </summary>
        public string UsageText =>
            WorkerCount == 1
                ? "1 employee"
                : $"{WorkerCount} employees";

        /// <summary>Points 1-3, which must all be awarded before 4-10 unlock.</summary>
        public decimal BaseTotal => A + B + C;

        public decimal AdditionalTotal => D + E + F + G + H + I + J;

        public decimal Total => BaseTotal + AdditionalTotal;
    }

    public class BonusConfigViewModel : ObservableObject, IPageViewModel
    {
        public ObservableCollection<BonusConfigEntity> BonusConfigs { get; set; }

        private BonusConfigEntity _selectedConfig;
        public BonusConfigEntity SelectedConfig
        {
            get => _selectedConfig;
            set
            {
                _selectedConfig = value;
                OnPropertyChanged(nameof(SelectedConfig));
                OnPropertyChanged(nameof(HasSelection));

                if (value != null)
                {
                    EditA = value.A; EditB = value.B; EditC = value.C; EditD = value.D; EditE = value.E;
                    EditF = value.F; EditG = value.G; EditH = value.H; EditI = value.I; EditJ = value.J;
                }

                StatusMessage = null;
            }
        }

        public bool HasSelection => SelectedConfig != null;

        public bool HasNoConfigs => (BonusConfigs?.Count ?? 0) == 0;

        private decimal _editA, _editB, _editC, _editD, _editE, _editF, _editG, _editH, _editI, _editJ;

        public decimal EditA { get => _editA; set { _editA = value; OnEditChanged(nameof(EditA)); } }
        public decimal EditB { get => _editB; set { _editB = value; OnEditChanged(nameof(EditB)); } }
        public decimal EditC { get => _editC; set { _editC = value; OnEditChanged(nameof(EditC)); } }
        public decimal EditD { get => _editD; set { _editD = value; OnEditChanged(nameof(EditD)); } }
        public decimal EditE { get => _editE; set { _editE = value; OnEditChanged(nameof(EditE)); } }
        public decimal EditF { get => _editF; set { _editF = value; OnEditChanged(nameof(EditF)); } }
        public decimal EditG { get => _editG; set { _editG = value; OnEditChanged(nameof(EditG)); } }
        public decimal EditH { get => _editH; set { _editH = value; OnEditChanged(nameof(EditH)); } }
        public decimal EditI { get => _editI; set { _editI = value; OnEditChanged(nameof(EditI)); } }
        public decimal EditJ { get => _editJ; set { _editJ = value; OnEditChanged(nameof(EditJ)); } }

        /// <summary>
        /// Live totals while editing. Ten disconnected boxes gave no sense of the
        /// figure they add up to, which is the number HR actually cares about.
        /// </summary>
        public decimal EditBaseTotal => EditA + EditB + EditC;

        public decimal EditAdditionalTotal =>
            EditD + EditE + EditF + EditG + EditH + EditI + EditJ;

        public decimal EditTotal => EditBaseTotal + EditAdditionalTotal;

        private string _statusMessage;

        /// <summary>Result of the last save; previously swallowed by an empty catch block.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private bool _isStatusError;
        public bool IsStatusError
        {
            get => _isStatusError;
            private set { _isStatusError = value; OnPropertyChanged(nameof(IsStatusError)); }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand RevertCommand { get; private set; }
        public ICommand BackCommand { get; private set; }

        public BonusConfigViewModel()
        {
            SaveCommand = new RelayCommand(SaveConfig, x => SelectedConfig != null);
            RevertCommand = new RelayCommand(x => RevertEdits(), x => SelectedConfig != null);
            BackCommand = new RelayCommand(x => Mediator.Notify(Constants.HR_DASHBOARD_VIEW, ""));
            LoadConfigs();
        }

        /// <summary>Restores the ten boxes to the values currently stored for the profile.</summary>
        private void RevertEdits()
        {
            BonusConfigEntity config = SelectedConfig;
            if (config == null)
                return;

            EditA = config.A; EditB = config.B; EditC = config.C; EditD = config.D; EditE = config.E;
            EditF = config.F; EditG = config.G; EditH = config.H; EditI = config.I; EditJ = config.J;

            Report("Reverted to the saved values.");
        }

        private void OnEditChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
            OnPropertyChanged(nameof(EditBaseTotal));
            OnPropertyChanged(nameof(EditAdditionalTotal));
            OnPropertyChanged(nameof(EditTotal));
        }

        private void Report(string message, bool isError = false)
        {
            IsStatusError = isError;
            StatusMessage = message;
        }

        private void LoadConfigs()
        {
            BonusConfigs = new ObservableCollection<BonusConfigEntity>();
            try
            {
                Dictionary<int, int> usage = LoadProfileUsage();

                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT ID, A, B, C, D, E, F, G, H, I, J FROM BV_General";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);

                            BonusConfigs.Add(new BonusConfigEntity
                            {
                                Id = id,
                                A = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                                B = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                                C = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                D = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                                E = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                F = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                G = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                                H = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                                I = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                                J = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10),
                                WorkerCount = usage.TryGetValue(id, out int count) ? count : 0
                            });
                        }
                    }
                }

                OnPropertyChanged(nameof(BonusConfigs));
                OnPropertyChanged(nameof(HasNoConfigs));
            }
            catch (Exception ex)
            {
                Report($"Could not load the bonus profiles: {ex.Message}", isError: true);
            }
        }

        /// <summary>
        /// Counts active employees per profile so each row can say who it affects.
        /// A failure here costs only the count, so it is not reported as an error.
        /// </summary>
        private Dictionary<int, int> LoadProfileUsage()
        {
            var usage = new Dictionary<int, int>();

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT BV_Id, COUNT(*) FROM Workers WHERE Active = 'Yes' GROUP BY BV_Id";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0))
                                continue;

                            usage[Convert.ToInt32(reader.GetValue(0))] = Convert.ToInt32(reader.GetValue(1));
                        }
                    }
                }
            }
            catch
            {
                // Counts are informational only.
            }

            return usage;
        }

        private void SaveConfig(object obj)
        {
            if (SelectedConfig == null)
            {
                Report("Select a profile before saving.", isError: true);
                return;
            }

            if (new[] { EditA, EditB, EditC, EditD, EditE, EditF, EditG, EditH, EditI, EditJ }
                    .Any(v => v < 0))
            {
                Report("Bonus amounts cannot be negative.", isError: true);
                return;
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "UPDATE BV_General SET A=@A, B=@B, C=@C, D=@D, E=@E, F=@F, G=@G, H=@H, I=@I, J=@J WHERE ID=@id";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@A", EditA); cmd.Parameters.AddWithValue("@B", EditB);
                        cmd.Parameters.AddWithValue("@C", EditC); cmd.Parameters.AddWithValue("@D", EditD);
                        cmd.Parameters.AddWithValue("@E", EditE); cmd.Parameters.AddWithValue("@F", EditF);
                        cmd.Parameters.AddWithValue("@G", EditG); cmd.Parameters.AddWithValue("@H", EditH);
                        cmd.Parameters.AddWithValue("@I", EditI); cmd.Parameters.AddWithValue("@J", EditJ);
                        cmd.Parameters.AddWithValue("@id", SelectedConfig.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                int savedId = SelectedConfig.Id;
                int affected = SelectedConfig.WorkerCount;

                LoadConfigs();
                SelectedConfig = BonusConfigs.FirstOrDefault(c => c.Id == savedId);

                Report(affected > 0
                    ? $"Saved profile {savedId}. Applies to {affected} active "
                      + (affected == 1 ? "employee" : "employees") + " from the next bonus onward."
                    : $"Saved profile {savedId}.");
            }
            catch (Exception ex)
            {
                Report($"Save failed: {ex.Message}", isError: true);
            }
        }
    }
}
