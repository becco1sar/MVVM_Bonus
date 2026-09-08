using System;
using System.Collections.Generic;
using MVVM_Bonus.Model;

namespace MVVM_Bonus.Services
{
    public interface IEvaluationService
    {
        decimal CalculateTotalBonus(IEnumerable<BonusValuesModel> selectedItems, decimal additionalBonus);
        bool ShouldUnlockSecondaryBonus(IEnumerable<BonusValuesModel> items);
    }
}
