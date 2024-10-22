using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.ViewModel
{
    public class CommonViewModel : ObservableObject
    {
        private static ICommand _return;
        private static string _goToViewModel;
        private static IPageViewModel _previousViewModel;

        public static ICommand Return 
        {
            get
            {
                if (_return == null)
                    _return = new RelayCommand(x => Mediator.Notify(GoToViewModel, ""));
                return _return;
            }
        }
        public static string GoToViewModel 
        {
            get
            {
                _goToViewModel = "GoToGeneral";
                if (PreviousViewModel != null)
                {
                    if (PreviousViewModel.GetType().ToString() == "MVVM_Bonus.ViewModel.InsertViewModel")
                    {

                        _goToViewModel = "InsertPersonsBonusView";
                    }
                    else if (PreviousViewModel.GetType().ToString() == "MVVM_Bonus.ViewModel.GetBonusViewModel")
                    {
                        _goToViewModel = "GetPersonsBonusView";
                    }
                }
                


                return _goToViewModel;
            }
            set
            {
                _goToViewModel = value;
                
            } 
        }

        public static IPageViewModel PreviousViewModel { get => _previousViewModel; set => _previousViewModel = value; }
    }
}
