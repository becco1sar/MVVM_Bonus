using System;
using MVVM_Bonus.Services;

namespace MVVM_Bonus.Model
{
    public class Person : ObservableObject
    {
        private string _name;
        private string _language;
        private string _role;
        private string _contract;
        private int _bvId;
        private int _id;
        private int _teamLeaderId;
        private string _pathToContentCells;

        public string Name 
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public string Contract
        {
            get => _contract;
            set { _contract = value; OnPropertyChanged(); }
        }

        public int TeamLeaderId
        {
            get => _teamLeaderId;
            set { _teamLeaderId = value; OnPropertyChanged(); }
        }

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int BvId 
        {
            get => _bvId;
            set { _bvId = value; OnPropertyChanged(); }
        }

        public string Language 
        {
            get => _language;
            set { _language = value; OnPropertyChanged(); }
        }

        public string PathToContentCells 
        {
            get => _pathToContentCells;
            set { _pathToContentCells = value; OnPropertyChanged(); }
        }
    }
}
