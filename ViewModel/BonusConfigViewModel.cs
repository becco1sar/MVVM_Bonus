using System;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Windows.Input;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
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
                if (value != null)
                {
                    EditA = value.A; EditB = value.B; EditC = value.C; EditD = value.D; EditE = value.E;
                    EditF = value.F; EditG = value.G; EditH = value.H; EditI = value.I; EditJ = value.J;
                }
            }
        }

        private decimal _editA, _editB, _editC, _editD, _editE, _editF, _editG, _editH, _editI, _editJ;
        public decimal EditA { get => _editA; set { _editA = value; OnPropertyChanged(nameof(EditA)); } }
        public decimal EditB { get => _editB; set { _editB = value; OnPropertyChanged(nameof(EditB)); } }
        public decimal EditC { get => _editC; set { _editC = value; OnPropertyChanged(nameof(EditC)); } }
        public decimal EditD { get => _editD; set { _editD = value; OnPropertyChanged(nameof(EditD)); } }
        public decimal EditE { get => _editE; set { _editE = value; OnPropertyChanged(nameof(EditE)); } }
        public decimal EditF { get => _editF; set { _editF = value; OnPropertyChanged(nameof(EditF)); } }
        public decimal EditG { get => _editG; set { _editG = value; OnPropertyChanged(nameof(EditG)); } }
        public decimal EditH { get => _editH; set { _editH = value; OnPropertyChanged(nameof(EditH)); } }
        public decimal EditI { get => _editI; set { _editI = value; OnPropertyChanged(nameof(EditI)); } }
        public decimal EditJ { get => _editJ; set { _editJ = value; OnPropertyChanged(nameof(EditJ)); } }

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
        public ICommand BackCommand { get; private set; }

        private void Report(string message, bool isError = false)
        {
            IsStatusError = isError;
            StatusMessage = message;
        }

        public BonusConfigViewModel()
        {
            SaveCommand = new RelayCommand(SaveConfig, x => SelectedConfig != null);
            BackCommand = new RelayCommand(x => Mediator.Notify(Constants.HR_DASHBOARD_VIEW, ""));
            LoadConfigs();
        }

        private void LoadConfigs()
        {
            BonusConfigs = new ObservableCollection<BonusConfigEntity>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT ID, A, B, C, D, E, F, G, H, I, J FROM BV_General";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            BonusConfigs.Add(new BonusConfigEntity
                            {
                                Id = reader.GetInt32(0),
                                A = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                                B = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                                C = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                D = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                                E = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                F = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                G = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                                H = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                                I = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                                J = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Report($"Could not load the bonus profiles: {ex.Message}", isError: true);
            }
        }

        private void SaveConfig(object obj)
        {
            if (SelectedConfig == null)
            {
                Report("Select a profile before saving.", isError: true);
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
                LoadConfigs();
                Report($"Saved profile {savedId}.");
            }
            catch (Exception ex)
            {
                Report($"Save failed: {ex.Message}", isError: true);
            }
        }
    }
}
