using System.ComponentModel;

namespace MVVM_Bonus
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propName)
        {
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}