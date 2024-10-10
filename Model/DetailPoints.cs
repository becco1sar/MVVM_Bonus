using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MVVM_Bonus.Model
{
    public abstract class DetailPoints : ObservableObject
    {
        static ObservableCollection<string> _listDetailPoints;

        public ObservableCollection<string> ListDetailPoints 
        {
            get
            {            
                return  _listDetailPoints;
            }
            set
            {
                _listDetailPoints = value;
                OnPropertyChanged(nameof(ListDetailPoints));
            }
            
        }

        public static ObservableCollection<string> GenerateDetailPoints(string path)
        {
            var listDetailPoints = new ObservableCollection<string>();
            var files = Directory.GetFiles($@"{path}", "*.txt", SearchOption.TopDirectoryOnly);
            if (files.Count() == 0)
            {
                MessageBox.Show("No files found");
                return null;
                    
            }
            for (int i = 0; i < files.Count(); i++)
                listDetailPoints.Add(File.ReadAllText(files[i]));
            return listDetailPoints;   

        }
    }
}
