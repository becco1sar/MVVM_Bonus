using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MVVM_Bonus.Model;

namespace MVVM_Bonus.Services
{
    public class ReportService : IReportService
    {
        public FlowDocument GenerateBonusReport(BonusModel bonusModel, double pageWidth = 793.7, double pageHeight = 1122.5)
        {
            if (bonusModel == null)
                return null;

            FlowDocument doc = new FlowDocument
            {
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                PagePadding = new Thickness(50),
                ColumnWidth = pageWidth,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                TextAlignment = TextAlignment.Left
            };

            doc.Blocks.Add(new Paragraph(new Run("Bauer Media Outdoor"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            doc.Blocks.Add(new Paragraph(new Run("Prime de qualité / Quality bonus"))
            {
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 18)
            });

            Table metaTable = new Table();
            metaTable.Columns.Add(new TableColumn());
            metaTable.Columns.Add(new TableColumn());
            metaTable.Columns.Add(new TableColumn());

            TableRowGroup metaGroup = new TableRowGroup();
            TableRow metaRow = new TableRow();
            metaRow.Cells.Add(CreateCell($"Name: {bonusModel.WorkerName}", true));
            metaRow.Cells.Add(CreateCell($"Period: {bonusModel.Period}", true));
            metaRow.Cells.Add(CreateCell($"Total: {bonusModel.Total:0.00} €", true));
            metaGroup.Rows.Add(metaRow);
            metaTable.RowGroups.Add(metaGroup);
            doc.Blocks.Add(metaTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 6, 0, 6) });

            Table detailTable = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            detailTable.Columns.Add(new TableColumn { Width = new GridLength(5, GridUnitType.Star) });
            detailTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            detailTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210))
            };

            headerRow.Cells.Add(CreateHeaderCell("Evaluation Point / Category"));
            headerRow.Cells.Add(CreateHeaderCell("Comment"));
            headerRow.Cells.Add(CreateHeaderCell("Amount"));
            headerGroup.Rows.Add(headerRow);
            detailTable.RowGroups.Add(headerGroup);

            TableRowGroup bodyGroup = new TableRowGroup();

            int count = Math.Min(bonusModel.Amounts.Count, bonusModel.Comments.Count);
            for (int i = 0; i < count; i++)
            {
                if (bonusModel.Amounts[i] == 0 && string.IsNullOrWhiteSpace(bonusModel.Comments[i]))
                    continue;

                string label = i < bonusModel.ItemLabels.Count
                               && !string.IsNullOrWhiteSpace(bonusModel.ItemLabels[i])
                    ? bonusModel.ItemLabels[i].Trim()
                    : $"Point {i + 1}";

                TableRow bodyRow = new TableRow();
                bodyRow.Cells.Add(CreateCell(label));
                bodyRow.Cells.Add(CreateCell(bonusModel.Comments[i]));
                bodyRow.Cells.Add(CreateCell($"{bonusModel.Amounts[i]:0.00} €", false, TextAlignment.Center));
                bodyGroup.Rows.Add(bodyRow);
            }

            TableRow totalRow = new TableRow
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233))
            };

            totalRow.Cells.Add(CreateCell("TOTAL", true));
            totalRow.Cells.Add(CreateCell(""));
            totalRow.Cells.Add(CreateCell($"{bonusModel.Total:0.00} €", true, TextAlignment.Center));
            bodyGroup.Rows.Add(totalRow);

            detailTable.RowGroups.Add(bodyGroup);
            doc.Blocks.Add(detailTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 24, 0, 0) });

            Table signatureTable = new Table();
            signatureTable.Columns.Add(new TableColumn());
            signatureTable.Columns.Add(new TableColumn());

            TableRowGroup signatureGroup = new TableRowGroup();
            TableRow sigRow = new TableRow();
            sigRow.Cells.Add(CreateCell("Signature Worker\n\n________________________"));
            sigRow.Cells.Add(CreateCell("Signature Manager\n\n________________________"));
            signatureGroup.Rows.Add(sigRow);
            signatureTable.RowGroups.Add(signatureGroup);

            doc.Blocks.Add(signatureTable);

            return doc;
        }

        public void PrintDocument(FlowDocument document, string jobTitle = "Bonus Print")
        {
            if (document == null)
                return;

            try
            {
                PrintDialog printDialog = new PrintDialog();
                printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;

                if (printDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, jobTitle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TableCell CreateHeaderCell(string text)
        {
            return new TableCell(new Paragraph(new Run(text))
            {
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0),
                FontSize = 10
            })
            {
                Padding = new Thickness(8),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
        }

        private TableCell CreateCell(string text, bool bold = false, TextAlignment alignment = TextAlignment.Left)
        {
            return new TableCell(new Paragraph(new Run(text ?? string.Empty))
            {
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = alignment,
                Margin = new Thickness(0),
                FontSize = 10
            })
            {
                Padding = new Thickness(8),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
        }
    }
}
