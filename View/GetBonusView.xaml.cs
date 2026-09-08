using MVVM_Bonus.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MVVM_Bonus.View
{
	public partial class GetBonusView : UserControl
	{
		private BonusViewModel _viewModel;

		public GetBonusView()
		{
			InitializeComponent();
			Loaded += GetBonusView_Loaded;
			Unloaded += GetBonusView_Unloaded;
		}

		private void GetBonusView_Loaded(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= ViewModel_PropertyChanged;

			_viewModel = DataContext as BonusViewModel;

			if (_viewModel != null)
				_viewModel.PropertyChanged += ViewModel_PropertyChanged;

			if (_viewModel != null && _viewModel.IsDetailPanelOpen)
				OpenDetailPanelVisual();
			else
				CloseDetailPanelVisual();
		}

		private void GetBonusView_Unloaded(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= ViewModel_PropertyChanged;
		}

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BonusViewModel.IsDetailPanelOpen))
			{
				Dispatcher.Invoke(() =>
				{
					if (_viewModel == null)
						return;

					if (_viewModel.IsDetailPanelOpen)
						OpenDetailPanelVisual();
					else
						CloseDetailPanelVisual();
				});
			}
		}

		private void OpenDetailPanelVisual()
		{
			DetailPanelColumn.Width = new GridLength(600);
			FadeDetailPanel(1.0);
		}

		private void CloseDetailPanelVisual()
		{
			DetailPanelColumn.Width = new GridLength(0);
			FadeDetailPanel(0.0);
		}

		private void FadeDetailPanel(double toOpacity)
		{
			DoubleAnimation animation = new DoubleAnimation
			{
				To = toOpacity,
				Duration = new Duration(System.TimeSpan.FromMilliseconds(200))
			};

			DetailPanel.BeginAnimation(OpacityProperty, animation);
		}

		private void CloseDetailPanel(object sender, RoutedEventArgs e)
		{
			_viewModel?.CloseDetailPanel();
		}
	}
}