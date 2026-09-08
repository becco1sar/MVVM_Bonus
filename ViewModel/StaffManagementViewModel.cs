using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Linq;
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
        public int BvId { get; set; }

        public bool IsActive =>
            string.Equals(Active, "Yes", StringComparison.OrdinalIgnoreCase);

        /// <summary>Team leader name, filled in from the Teamleaders lookup.</summary>
        public string TeamleaderName { get; set; }

        public string StatusText => IsActive ? "Active" : "Inactive";

        /// <summary>
        /// Flags the records the old "Add as New" produced: an insert that set no
        /// Teamleader_id, leaving the employee invisible to every team leader.
        /// </summary>
        public bool IsUnassigned => TeamleaderId <= 0;
    }

    /// <summary>Row in the team leader picker.</summary>
    public class TeamLeaderOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class StaffManagementViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private ObservableCollection<WorkerEntity> _workers;
        private ObservableCollection<WorkerEntity> _filteredWorkers;
        private WorkerEntity _selectedWorker;
        private string _search;
        private string _editName;
        private string _editRole;
        private string _editLanguage;
        private bool _editActive;
        private TeamLeaderOption _editTeamLeader;
        private bool _isNewEmployee;
        private string _statusMessage;
        private bool _isStatusError;
        #endregion

        #region Properties
        public ObservableCollection<WorkerEntity> Workers
        {
            get => _workers;
            private set { _workers = value; OnPropertyChanged(nameof(Workers)); }
        }

        public ObservableCollection<WorkerEntity> FilteredWorkers
        {
            get => _filteredWorkers;
            private set
            {
                _filteredWorkers = value;
                OnPropertyChanged(nameof(FilteredWorkers));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }

        public ObservableCollection<TeamLeaderOption> TeamLeaders { get; private set; }

        /// <summary>
        /// Filters the directory. It lists every employee ever recorded, active or not,
        /// so without a search this screen is unusable past a couple of dozen people.
        /// </summary>
        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged(nameof(Search));
                ApplyFilter();
            }
        }

        public bool HasNoSearchResults =>
            (Workers?.Count ?? 0) > 0 && (FilteredWorkers?.Count ?? 0) == 0;

        public WorkerEntity SelectedWorker
        {
            get => _selectedWorker;
            set
            {
                _selectedWorker = value;
                OnPropertyChanged(nameof(SelectedWorker));
                OnPropertyChanged(nameof(HasEditor));
                OnPropertyChanged(nameof(EditorTitle));

                if (value != null)
                {
                    IsNewEmployee = false;
                    LoadIntoForm(value);
                }
            }
        }

        /// <summary>
        /// True while the form holds a new employee rather than an existing one.
        /// The old screen offered "Save Changes" and "Add as New" side by side with no
        /// indication of which record was in play, so the same click could update
        /// someone or create a duplicate.
        /// </summary>
        public bool IsNewEmployee
        {
            get => _isNewEmployee;
            private set
            {
                _isNewEmployee = value;
                OnPropertyChanged(nameof(IsNewEmployee));
                OnPropertyChanged(nameof(HasEditor));
                OnPropertyChanged(nameof(EditorTitle));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }

        public bool HasEditor => IsNewEmployee || SelectedWorker != null;

        public string EditorTitle => IsNewEmployee ? "New employee" : "Employee details";

        public string SaveButtonText => IsNewEmployee ? "Create employee" : "Save changes";

        public string EditName
        {
            get => _editName;
            set { _editName = value; OnPropertyChanged(nameof(EditName)); }
        }

        public string EditRole
        {
            get => _editRole;
            set { _editRole = value; OnPropertyChanged(nameof(EditRole)); }
        }

        public string EditLanguage
        {
            get => _editLanguage;
            set { _editLanguage = value; OnPropertyChanged(nameof(EditLanguage)); }
        }

        public bool EditActive
        {
            get => _editActive;
            set { _editActive = value; OnPropertyChanged(nameof(EditActive)); }
        }

        /// <summary>
        /// Required. Role and language decide which evaluation points an employee is
        /// scored on, and the team leader decides who can score them at all.
        /// </summary>
        public TeamLeaderOption EditTeamLeader
        {
            get => _editTeamLeader;
            set { _editTeamLeader = value; OnPropertyChanged(nameof(EditTeamLeader)); }
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
        public ICommand SaveCommand { get; private set; }
        public ICommand NewEmployeeCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        #endregion

        #region Constructors
        public StaffManagementViewModel()
        {
            SaveCommand = new RelayCommand(
                x => Save(),
                x => HasEditor && !string.IsNullOrWhiteSpace(EditName));

            NewEmployeeCommand = new RelayCommand(x => StartNewEmployee());
            CancelCommand = new RelayCommand(x => CancelEdit(), x => HasEditor);
            BackCommand = new RelayCommand(x => Mediator.Notify(Constants.HR_DASHBOARD_VIEW, ""));

            TeamLeaders = new ObservableCollection<TeamLeaderOption>();

            LoadTeamLeaders();
            LoadWorkers();
        }
        #endregion

        #region Methods
        private void Report(string message, bool isError = false)
        {
            IsStatusError = isError;
            StatusMessage = message;
        }

        private void LoadIntoForm(WorkerEntity worker)
        {
            EditName = worker.Name;
            EditRole = worker.Role;
            EditLanguage = worker.Language;
            EditActive = worker.IsActive;
            EditTeamLeader = TeamLeaders.FirstOrDefault(t => t.Id == worker.TeamleaderId);
            StatusMessage = null;
        }

        private void StartNewEmployee()
        {
            _selectedWorker = null;
            OnPropertyChanged(nameof(SelectedWorker));

            EditName = string.Empty;
            EditRole = string.Empty;
            EditLanguage = string.Empty;
            EditActive = true;
            EditTeamLeader = null;

            IsNewEmployee = true;
            Report(null);
        }

        private void CancelEdit()
        {
            IsNewEmployee = false;
            _selectedWorker = null;
            OnPropertyChanged(nameof(SelectedWorker));

            EditName = string.Empty;
            EditRole = string.Empty;
            EditLanguage = string.Empty;
            EditActive = false;
            EditTeamLeader = null;

            Report(null);
        }

        private void ApplyFilter()
        {
            if (Workers == null)
            {
                FilteredWorkers = new ObservableCollection<WorkerEntity>();
                return;
            }

            if (string.IsNullOrWhiteSpace(Search))
            {
                FilteredWorkers = new ObservableCollection<WorkerEntity>(Workers);
                return;
            }

            string term = Search.Trim();

            FilteredWorkers = new ObservableCollection<WorkerEntity>(
                Workers.Where(w =>
                    (w.Name != null && w.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (w.Role != null && w.Role.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (w.TeamleaderName != null && w.TeamleaderName.Contains(term, StringComparison.OrdinalIgnoreCase))));
        }

        private void LoadTeamLeaders()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT ID, Name FROM Teamleaders ORDER BY Name";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        TeamLeaders.Clear();
                        while (reader.Read())
                        {
                            TeamLeaders.Add(new TeamLeaderOption
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? "(unnamed)" : reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Report($"Could not load the team leader list: {ex.Message}", isError: true);
            }
        }

        private void LoadWorkers()
        {
            var loaded = new ObservableCollection<WorkerEntity>();

            try
            {
                Dictionary<int, string> leaderNames =
                    TeamLeaders.ToDictionary(t => t.Id, t => t.Name);

                using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
                {
                    conn.Open();
                    string query = "SELECT ID, Name, Active, Language, Role, Teamleader_id, BV_Id FROM Workers ORDER BY Name";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int tlId = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5));

                            loaded.Add(new WorkerEntity
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Active = reader.IsDBNull(2) ? "No" : reader.GetString(2),
                                Language = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Role = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                TeamleaderId = tlId,
                                BvId = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6)),
                                TeamleaderName = leaderNames.TryGetValue(tlId, out string name)
                                    ? name
                                    : null
                            });
                        }
                    }
                }

                Workers = loaded;
                ApplyFilter();
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
            catch (Exception ex)
            {
                Workers = loaded;
                ApplyFilter();
                Report($"Could not load the employee directory: {ex.Message}", isError: true);
            }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                Report("Enter the employee's name.", isError: true);
                return;
            }

            // A worker with no team leader never appears in anybody's list, so this is
            // required rather than optional. The previous insert omitted it entirely.
            if (EditTeamLeader == null)
            {
                Report("Choose a team leader. Without one the employee will not appear in any team leader's list.",
                       isError: true);
                return;
            }

            try
            {
                if (IsNewEmployee)
                    InsertWorker();
                else
                    UpdateWorker();
            }
            catch (Exception ex)
            {
                Report($"Save failed: {ex.Message}", isError: true);
            }
        }

        private void InsertWorker()
        {
            using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
            {
                conn.Open();
                string query =
                    "INSERT INTO Workers ([Name], [Role], [Language], [Active], [Teamleader_id]) "
                    + "VALUES (@name, @role, @language, @active, @tl)";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", EditName.Trim());
                    cmd.Parameters.AddWithValue("@role", EditRole ?? "");
                    cmd.Parameters.AddWithValue("@language", EditLanguage ?? "");
                    cmd.Parameters.AddWithValue("@active", EditActive ? "Yes" : "No");
                    cmd.Parameters.AddWithValue("@tl", EditTeamLeader.Id);
                    cmd.ExecuteNonQuery();
                }
            }

            string created = EditName.Trim();
            string leader = EditTeamLeader.Name;

            IsNewEmployee = false;
            LoadWorkers();

            SelectedWorker = Workers.FirstOrDefault(w =>
                string.Equals(w.Name, created, StringComparison.OrdinalIgnoreCase));

            Report($"Added {created} to {leader}'s team. Set their bonus profile in the database "
                   + "if they need values other than the default.");
        }

        private void UpdateWorker()
        {
            if (SelectedWorker == null)
            {
                Report("Select an employee first.", isError: true);
                return;
            }

            using (OleDbConnection conn = new OleDbConnection(Constants.SQL_CONNECTION_STRING))
            {
                conn.Open();
                string query =
                    "UPDATE Workers SET [Name] = @name, [Role] = @role, [Language] = @language, "
                    + "[Active] = @active, [Teamleader_id] = @tl WHERE ID = @id";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", EditName.Trim());
                    cmd.Parameters.AddWithValue("@role", EditRole ?? "");
                    cmd.Parameters.AddWithValue("@language", EditLanguage ?? "");
                    cmd.Parameters.AddWithValue("@active", EditActive ? "Yes" : "No");
                    cmd.Parameters.AddWithValue("@tl", EditTeamLeader.Id);
                    cmd.Parameters.AddWithValue("@id", SelectedWorker.Id);
                    cmd.ExecuteNonQuery();
                }
            }

            int savedId = SelectedWorker.Id;
            string savedName = EditName.Trim();

            LoadWorkers();
            SelectedWorker = Workers.FirstOrDefault(w => w.Id == savedId);

            Report($"Saved changes to {savedName}.");
        }
        #endregion
    }
}
