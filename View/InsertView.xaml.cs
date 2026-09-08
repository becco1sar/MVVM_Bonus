using System.Windows.Controls;

namespace MVVM_Bonus.View
{
    /// <summary>
    /// Interaction logic for InsertView.xaml
    /// </summary>
    /// <remarks>
    /// Deliberately empty. This class used to mirror the view model's preview flag by
    /// hand, animating a grid column and subscribing to PropertyChanged on load and
    /// unsubscribing on unload. The panel's visibility is now bound directly, so
    /// there is no second copy of that state to keep in step.
    /// </remarks>
    public partial class InsertView : UserControl
    {
        public InsertView()
        {
            InitializeComponent();
        }
    }
}
