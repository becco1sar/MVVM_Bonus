using System;
using System.Collections.Generic;
using System.Linq;
using MVVM_Bonus.Model;

namespace MVVM_Bonus.Services
{
    public class EvaluationService : IEvaluationService
    {
        public decimal CalculateTotalBonus(IEnumerable<BonusValuesModel> selectedItems, decimal additionalBonus)
        {
            if (selectedItems == null)
                return additionalBonus;

            decimal total = additionalBonus + selectedItems.Where(x => x != null && x.IsSelected).Sum(x => x.Amount);

            if (total > 124m)
            {
                return Math.Round(total, 2, MidpointRounding.AwayFromZero);
            }
            return total;
        }

        public bool ShouldUnlockSecondaryBonus(IEnumerable<BonusValuesModel> items)
        {
            var itemList = items?.ToList();
            if (itemList == null || itemList.Count < 3)
                return false;

            return itemList[0].IsSelected && itemList[1].IsSelected && itemList[2].IsSelected;
        }
    }
}
