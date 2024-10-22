using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus
{
    public class TeamLeaderModel : ObservableObject
    {
        private string tl_Name;
        private int tl_Id;

        public string Tl_Name
        {
            get
            {
                return tl_Name;
            }
            set
            {
                tl_Name = value;
                OnPropertyChanged(nameof(Tl_Name));
            }
        }
        public int Tl_Id
        {
            get
            {
                return tl_Id;
            }
            set
            {
                tl_Id = value;
                OnPropertyChanged(nameof(Tl_Id));
            }
        }
    }
}
