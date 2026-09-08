using System.Windows;
using System.Windows.Controls;

namespace MVVM_Bonus.View
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += LoginView_Loaded;
        }

        /// <summary>
        /// Puts the caret in the only field on the screen, so signing in is type-then-Enter
        /// rather than click-then-type-then-click.
        /// </summary>
        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            UserNameBox.Focus();
            UserNameBox.SelectAll();
        }
    }
}
