using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using MVVM_Bonus.ViewModel;
using MVVM_Bonus.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class MainMenuViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        string _search;
        Person _selectedWorker;
        ObservableCollection<Person> _listPersons;
        ObservableCollection<Person> _filteredListPerson;
        ICommand _getPersonsBonus;
        ICommand _insertPersonsBonus;
        ICommand _signOutCommand;
        DataBaseService _databaseService;
        string _teamLeaderName;
        bool _isLoading;
        string _lastBonusPeriod;
        int _bonusCount;
        bool _isSummaryLoading;
        CancellationTokenSource _summaryCts;
        #endregion
        #region Properties
        /// <summary>Signed-in team leader, shown in the header so shared machines are unambiguous.</summary>
        public string TeamLeaderName
        {
            get => _teamLeaderName;
            private set { _teamLeaderName = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(); }
        }

        /// <summary>True when the team has loaded but is empty, so the list can say so.</summary>
        public bool HasNoWorkers => !IsLoading && ListPersons.Count == 0;

        /// <summary>
        /// True when a search is active but matches nothing. Distinct from
        /// <see cref="HasNoWorkers"/>: "no results for 'xyz'" and "no team members" are
        /// different problems and need different wording.
        /// </summary>
        public bool HasNoSearchResults =>
            !IsLoading
            && ListPersons.Count > 0
            && FilteredListPerson.Count == 0;

        public string Search
        {
            get => _search;
            set
            {
                if (_search != value)
                {
                    _search = value;
                    OnPropertyChanged();
                    ApplyFilter(_search);
                }
            }
        }
        public Person SelectedWorker
        {
            get => _selectedWorker;
            set
            {
                if (_selectedWorker == value)
                    return;

                _selectedWorker = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));

                LoadWorkerSummary(_selectedWorker);
            }
        }

        public bool HasSelection => SelectedWorker != null;

        /// <summary>
        /// Period of this employee's most recent bonus, or a plain "None yet".
        /// The card used to print a hardcoded "00/00/00" here, which looked like real
        /// data and told the team leader nothing.
        /// </summary>
        public string LastBonusPeriod
        {
            get => _lastBonusPeriod;
            private set { _lastBonusPeriod = value; OnPropertyChanged(); }
        }

        /// <summary>How many bonuses this employee already has on record.</summary>
        public int BonusCount
        {
            get => _bonusCount;
            private set { _bonusCount = value; OnPropertyChanged(); }
        }

        public bool IsSummaryLoading
        {
            get => _isSummaryLoading;
            private set { _isSummaryLoading = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Person> ListPersons
        {
            get
            {
                if (_listPersons == null)
                    _listPersons = new ObservableCollection<Person>();
                return _listPersons;
            }
            set
            {
                _listPersons = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Person> FilteredListPerson
        {
            get
            {
                if (_filteredListPerson == null)
                    _filteredListPerson = new ObservableCollection<Person>();
                return _filteredListPerson;
            }
            set
            {
                _filteredListPerson = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Opens the selected employee's bonus history. Disabled until someone is selected,
        /// which is why there is no longer a "please select a worker first" pop-up: an
        /// unavailable button explains itself before the click instead of after it.
        /// </summary>
        public ICommand GetPersonsBonus
        {
            get
            {
                if (_getPersonsBonus == null)
                {
                    _getPersonsBonus = new RelayCommand(
                        x =>
                        {
                            Mediator.Notify(Constants.GET_BONUS_VIEW, "");
                            Messenger.Default.Send(SelectedWorker, Constants.MESSENGER_GET_VIEW);
                        },
                        x => SelectedWorker != null);
                }
                return _getPersonsBonus;
            }
        }

        /// <summary>Starts a new bonus for the selected employee.</summary>
        public ICommand InsertPersonsBonus
        {
            get
            {
                if (_insertPersonsBonus == null)
                {
                    _insertPersonsBonus = new RelayCommand(
                        x =>
                        {
                            Mediator.Notify(Constants.INSERT_BONUS_VIEW, SelectedWorker);
                            Messenger.Default.Send(SelectedWorker, Constants.MESSENGER_INSERT_VIEW);
                        },
                        x => SelectedWorker != null);
                }
                return _insertPersonsBonus;
            }
        }

        /// <summary>
        /// Ends the session. Team leaders had no way out of this screen except closing the
        /// window, which left the next person signed in as them on a shared machine.
        /// </summary>
        public ICommand SignOutCommand
        {
            get
            {
                if (_signOutCommand == null)
                    _signOutCommand = new RelayCommand(x => Mediator.Notify(Constants.SIGN_OUT, ""));
                return _signOutCommand;
            }
        }
        #endregion
        #region Constructors
        public MainMenuViewModel()
        {
            Messenger.Default.Register<TeamLeaderModel>(this, Constants.MESSENGER_TEAMLEADER_IDENTIFICATION, action => GenerateWorkers(action));   
            _databaseService = new DataBaseService();
        }
        #endregion
        #region Methods
        private void ApplyFilter(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                FilteredListPerson = new ObservableCollection<Person>(ListPersons);
            }
            else
            {
                var matches = ListPersons
                    .Where(x => x.Name != null && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredListPerson = new ObservableCollection<Person>(matches);
            }

            OnPropertyChanged(nameof(HasNoWorkers));
            OnPropertyChanged(nameof(HasNoSearchResults));
        }

        private void ShowAll()
        {
            ApplyFilter(null);
        }
        private void GenerateWorkers(TeamLeaderModel tl)
        {
            if (tl == null) return;

            TeamLeaderName = tl.Name;
            IsLoading = true;

            try
            {
                // A reload means a different team: drop the previous selection and search
                // so the profile card cannot keep showing someone who is no longer listed.
                SelectedWorker = null;
                _search = string.Empty;
                OnPropertyChanged(nameof(Search));

                ListPersons.Clear();
                var table = _databaseService.ExecuteQuery(Constants.SQL_GET_WORKER_QUERY, tl.Id, Constants.ACTIVE_ROWS);
                foreach (DataRow row in table.Rows)
                {
                    Person person = new Person
                    {
                        Id = Convert.ToInt32(row[Constants.SQL_ID_COLUMN_NAME]),
                        Name = Convert.ToString(row[Constants.SQL_NAME_COLUMN_NAME]),
                        Language = Convert.ToString(row[Constants.SQL_LANGUAGE_COLUMN_NAME]),
                        Role = Convert.ToString(row[Constants.SQL_ROLE_COLUMN_NAME]),
                        Contract = Convert.ToString(row[Constants.SQL_CONTRACT_ID_COLUMN_NAME]),
                        TeamLeaderId = tl.Id,
                        BvId = Convert.ToInt32(row[Constants.SQL_BV_ID_COLUMN_NAME])
                    };
                    person.PathToContentCells = GetAttentionPoint(person);
                    ListPersons.Add(person);
                }
                ShowAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load your team.\n\n{ex.Message}",
                    "Connection problem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoWorkers));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
        
        /// <summary>
        /// Fetches the selected employee's bonus history summary off the UI thread.
        /// The database sits on a network share, so running this inline would stall the
        /// window every time the user moved through the list with the arrow keys.
        /// A previous in-flight request is cancelled so fast scrolling cannot let an
        /// older reply overwrite a newer one.
        /// </summary>
        private void LoadWorkerSummary(Person person)
        {
            _summaryCts?.Cancel();

            if (person == null)
            {
                LastBonusPeriod = string.Empty;
                BonusCount = 0;
                IsSummaryLoading = false;
                return;
            }

            var cts = new CancellationTokenSource();
            _summaryCts = cts;

            int workerId = person.Id;
            IsSummaryLoading = true;
            LastBonusPeriod = string.Empty;
            BonusCount = 0;

            Task.Run(() =>
            {
                try
                {
                    var table = _databaseService.ExecuteQuery(
                        "SELECT Period FROM Bonus_General WHERE Worker_id = ? ORDER BY ID DESC",
                        workerId);

                    int count = table.Rows.Count;
                    string latest = count > 0
                        ? Convert.ToString(table.Rows[0]["Period"])
                        : null;

                    return new WorkerSummary(count, latest, false);
                }
                catch
                {
                    // A summary is a nicety; a failed lookup must not interrupt the screen.
                    return new WorkerSummary(0, null, true);
                }
            }, cts.Token)
            .ContinueWith(task =>
            {
                if (cts.IsCancellationRequested || task.IsFaulted || task.IsCanceled)
                    return;

                WorkerSummary summary = task.Result;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Another selection has happened since; that request owns the fields now.
                    if (!ReferenceEquals(_summaryCts, cts))
                        return;

                    IsSummaryLoading = false;
                    BonusCount = summary.Count;
                    LastBonusPeriod = summary.Failed
                        ? "Unavailable"
                        : (string.IsNullOrWhiteSpace(summary.LatestPeriod) ? "None yet" : summary.LatestPeriod);
                });
            });
        }

        /// <summary>Result of one background bonus-history lookup.</summary>
        private readonly struct WorkerSummary
        {
            public WorkerSummary(int count, string latestPeriod, bool failed)
            {
                Count = count;
                LatestPeriod = latestPeriod;
                Failed = failed;
            }

            public int Count { get; }
            public string LatestPeriod { get; }
            public bool Failed { get; }
        }

        private string GetAttentionPoint(Person person)
        {
            try
            {
                string query = "SELECT [Path] FROM Contentcell WHERE Worker_Role = ? AND Worker_Language = ?";
                var table = _databaseService.ExecuteQuery(query, person.Role, person.Language);
                if (table.Rows.Count > 0)
                {
                    return Convert.ToString(table.Rows[0]["Path"]);
                }
            }
            catch
            {
                // Fallback
            }
            return string.Empty;                   
        }
        #endregion
    }
}