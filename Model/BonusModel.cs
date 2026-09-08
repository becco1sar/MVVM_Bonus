using System;
using System.Collections.Generic;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.Model
{
    public class BonusModel : ObservableObject
    {
        #region Fields
        private decimal _total;
        private string _workerName;
        private string _period; 
        private List<decimal> _amounts;
        private List<string> _comments;
        private List<string> _itemLabels;
        #endregion

        #region Properties
        public string WorkerName
        {
            get
            {
                return _workerName;
            }
            set
            {
                _workerName = value;
                OnPropertyChanged(nameof(WorkerName));
            }
        }
        public string Period 
        {
            get
            {
                return _period;
            }
            set
            {
                _period = value;
                OnPropertyChanged(nameof(Period));
            }
        }
        public decimal Total 
        {
            get
            {
                return _total;
            }
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            } 
        }
        public List<decimal> Amounts 
        {
            get
            {
                if (_amounts == null)
                    _amounts = new List<decimal>(); 
                return _amounts;
            }
            set
            {
                _amounts = value;
                OnPropertyChanged(nameof(Amounts));
            }
        }
        /// <summary>
        /// Text of each evaluation point, positionally matching <see cref="Amounts"/>.
        /// Without this the printed sheet an employee signs listed its rows as
        /// "Item #1" to "Item #10", because only the amounts were ever carried.
        /// </summary>
        public List<string> ItemLabels
        {
            get
            {
                if (_itemLabels == null)
                    _itemLabels = new List<string>();
                return _itemLabels;
            }
            set
            {
                _itemLabels = value;
                OnPropertyChanged(nameof(ItemLabels));
            }
        }

        public List<string> Comments 
        {
            get
            {
                if (_comments == null)
                    _comments = new List<string>();
                return _comments;
            }
            set
            {
                _comments = value;
                OnPropertyChanged(nameof(Comments));
            } 
        }
        #endregion
    }
}