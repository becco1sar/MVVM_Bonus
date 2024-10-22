using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.Model
{
    public class BonusValuesModel : ObservableObject
    {

        private string _item;
        private decimal _amount;
        private bool _isSelected;
        private bool _isEnabled;
        private string _comment;

        public string Item 
        {
            get
            {
                return _item;
            }
            set
            {
                _item = value;
                OnPropertyChanged(nameof(Item));
            }
        }
        public decimal Amount 
        {
            get
            {
                return _amount;
            }
            set
            {
                _amount = value;
                OnPropertyChanged(nameof(Amount));
            }
        }
        public bool IsSelected 
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
        public bool IsEnabled 
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            } 
        }

        public string Comment 
        {
            get
            { 
                return _comment;
            }
            set
            {
                _comment = value;
                OnPropertyChanged(nameof(Comment));
            }
        }
    }
}
