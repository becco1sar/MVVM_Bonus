using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Bonus.Model
{
    public class Person : ObservableObject
    {
        private string p_Name;
        private string p_Language;
        private string p_Role;
        private string p_ContractID;
        private int p_BVID;
        private int p_Id;
        private int p_TeamLeaderID;
        private string p_PathToContentCells;
        public string P_Name
        {
            get
            {
                return p_Name;
            }
            set
            {
                p_Name = value;
                OnPropertyChanged(nameof(P_Name));
            }
        }
        public string P_Role
        {
            get
            {
                return p_Role;
            }
            set
            {
                p_Role = value;
                OnPropertyChanged(nameof(p_Role));
            }
        }
        public string P_Contract
        {
            get
            {
                return p_ContractID;
            }
            set
            {
                p_ContractID = value;
                OnPropertyChanged(nameof(P_Contract));
            }
        }
        public int P_TeamLeaderID
        {
            get
            {
                return p_TeamLeaderID;
            }
            set
            {
                p_TeamLeaderID = value;
                OnPropertyChanged(nameof(p_TeamLeaderID));
            }
        }
        public int P_Id
        {
            get
            {
                return p_Id;
            }
            set
            {
                p_Id = value;
                OnPropertyChanged(nameof(P_Id));
            }
        }
        public int P_BVID 
        {
            get
            {
                return p_BVID;
            }
            set
            {
                p_BVID = value;
                OnPropertyChanged(nameof(P_BVID));
            } 
        }
        public string P_Language 
        {
            get
            {
                return p_Language;
            }
            set
            {
                p_Language = value;
                OnPropertyChanged(nameof(P_Language));
            }
        }
        public string P_PathToContentCells 
        {
            get
            {
                return p_PathToContentCells;
            }
            set
            {
                p_PathToContentCells = value;
                OnPropertyChanged(nameof(P_PathToContentCells));
            } 
        }
    }
}
