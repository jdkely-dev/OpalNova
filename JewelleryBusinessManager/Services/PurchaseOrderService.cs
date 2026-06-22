using System.Diagnostics;
using System.IO;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using Microsoft.EntityFrameworkCore;

namespace JewelleryBusinessManager.Services;

public sealed class PurchaseOrderReceiveResult
{
    public int PurchaseOrderId { get; set; }
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public int LinesReceived { get; set; }
    public int TransactionsCreated { get; set; }
    public decimal TotalQuantityReceived { get; set; }
    public List<string> Warnings { get; } = new();
    public List<string> UpdatedMaterials { get; } = new();

    public string ToUserMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{PurchaseOrderCode} received into inventory.");
        sb.AppendLine();
        sb.AppendLine($"Lines received: {LinesReceived}");
        sb.AppendLine($"Total quantity received: {TotalQuantityReceived:N2}");
        sb.AppendLine($"Material transactions created: {TransactionsCreated}");
        if (UpdatedMaterials.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Updated materials:");
            foreach (var material in UpdatedMaterials.Take(8))
                sb.AppendLine($"• {material}");
            if (UpdatedMaterials.Count > 8)
                sb.AppendLine($"• ...and {UpdatedMaterials.Count - 8} more");
        }
        if (Warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Warnings:");
            foreach (var warning in Warnings)
                sb.AppendLine($"• {warning}");
        }
        return sb.ToString();
    }
}

public static class PurchaseOrderService
{
    public static PurchaseOrder CreateDraftPurchaseOrderFromLowStock(int? preferredSupplierId = null)
    {
        using var db = new AppDbContext();
        var lowMaterials = db.Materials.AsNoTracking()
            .AsEnumerable()
            .Where(m => m.CurrentQuantity <= m.ReorderLevel)
            .OrderBy(m => m.SupplierId != preferredSupplierId)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToList();

        if (lowMaterials.Count == 0)
            throw new InvalidOperationException("No materials are currently at or below their reorder level.");

        var supplierId = preferredSupplierId ?? lowMaterials.FirstOrDefault(m => m.SupplierId.HasValue)?.SupplierId;
        var order = new PurchaseOrder
        {
            PurchaseOrderCode = GeneratePurchaseOrderCode(db),
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = DateTime.Today.AddDays(7),
            Notes = "Created from low stock / reorder suggestions. Review quantities and costs before ordering."
        };

        db.PurchaseOrders.Add(order);
        db.SaveChanges();

        foreach (var material in lowMaterials.Where(m => !supplierId.HasValue || m.SupplierId == supplierId))
        {
            var reorderQuantity = CalculateSuggestedReorderQuantity(material);
            var unitCost = material.CurrentQuantity > 0 && material.PurchaseCost > 0
                ? material.PurchaseCost / material.CurrentQuantity
                : 0;
            db.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = order.Id,
                MaterialId = material.Id,
                ItemName = material.Name,
                UnitType = material.UnitType.ToString(),
                OrderedQuantity = reorderQuantity,
                UnitCost = unitCost,
                LineTotal = reorderQuantity * unitCost,
                Notes = $"Current quantity {material.CurrentQuantity}; reorder level {material.ReorderLevel}."
            });
        }

        RecalculatePurchaseOrderTotals(db, order.Id);
        db.SaveChanges();
        return db.PurchaseOrders.Find(order.Id)!;
    }

    public static void MarkPurchaseOrderOrdered(int purchaseOrderId)
    {
        using var db = new AppDbContext();
        var order = db.PurchaseOrders.Find(purchaseOrderId) ?? throw new InvalidOperationException("Purchase order could not be found.");
        RecalculatePurchaseOrderTotals(db, order.Id);
        order.Status = PurchaseOrderStatus.Ordered;
        if (string.IsNullOrWhiteSpace(order.PurchaseOrderCode))
            order.PurchaseOrderCode = GeneratePurchaseOrderCode(db);
        db.SaveChanges();
    }

    public static PurchaseOrderReceiveResult ReceivePurchaseOrder(int purchaseOrderId)
    {
        using var db = new AppDbContext();
        using var transaction = db.Database.BeginTransaction();

        var order = db.PurchaseOrders.Find(purchaseOrderId) ?? throw new InvalidOperationException("Purchase order could not be found.");
        var items = db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == purchaseOrderId).ToList();
        if (items.Count == 0)
            throw new InvalidOperationException("This purchase order has no line items to receive.");

        var result = new PurchaseOrderReceiveResult
        {
            PurchaseOrderId = order.Id,
            PurchaseOrderCode = string.IsNullOrWhiteSpace(order.PurchaseOrderCode) ? $"Purchase Order #{order.Id}" : order.PurchaseOrderCode
        };

        foreach (var item in items)
        {
            var quantityToReceive = Math.Max(0, item.OrderedQuantity - item.ReceivedQuantity);
            if (quantityToReceive <= 0)
                continue;

            var material = ResolveLinkedMaterial(db, item);
            if (material == null)
            {
                result.Warnings.Add($"{item.ItemName}: no linked material found, so quantity was not added to inventory.");
                continue;
            }

            var valueReceived = quantityToReceive * item.UnitCost;
            var beforeQuantity = material.CurrentQuantity;
            material.CurrentQuantity = beforeQuantity + quantityToReceive;
            if (valueReceived > 0)
                material.PurchaseCost += valueReceived;

            item.MaterialId = material.Id;
            item.ReceivedQuantity += quantityToReceive;
            item.LineTotal = item.OrderedQuantity * item.UnitCost;
            if (string.IsNullOrWhiteSpace(item.ItemName)) item.ItemName = material.Name;
            if (string.IsNullOrWhiteSpace(item.UnitType)) item.UnitType = material.UnitType.ToString();

            db.MaterialTransactions.Add(new MaterialTransaction
            {
                MaterialId = material.Id,
                TransactionDate = DateTime.Today,
                QuantityChange = quantityToReceive,
                Reason = $"Received from purchase order {order.PurchaseOrderCode} at {item.UnitCost:C} per {item.UnitType}",
                Notes = string.IsNullOrWhiteSpace(item.Notes)
                    ? $"PO item #{item.Id}; quantity before {beforeQuantity:N2}, after {material.CurrentQuantity:N2}."
                    : $"{item.Notes} | PO item #{item.Id}; quantity before {beforeQuantity:N2}, after {material.CurrentQuantity:N2}."
            });

            result.LinesReceived++;
            result.TransactionsCreated++;
            result.TotalQuantityReceived += quantityToReceive;
            result.UpdatedMaterials.Add($"{material.MaterialCode} {material.Name}".Trim() + $": {beforeQuantity:N2} → {material.CurrentQuantity:N2} {material.UnitType}");
        }

        RecalculatePurchaseOrderTotals(db, order.Id);
        order.Status = items.All(i => Math.Max(0, i.OrderedQuantity - i.ReceivedQuantity) <= 0)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        if (order.Status == PurchaseOrderStatus.Received)
            order.ReceivedDate = DateTime.Today;

        db.SaveChanges();
        transaction.Commit();

        if (result.LinesReceived == 0)
        {
            if (result.Warnings.Count > 0)
            {
                var warningMessage = "No purchase order lines were received. Check that each purchase order item is linked to a material."
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, result.Warnings);
                throw new InvalidOperationException(warningMessage);
            }
            throw new InvalidOperationException("There were no outstanding purchase order quantities to receive.");
        }

        return result;
    }

    public static string CreatePurchaseOrderDocument(int purchaseOrderId)
    {
        using var db = new AppDbContext();
        var order = db.PurchaseOrders.AsNoTracking().FirstOrDefault(o => o.Id == purchaseOrderId)
            ?? throw new InvalidOperationException("Purchase order could not be found.");
        var supplier = order.SupplierId.HasValue ? db.Suppliers.AsNoTracking().FirstOrDefault(s => s.Id == order.SupplierId.Value) : null;
        var items = db.PurchaseOrderItems.AsNoTracking().Where(i => i.PurchaseOrderId == purchaseOrderId).ToList();
        var settings = BusinessSettingsService.Load();

        var path = CreatePrintoutPath($"purchase-order-{order.PurchaseOrderCode}-{order.Id}.html");
        var html = new StringBuilder();
        AppendHtmlHeader(html, "Purchase Order");
        html.AppendLine($"<h1>Purchase Order {Html(order.PurchaseOrderCode)}</h1>");
        html.AppendLine($"<div class='meta'><b>{Html(settings.BusinessName)}</b><br>{Html(settings.Email)} {Html(settings.Phone)}<br>{Html(settings.Address)}</div>");
        html.AppendLine($"<div class='box'><b>Supplier:</b> {Html(supplier?.Name ?? "No supplier selected")}<br><b>Status:</b> {order.Status}<br><b>Order date:</b> {order.OrderDate:d}<br><b>Expected:</b> {(order.ExpectedDeliveryDate.HasValue ? order.ExpectedDeliveryDate.Value.ToShortDateString() : "Not set")}<br><b>Supplier ref:</b> {Html(order.SupplierReference)}</div>");
        html.AppendLine("<table><tr><th>Item</th><th>Material</th><th>Qty</th><th>Received</th><th>Unit</th><th>Unit Cost</th><th>Total</th></tr>");
        foreach (var item in items)
        {
            var material = item.MaterialId.HasValue ? db.Materials.AsNoTracking().FirstOrDefault(m => m.Id == item.MaterialId.Value) : null;
            html.AppendLine($"<tr><td>{Html(item.ItemName)}</td><td>{Html(material?.MaterialCode ?? string.Empty)}</td><td>{item.OrderedQuantity:N2}</td><td>{item.ReceivedQuantity:N2}</td><td>{Html(item.UnitType)}</td><td>{item.UnitCost:C}</td><td>{item.LineTotal:C}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine($"<div class='totals'><b>Items:</b> {order.ItemsTotal:C}<br><b>Shipping:</b> {order.ShippingCost:C}<br><b>Other:</b> {order.OtherCost:C}<br><b>Total:</b> {order.TotalCost:C}</div>");
        html.AppendLine($"<div class='box'><b>Notes:</b><br>{Html(order.Notes).Replace("\n", "<br>")}</div>");
        html.AppendLine("</body></html>");
        File.WriteAllText(path, html.ToString());
        OpenFile(path);
        return path;
    }

    public static string CreateReorderReport()
    {
        using var db = new AppDbContext();
        var lowMaterials = db.Materials.AsNoTracking().AsEnumerable()
            .Where(m => m.CurrentQuantity <= m.ReorderLevel)
            .OrderBy(m => m.SupplierId)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToList();
        var openOrders = db.PurchaseOrders.AsNoTracking().AsEnumerable()
            .Where(o => o.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .OrderBy(o => o.ExpectedDeliveryDate ?? DateTime.MaxValue)
            .ToList();
        var settings = BusinessSettingsService.Load();
        var path = CreatePrintoutPath($"reorder-report-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = new StringBuilder();
        AppendHtmlHeader(html, "Reorder Report");
        html.AppendLine("<h1>Supplier Reorder Report</h1>");
        html.AppendLine($"<div class='meta'>{Html(settings.BusinessName)} • Created {DateTime.Now:g}</div>");
        html.AppendLine("<h2>Materials at or below reorder level</h2>");
        html.AppendLine("<table><tr><th>Code</th><th>Name</th><th>Category</th><th>Current</th><th>Reorder Level</th><th>Suggested Qty</th><th>Supplier</th></tr>");
        foreach (var material in lowMaterials)
        {
            var supplier = material.SupplierId.HasValue ? db.Suppliers.AsNoTracking().FirstOrDefault(s => s.Id == material.SupplierId.Value) : null;
            html.AppendLine($"<tr><td>{Html(material.MaterialCode)}</td><td>{Html(material.Name)}</td><td>{material.Category}</td><td>{material.CurrentQuantity:N2} {material.UnitType}</td><td>{material.ReorderLevel:N2}</td><td>{CalculateSuggestedReorderQuantity(material):N2}</td><td>{Html(supplier?.Name ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Open purchase orders</h2>");
        html.AppendLine("<table><tr><th>PO</th><th>Supplier</th><th>Status</th><th>Expected</th><th>Total</th></tr>");
        foreach (var order in openOrders)
        {
            var supplier = order.SupplierId.HasValue ? db.Suppliers.AsNoTracking().FirstOrDefault(s => s.Id == order.SupplierId.Value) : null;
            html.AppendLine($"<tr><td>{Html(order.PurchaseOrderCode)}</td><td>{Html(supplier?.Name ?? string.Empty)}</td><td>{order.Status}</td><td>{(order.ExpectedDeliveryDate.HasValue ? order.ExpectedDeliveryDate.Value.ToShortDateString() : "")}</td><td>{order.TotalCost:C}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</body></html>");
        File.WriteAllText(path, html.ToString());
        OpenFile(path);
        return path;
    }

    public static void RecalculatePurchaseOrderTotals(AppDbContext db, int purchaseOrderId)
    {
        var order = db.PurchaseOrders.Find(purchaseOrderId);
        if (order == null) return;
        var items = db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == purchaseOrderId).ToList();
        foreach (var item in items)
            item.LineTotal = item.OrderedQuantity * item.UnitCost;
        order.ItemsTotal = items.Sum(i => i.LineTotal);
    }

    private static Material? ResolveLinkedMaterial(AppDbContext db, PurchaseOrderItem item)
    {
        if (item.MaterialId.HasValue)
        {
            var linkedMaterial = db.Materials.Find(item.MaterialId.Value);
            if (linkedMaterial != null)
                return linkedMaterial;
        }

        var name = item.ItemName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return db.Materials.AsEnumerable().FirstOrDefault(m =>
            string.Equals(m.MaterialCode, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string GeneratePurchaseOrderCode(AppDbContext db)
    {
        var next = db.PurchaseOrders.Count() + 1;
        return $"PO-{DateTime.Today:yyyyMMdd}-{next:000}";
    }

    private static decimal CalculateSuggestedReorderQuantity(Material material)
    {
        var target = Math.Max(material.ReorderLevel * 2, material.ReorderLevel + 1);
        return Math.Max(1, target - material.CurrentQuantity);
    }

    private static string CreatePrintoutPath(string fileName)
    {
        var settings = BusinessSettingsService.Load();
        var folder = string.IsNullOrWhiteSpace(settings.PrintoutFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Printouts")
            : settings.PrintoutFolder;
        Directory.CreateDirectory(folder);
        var safeName = string.Concat(fileName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
        return Path.Combine(folder, safeName);
    }

    private static void AppendHtmlHeader(StringBuilder html, string title)
    {
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
        html.AppendLine($"<title>{Html(title)}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #1f2937; }");
        html.AppendLine("h1 { color: #111827; margin-bottom: 8px; }");
        html.AppendLine("h2 { margin-top: 26px; color: #374151; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 12px; }");
        html.AppendLine("th, td { border: 1px solid #d1d5db; padding: 8px; text-align: left; vertical-align: top; }");
        html.AppendLine("th { background: #f3f4f6; }");
        html.AppendLine(".box { border: 1px solid #d1d5db; background: #f9fafb; padding: 12px; margin: 14px 0; border-radius: 8px; }");
        html.AppendLine(".meta { color: #6b7280; margin-bottom: 18px; }");
        html.AppendLine(".totals { float: right; min-width: 260px; border: 1px solid #d1d5db; padding: 12px; margin-top: 14px; }");
        html.AppendLine("@media print { body { margin: 12mm; } }");
        html.AppendLine("</style></head><body>");
    }

    private static string Html(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { }
    }
}
