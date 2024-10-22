using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.Model
{
    public class BonusDetailModel : ObservableObject
    {
        private string _detailPoint;
        private decimal _amount;
        private string _comment;

        public string DetailPoint 
        {
            get 
            {
                return _detailPoint;
            }
            set
            {
                _detailPoint = value;
                OnPropertyChanged(nameof(_detailPoint));
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
