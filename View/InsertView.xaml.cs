using MVVM_Bonus.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MVVM_Bonus.View
{
	public partial class InsertView : UserControl
	{
		private InsertViewModel _viewModel;

		public InsertView()
		{
			InitializeComponent();
			Loaded += InsertView_Loaded;
			Unloaded += InsertView_Unloaded;
		}

		private void InsertView_Loaded(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= ViewModel_PropertyChanged;

			_viewModel = DataContext as InsertViewModel;

			if (_viewModel != null)
				_viewModel.PropertyChanged += ViewModel_PropertyChanged;

			if (_viewModel != null && _viewModel.IsPreviewPanelOpen)
				OpenPreviewPanel();
			else
				ClosePreviewPanelVisual();
		}

		private void InsertView_Unloaded(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= ViewModel_PropertyChanged;
		}

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(InsertViewModel.IsPreviewPanelOpen))
			{
				Dispatcher.Invoke(() =>
				{
					if (_viewModel == null)
						return;

					if (_viewModel.IsPreviewPanelOpen)
						OpenPreviewPanel();
					else
						ClosePreviewPanelVisual();
				});
			}
		}

		private void OpenPreviewPanel()
		{
			PreviewPanelColumn.Width = new GridLength(650);
			FadePreviewPanel(1.0);
		}

		private void ClosePreviewPanelVisual()
		{
			PreviewPanelColumn.Width = new GridLength(0);
			FadePreviewPanel(0.0);
		}

		private void FadePreviewPanel(double toOpacity)
		{
			DoubleAnimation animation = new DoubleAnimation
			{
				To = toOpacity,
				Duration = new Duration(System.TimeSpan.FromMilliseconds(200))
			};

			PreviewPanel.BeginAnimation(OpacityProperty, animation);
		}

		private void ClosePreviewPanel(object sender, RoutedEventArgs e)
		{
			_viewModel?.ClosePreview();
		}
	}
}