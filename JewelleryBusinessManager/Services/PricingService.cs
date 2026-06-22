using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class PricingService
{
    public static decimal CalculateJewelleryCost(JewelleryItem item)
        => item.MaterialCost + item.OtherCost + item.LabourHours * item.LabourRate;

    public static decimal CalculateRetailProfit(JewelleryItem item)
        => item.RetailPrice - CalculateJewelleryCost(item);

    public static decimal CalculateSaleProfit(Sale sale)
        => sale.SaleAmount - sale.CostOfGoods;

    public static decimal CalculateProfitMargin(decimal saleAmount, decimal cost)
        => saleAmount <= 0 ? 0 : (saleAmount - cost) / saleAmount;

    public static decimal CalculateMarkup(decimal saleAmount, decimal cost)
        => cost <= 0 ? 0 : (saleAmount - cost) / cost;

    public static decimal CalculateRecommendedRetail(decimal totalCost, decimal targetMarginPercent)
    {
        var margin = targetMarginPercent / 100m;
        if (totalCost <= 0 || margin <= 0 || margin >= 1)
            return totalCost;
        return Math.Round(totalCost / (1 - margin), 2);
    }

    public static decimal CalculateJobCost(Job job)
        => job.MaterialCost + job.LabourCost;

    public static decimal CalculateJobProfit(Job job)
    {
        var income = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        return income - CalculateJobCost(job);
    }
}
