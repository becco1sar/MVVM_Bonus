using System;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Windows.Input;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class WorkerEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Active { get; set; }
        public string Language { get; set; }
        public string Role { get; set; }
        public int TeamleaderId { get; set; }
    }

    public class StaffManagementViewModel : ObservableObject, IPageViewModel
    {
        private ObservableCollection<WorkerEntity> _workers;
        public ObservableCollection<WorkerEntity> Workers
        {
            get => _workers;
            set { _workers = value; OnPropertyChanged(nameof(Workers)); }
        }

        private WorkerEntity _selectedWorker;
        public WorkerEntity SelectedWorker
        {
            get => _selectedWorker;
            set 
            { 
                _selectedWorker = value; 
                OnPropertyChanged(nameof(SelectedWorker));
                if (value != null)
                {
                    EditName = value.Name;
                    EditRole = value.Role;
                    EditActive = value.Active == "Yes";
                }
            }
        }

        private string _editName;
        public string EditName { get => _editName; set { _editName = value; OnPropertyChanged(nameof(EditName)); } }

        private string _editRole;
        public string EditRole { get => _editRole; set { _editRole = value; OnPropertyChanged(nameof(EditRole)); } }

        private bool _editActive;
        public bool EditActive { get => _editActive; set { _editActive = value; OnPropertyChanged(nameof(EditActive)); } }

        public ICommand SaveCommand { get; private set; }
        public ICommand AddNewCommand { get; private set; }
        public ICommand BackCommand { get; private set; }

        public StaffManagementViewModel()
        {
            SaveCommand = new RelayCommand(SaveWorker);
            AddNewCommand = new RelayCommand(AddNewWorker);
            BackCommand = new RelayCommand(GoBack);
            LoadWorkers();
        }

        private void LoadWorkers()
        {
            Workers = new ObservableCollection<WorkerEntity>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT ID, Name, Active, Language, Role, Teamleader_id FROM Workers ORDER BY Name";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Workers.Add(new WorkerEntity
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Active = reader.IsDBNull(2) ? "No" : reader.GetString(2),
                                Language = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Role = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                TeamleaderId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveWorker(object obj)
        {
            if (SelectedWorker == null || string.IsNullOrWhiteSpace(EditName)) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "UPDATE Workers SET [Name] = @name, [Role] = @role, [Active] = @active WHERE ID = @id";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", EditName);
                        cmd.Parameters.AddWithValue("@role", EditRole ?? "");
                        cmd.Parameters.AddWithValue("@active", EditActive ? "Yes" : "No");
                        cmd.Parameters.AddWithValue("@id", SelectedWorker.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadWorkers();
            }
            catch { }
        }

        private void AddNewWorker(object obj)
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "INSERT INTO Workers ([Name], [Role], [Active]) VALUES (@name, @role, @active)";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", EditName);
                        cmd.Parameters.AddWithValue("@role", EditRole ?? "");
                        cmd.Parameters.AddWithValue("@active", EditActive ? "Yes" : "No");
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadWorkers();
            }
            catch { }
        }

        private void GoBack(object obj)
        {
            Mediator.Notify(Constants.HR_DASHBOARD_VIEW, "");
        }
    }
}
