using System.Windows.Documents;
using MVVM_Bonus.Model;

namespace MVVM_Bonus.Services
{
    public interface IReportService
    {
        FlowDocument GenerateBonusReport(BonusModel bonusModel, double pageWidth = 793.7, double pageHeight = 1122.5);
        void PrintDocument(FlowDocument document, string jobTitle = "Bonus Print");
    }
}
