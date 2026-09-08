using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MVVM_Bonus.Services;

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

        /// <summary>
        /// Reads the evaluation points for one role and language from their folder.
        /// Returns null when the folder is missing or empty.
        /// </summary>
        /// <remarks>
        /// This used to raise a bare "No files found" MessageBox from inside a model
        /// class, with no indication of which employee or folder was meant, and then
        /// returned null anyway. Reporting is left to the caller, which knows the
        /// context; the missing-directory case no longer throws.
        /// </remarks>
        public static ObservableCollection<string> GenerateDetailPoints(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return null;

            string[] files = Directory.GetFiles(path, "*.txt", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
                return null;

            var listDetailPoints = new ObservableCollection<string>();

            foreach (string file in files)
                listDetailPoints.Add(File.ReadAllText(file).Trim());

            return listDetailPoints;
        }
    }
}
