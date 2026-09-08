using MVVM_Bonus.Services;

namespace MVVM_Bonus.Model
{
    public class AdditionalBonusCategory : ObservableObject
    {
        private string _categoryName;
        private decimal _amount;
        private string _comment;

        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); }
        }

        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }
    }
}
