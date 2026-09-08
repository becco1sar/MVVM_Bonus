using System.Windows;
using MVVM_Bonus.ViewModel;

namespace MVVM_Bonus
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            StateChanged += MainWindow_StateChanged;
        }

        /// <summary>
        /// Keeps the maximise button's glyph honest. It used to show the same square
        /// whether the window was maximised or not, so the only way to tell what the
        /// button would do was to press it.
        /// </summary>
        private void MainWindow_StateChanged(object sender, System.EventArgs e)
        {
            bool isMaximized = WindowState == WindowState.Maximized;

            MaximizeButton.Tag = FindResource(isMaximized ? "GlyphRestore" : "GlyphMaximize");
            MaximizeButton.ToolTip = isMaximized ? "Restore" : "Maximise";
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
