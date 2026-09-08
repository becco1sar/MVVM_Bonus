using GalaSoft.MvvmLight.Messaging;
using MVVM_Bonus.Model;
using MVVM_Bonus.Services;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MVVM_Bonus.ViewModel
{
	public class BonusViewModel : ObservableObject, IPageViewModel
	{
		#region Fields
		private string _search;
		private Person _currentPerson;
		private BonusModel _bonusPoint;
		private BonusModel _selectedBonusModel;
		private DataBaseService _databaseService;
		private ObservableCollection<BonusModel> _listBonusModels;
		private ObservableCollection<BonusModel> _filteredListBonusModels;
		private ObservableCollection<BonusDetailModel> _myBonusDetails;
		private ICommand _backButtonCommand;
		private ICommand _btnDetailViewCommand;
		private ICommand _btnPrintCommand;
		private ICommand _btnPrintPreviewCommand;
		private ICommand _btnCloseDetailPanelCommand;
		private bool _isDetailPanelOpen;
		private string _detailWarning;
		#endregion

		#region Properties
		public string Search
		{
			get
			{
				return _search;
			}
			set
			{
				_search = value;
				OnPropertyChanged(nameof(Search));
				_filterByDate(_search);
			}
		}

		public Person CurrentPerson
		{
			get
			{
				if (_currentPerson == null)
					_currentPerson = new Person();
				return _currentPerson;
			}
			set
			{
				_currentPerson = value;
				OnPropertyChanged(nameof(CurrentPerson));
			}
		}

		public BonusModel BonusPoint
		{
			get
			{
				if (_bonusPoint == null)
					_bonusPoint = new BonusModel();
				return _bonusPoint;
			}
			set
			{
				_bonusPoint = value;
				OnPropertyChanged(nameof(BonusPoint));
			}
		}

		public BonusModel SelectedBonusModel
		{
			get
			{
				return _selectedBonusModel;
			}
			set
			{
				_selectedBonusModel = value;
				OnPropertyChanged(nameof(SelectedBonusModel));
				OnPropertyChanged(nameof(HasSelection));

				if (_selectedBonusModel != null)
				{
					LoadBonusDetails(_selectedBonusModel);
					GeneratePreviewDocument();
					OpenDetailPanel();
				}
				else
				{
					PreviewDocument = null;
				}
			}
		}
		private FlowDocument _previewDocument;
		public FlowDocument PreviewDocument
		{
			get => _previewDocument;
			set
			{
				_previewDocument = value;
				OnPropertyChanged(nameof(PreviewDocument));
			}
		}
		public ObservableCollection<BonusModel> ListBonusModels
		{
			get
			{
				if (_listBonusModels == null)
					_listBonusModels = new ObservableCollection<BonusModel>();
				return _listBonusModels;
			}
			set
			{
				_listBonusModels = value;
				OnPropertyChanged(nameof(ListBonusModels));
			}
		}

		public ObservableCollection<BonusModel> FilteredListBonusModels
		{
			get
			{
				if (_filteredListBonusModels == null)
					_filteredListBonusModels = new ObservableCollection<BonusModel>();
				return _filteredListBonusModels;
			}
			set
			{
				_filteredListBonusModels = value;
				OnPropertyChanged(nameof(FilteredListBonusModels));
			}
		}

		public ObservableCollection<BonusDetailModel> MyBonusDetails
		{
			get
			{
				if (_myBonusDetails == null)
					_myBonusDetails = new ObservableCollection<BonusDetailModel>();
				return _myBonusDetails;
			}
			set
			{
				_myBonusDetails = value;
				OnPropertyChanged(nameof(MyBonusDetails));
			}
		}

		public bool HasSelection => SelectedBonusModel != null;

		/// <summary>True when this employee has no bonuses recorded at all.</summary>
		public bool HasNoHistory => ListBonusModels.Count == 0;

		/// <summary>True when a search is active but matches no period.</summary>
		public bool HasNoSearchResults =>
			ListBonusModels.Count > 0 && FilteredListBonusModels.Count == 0;

		/// <summary>
		/// Set when the evaluation point text for this employee's role and language
		/// cannot be read, so the detail rows would otherwise be silently empty.
		/// </summary>
		public string DetailWarning
		{
			get => _detailWarning;
			private set
			{
				_detailWarning = value;
				OnPropertyChanged(nameof(DetailWarning));
				OnPropertyChanged(nameof(HasDetailWarning));
			}
		}

		public bool HasDetailWarning => !string.IsNullOrWhiteSpace(DetailWarning);

		public bool IsDetailPanelOpen
		{
			get
			{
				return _isDetailPanelOpen;
			}
			set
			{
				_isDetailPanelOpen = value;
				OnPropertyChanged(nameof(IsDetailPanelOpen));
			}
		}
		#endregion

		#region Commands

		public ICommand BackButtonCommand
		{
			get
			{
				if (_backButtonCommand == null)
				{
					_backButtonCommand = new RelayCommand(
							param => _backToGeneral()
					);
				}
				return _backButtonCommand;
			}
		}

		public ICommand BtnDetailViewCommand
		{
			get
			{
				if (_btnDetailViewCommand == null)
				{
					_btnDetailViewCommand = new RelayCommand(e =>
					{
						if (SelectedBonusModel != null)
						{
							CommonViewModel.PreviousViewModel = new BonusViewModel();
							Messenger.Default.Send(CurrentPerson, "selectedPerson");
							Messenger.Default.Send(SelectedBonusModel, "PrintView");
							Messenger.Default.Send(false, "PrintAndClose");
						}
					});
				}
				return _btnDetailViewCommand;
			}
		}

		public ICommand BtnPrintCommand
		{
			get
			{
				if (_btnPrintCommand == null)
				{
					_btnPrintCommand = new RelayCommand(param =>
					{
						if (SelectedBonusModel != null)
						{
							try
							{
								PrintDialog printDialog = new PrintDialog();
								printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;

								if (printDialog.ShowDialog() == true)
								{
									FlowDocument doc = BuildPrintDocument(
											printDialog.PrintableAreaWidth,
											printDialog.PrintableAreaHeight
									);

									IDocumentPaginatorSource paginator = doc;
									printDialog.PrintDocument(
											paginator.DocumentPaginator,
											"Bonus Print"
									);
								}
							}
							catch (Exception ex)
							{
								MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
					});
				}
				return _btnPrintCommand;
			}
		}

		private FlowDocument BuildPrintDocument(double pageWidth, double pageHeight)
		{
			FlowDocument doc = new FlowDocument
			{
				PageWidth = pageWidth,
				PageHeight = pageHeight,
				PagePadding = new Thickness(60, 50, 60, 50),
				ColumnWidth = pageWidth,
				FontFamily = new FontFamily("Segoe UI"),
				FontSize = 11
			};

			// ── Title ──────────────────────────────────────────────
			doc.Blocks.Add(new Paragraph(new Run("Bauer Media Outdoor"))
			{
				FontSize = 16,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(0, 0, 0, 4)
			});

			doc.Blocks.Add(new Paragraph(new Run("Prime de Qualité"))
			{
				FontSize = 11,
				Foreground = System.Windows.Media.Brushes.Gray,
				Margin = new Thickness(0, 0, 0, 16)
			});

			// ── Worker / Period / Total info ───────────────────────
			Table infoTable = new Table { CellSpacing = 0 };
			infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
			infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
			infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

			TableRowGroup infoGroup = new TableRowGroup();
			TableRow infoRow = new TableRow();

			infoRow.Cells.Add(MakeInfoCell("Employé", SelectedBonusModel.WorkerName));
			infoRow.Cells.Add(MakeInfoCell("Période", SelectedBonusModel.Period));
			infoRow.Cells.Add(MakeInfoCell("Total", SelectedBonusModel.Total.ToString("0.00") + " €"));

			infoGroup.Rows.Add(infoRow);
			infoTable.RowGroups.Add(infoGroup);
			doc.Blocks.Add(infoTable);

			doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 12, 0, 0) });

			// ── Bonus detail table ─────────────────────────────────
			Table detailTable = new Table { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1) };
			detailTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
			detailTable.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
			detailTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

			// Header row
			TableRowGroup headerGroup = new TableRowGroup
			{
				Background = new System.Windows.Media.SolidColorBrush(
							System.Windows.Media.Color.FromRgb(21, 101, 192))
			};
			TableRow headerRow = new TableRow();
			headerRow.Cells.Add(MakeHeaderCell("Point d'évaluation"));
			headerRow.Cells.Add(MakeHeaderCell("Commentaire"));
			headerRow.Cells.Add(MakeHeaderCell("Montant"));
			headerGroup.Rows.Add(headerRow);
			detailTable.RowGroups.Add(headerGroup);

			// Data rows
			TableRowGroup dataGroup = new TableRowGroup();
			bool alternate = false;
			foreach (var detail in MyBonusDetails)
			{
				var bg = alternate
						? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 249, 249))
						: System.Windows.Media.Brushes.White;

				TableRow row = new TableRow { Background = bg };
				row.Cells.Add(MakeDataCell(detail.DetailPoint, TextAlignment.Left));
				row.Cells.Add(MakeDataCell(detail.Comment, TextAlignment.Center));
				row.Cells.Add(MakeDataCell(detail.Amount.ToString("0.00") + " €", TextAlignment.Center, bold: true));
				dataGroup.Rows.Add(row);
				alternate = !alternate;
			}
			detailTable.RowGroups.Add(dataGroup);

			// Total row
			TableRowGroup totalGroup = new TableRowGroup
			{
				Background = new System.Windows.Media.SolidColorBrush(
							System.Windows.Media.Color.FromRgb(232, 245, 233))
			};
			TableRow totalRow = new TableRow();
			totalRow.Cells.Add(MakeDataCell("TOTAL", TextAlignment.Left, bold: true));
			totalRow.Cells.Add(MakeDataCell("", TextAlignment.Center));
			totalRow.Cells.Add(MakeDataCell(SelectedBonusModel.Total.ToString("0.00") + " €", TextAlignment.Center, bold: true, color: System.Windows.Media.Color.FromRgb(46, 125, 50)));
			totalGroup.Rows.Add(totalRow);
			detailTable.RowGroups.Add(totalGroup);

			doc.Blocks.Add(detailTable);

			// ── Signatures ─────────────────────────────────────────
			doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 30, 0, 0) });

			Table sigTable = new Table { CellSpacing = 0 };
			sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
			sigTable.Columns.Add(new TableColumn { Width = new GridLength(0.2, GridUnitType.Star) });
			sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

			TableRowGroup sigGroup = new TableRowGroup();
			TableRow sigLabelRow = new TableRow();
			sigLabelRow.Cells.Add(MakeDataCell("Signature de l'employé", TextAlignment.Left, bold: true));
			sigLabelRow.Cells.Add(MakeDataCell("", TextAlignment.Left));
			sigLabelRow.Cells.Add(MakeDataCell("Signature du responsable", TextAlignment.Left, bold: true));
			sigGroup.Rows.Add(sigLabelRow);

			TableRow sigLineRow = new TableRow();
			sigLineRow.Cells.Add(MakeSigCell());
			sigLineRow.Cells.Add(MakeDataCell("", TextAlignment.Left));
			sigLineRow.Cells.Add(MakeSigCell());
			sigGroup.Rows.Add(sigLineRow);

			sigTable.RowGroups.Add(sigGroup);
			doc.Blocks.Add(sigTable);

			return doc;
		}

		// ── Cell helpers ───────────────────────────────────────────

		private TableCell MakeInfoCell(string label, string value)
		{
			StackPanel sp = new StackPanel();
			var p = new Paragraph();
			p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold });
			p.Inlines.Add(new Run(value));
			p.Margin = new Thickness(0, 0, 0, 8);
			return new TableCell(p);
		}

		private TableCell MakeHeaderCell(string text)
		{
			return new TableCell(new Paragraph(new Run(text))
			{
				FontWeight = FontWeights.Bold,
				Foreground = System.Windows.Media.Brushes.White,
				FontSize = 10,
				Margin = new Thickness(6, 5, 6, 5),
				TextAlignment = TextAlignment.Center
			});
		}

		private TableCell MakeDataCell(string text, TextAlignment align, bool bold = false,
				System.Windows.Media.Color? color = null)
		{
			var run = new Run(text);
			if (color.HasValue)
				run.Foreground = new System.Windows.Media.SolidColorBrush(color.Value);

			return new TableCell(new Paragraph(run)
			{
				FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
				FontSize = 10,
				TextAlignment = align,
				Margin = new Thickness(6, 4, 6, 4),
				BorderBrush = System.Windows.Media.Brushes.LightGray,
				BorderThickness = new Thickness(0, 0, 0, 1)
			});
		}
		private void GeneratePreviewDocument()
		{
			if (SelectedBonusModel == null)
			{
				PreviewDocument = null;
				return;
			}

			PreviewDocument = BuildPrintDocument(793.7, 1122.5);
		}
		private TableCell MakeSigCell()
		{
			return new TableCell(new Paragraph())
			{
				BorderBrush = System.Windows.Media.Brushes.Gray,
				BorderThickness = new Thickness(0, 0, 0, 1),
				Padding = new Thickness(0, 30, 0, 0)
			};
		}
		public ICommand BtnCloseDetailPanelCommand
		{
			get
			{
				if (_btnCloseDetailPanelCommand == null)
				{
					_btnCloseDetailPanelCommand = new RelayCommand(param =>
					{
						CloseDetailPanel();
					});
				}
				return _btnCloseDetailPanelCommand;
			}
		}

		#endregion

		#region Constructors
		public BonusViewModel()
		{
			_databaseService = new DataBaseService();
			Messenger.Default.Register<Person>(this, Constants.MESSENGER_GET_VIEW, callback => _generateListOfBonus(callback));
		}
		#endregion

		#region Methods

		/// <summary>
		/// Navigates back to the general/main menu view
		/// </summary>
		private void _backToGeneral()
		{
			ListBonusModels.Clear();
			FilteredListBonusModels.Clear();
			MyBonusDetails.Clear();
			SelectedBonusModel = null;
			IsDetailPanelOpen = false;
			Mediator.Notify(Constants.MAIN_MENU_VIEW, "");
		}

		/// <summary>
		/// Filters the bonus list by date/period
		/// </summary>
		private void _filterByDate(string dt)
		{
			if (string.IsNullOrEmpty(dt))
			{
				FilteredListBonusModels = new ObservableCollection<BonusModel>(ListBonusModels);
			}
			else
			{
				var filteredItems = ListBonusModels
						.Where(x => x.Period != null
									&& x.Period.Contains(dt, StringComparison.OrdinalIgnoreCase))
						.ToList();

				FilteredListBonusModels = new ObservableCollection<BonusModel>(filteredItems);
			}

			OnPropertyChanged(nameof(HasNoHistory));
			OnPropertyChanged(nameof(HasNoSearchResults));
		}

		/// <summary>
		/// Generates the list of bonuses for a specific person
		/// </summary>
		private void _generateListOfBonus(Person person)
		{
			try
			{
				CurrentPerson = person;
				ListBonusModels.Clear();
				FilteredListBonusModels.Clear();

				var table = _databaseService.ExecuteQuery(
						"SELECT * FROM Bonus_General WHERE Worker_id = ?", CurrentPerson.Id
				);

				foreach (DataRow row in table.Rows)
				{
					BonusModel bonusModel = new BonusModel
					{
						WorkerName = CurrentPerson.Name,
						Total = Convert.ToDecimal(row[3]),
						Period = Convert.ToString(row[2])
					};

					for (int i = 0; i < 10; i++)
					{
						try
						{
							bonusModel.Amounts.Add(Convert.ToDecimal(row[i + 4]));
							bonusModel.Comments.Add(Convert.ToString(row[i + 14]) ?? "");
						}
						catch
						{
							bonusModel.Amounts.Add(0m);
							bonusModel.Comments.Add("");
						}
					}

					ListBonusModels.Add(bonusModel);
				}

				FilteredListBonusModels = new ObservableCollection<BonusModel>(ListBonusModels);

				OnPropertyChanged(nameof(HasNoHistory));
				OnPropertyChanged(nameof(HasNoSearchResults));
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Could not load the bonus history.\n\n{ex.Message}",
					"Connection problem",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Loads the details of a selected bonus and generates the detail points
		/// </summary>
		private void LoadBonusDetails(BonusModel bonusModel)
		{
			try
			{
				MyBonusDetails.Clear();
				DetailWarning = null;

				if (bonusModel.Amounts.Count == 0)
					return;

				ObservableCollection<string> detailPoints = DetailPoints.GenerateDetailPoints(
						Constants.CONTENT_CELL_ROOT + CurrentPerson.PathToContentCells);

				if (detailPoints == null)
				{
					// Falling back to numbered points beats an empty panel: the amounts
					// are still worth reading even when the wording cannot be found.
					DetailWarning = "The wording of the evaluation points could not be read for this "
									+ "employee's role and language, so the rows below are numbered instead.";

					detailPoints = new ObservableCollection<string>(
						Enumerable.Range(1, bonusModel.Amounts.Count).Select(n => $"Point {n}"));
				}

				for (int i = 0; i < detailPoints.Count && i < bonusModel.Amounts.Count; i++)
				{
					MyBonusDetails.Add(new BonusDetailModel
					{
						DetailPoint = detailPoints[i],
						Amount = bonusModel.Amounts[i],
						Comment = i < bonusModel.Comments.Count ? bonusModel.Comments[i] : string.Empty
					});
				}
			}
			catch (Exception ex)
			{
				DetailWarning = $"Could not load the bonus details: {ex.Message}";
			}
		}

		/// <summary>
		/// Opens the detail panel with animation
		/// </summary>
		private void OpenDetailPanel()
		{
			IsDetailPanelOpen = true;
		}

		/// <summary>
		/// Closes the detail panel with animation
		/// </summary>
		public void CloseDetailPanel()
		{
			IsDetailPanelOpen = false;
			SelectedBonusModel = null;
			MyBonusDetails.Clear();
		}

		#endregion
	}
}