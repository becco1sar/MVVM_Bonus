using System.Windows.Input;

namespace MVVM_Bonus
{
    public interface ICommonCommandsModel
    {
       
        public ICommand BackButtonCommand { get; }
        ICommand PrintButtonCommand { get; }
    }
}