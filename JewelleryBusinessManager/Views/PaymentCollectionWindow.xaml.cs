using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class PaymentCollectionWindow : Window
{
    private readonly List<JobPaymentRow> _rows = new();
    private JobPaymentRow? _selectedRow;
    private bool _loading;

    public PaymentCollectionWindow()
    {
        InitializeComponent();
        PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        PaymentMethodCombo.SelectedItem = PaymentMethod.Card;
        PaymentDatePicker.SelectedDate = DateTime.Today;
        LoadJobs();
    }

    private void LoadJobs()
    {
        _loading = true;
        try
        {
            using var db = new AppDbContext();
            var jobs = db.Jobs.AsNoTracking().OrderBy(x => x.DueDate ?? DateTime.MaxValue).ThenByDescending(x => x.UpdatedAt).ToList();
            var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x.FullName);
            var payments = db.Payments.AsNoTracking().Where(x => x.JobId.HasValue).ToList().GroupBy(x => x.JobId!.Value).ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));
            var salesByJob = db.Sales.AsNoTracking().Where(x => x.JobId.HasValue).ToList().GroupBy(x => x.JobId!.Value).ToDictionary(x => x.Key, x => x.First());

            _rows.Clear();
            foreach (var job in jobs)
            {
                var total = GetJobTotal(job);
                var paymentTotal = payments.TryGetValue(job.Id, out var sum) ? sum : 0m;
                var paid = Math.Max(job.DepositPaid, paymentTotal);
                var balance = Math.Max(0, total - paid);
                var customer = job.CustomerId.HasValue && customers.TryGetValue(job.CustomerId.Value, out var name) ? name : "No customer linked";
                _rows.Add(new JobPaymentRow(job.Id, job.JobCode, job.JobTitle, customer, job.Status, job.DueDate, total, paid, balance, salesByJob.ContainsKey(job.Id)));
            }

            ApplyFilters();
            StatusMessageText.Text = $"Loaded {_rows.Count} job(s).";
        }
        finally
        {
            _loading = false;
        }
    }

    private static decimal GetJobTotal(Job job) => job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;

    private void ApplyFilters()
    {
        var query = SearchBox.Text.Trim().ToLowerInvariant();
        var filter = (FilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Active handover jobs";
        IEnumerable<JobPaymentRow> rows = _rows;

        if (!string.IsNullOrWhiteSpace(query))
            rows = rows.Where(x => x.TitleLine.ToLowerInvariant().Contains(query) || x.CustomerLine.ToLowerInvariant().Contains(query));

        rows = filter switch
        {
            "Ready for collection" => rows.Where(x => x.Status == JobStatus.ReadyForPickup),
            "Ready to ship" => rows.Where(x => x.Status == JobStatus.ReadyToShip),
            "Balances owing" => rows.Where(x => x.Balance > 0m && x.Status != JobStatus.Cancelled),
            "Completed" => rows.Where(x => x.Status == JobStatus.Completed),
            "All jobs" => rows,
            _ => rows.Where(x => x.Status != JobStatus.Completed && x.Status != JobStatus.Cancelled && (x.Status == JobStatus.ReadyForPickup || x.Status == JobStatus.ReadyToShip || x.Status == JobStatus.QualityCheck || x.Status == JobStatus.AwaitingCustomerApproval || x.Status == JobStatus.Approved || x.Balance > 0m))
        };

        var selectedId = _selectedRow?.JobId;
        JobsList.ItemsSource = rows.ToList();
        if (selectedId.HasValue)
            JobsList.SelectedItem = JobsList.Items.Cast<JobPaymentRow>().FirstOrDefault(x => x.JobId == selectedId.Value);
    }

    private void JobsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _selectedRow = JobsList.SelectedItem as JobPaymentRow;
        LoadSelectedJobDetails();
    }

    private void LoadSelectedJobDetails()
    {
        if (_selectedRow == null)
        {
            SelectedJobTitle.Text = "Select a job";
            SelectedJobDetails.Text = string.Empty;
            TotalAmountText.Text = PaidAmountText.Text = BalanceAmountText.Text = StatusText.Text = string.Empty;
            PaymentsGrid.ItemsSource = null;
            return;
        }

        using var db = new AppDbContext();
        var job = db.Jobs.AsNoTracking().First(x => x.Id == _selectedRow.JobId);
        var customer = job.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == job.CustomerId.Value) : null;
        var payments = db.Payments.AsNoTracking().Where(x => x.JobId == job.Id).OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id).ToList();
        var total = GetJobTotal(job);
        var paid = Math.Max(job.DepositPaid, payments.Sum(x => x.Amount));
        var balance = Math.Max(0, total - paid);

        SelectedJobTitle.Text = $"{job.JobCode} {job.JobTitle}".Trim();
        SelectedJobDetails.Text = $"Customer: {customer?.FullName ?? "No customer linked"} - Due: {(job.DueDate.HasValue ? job.DueDate.Value.ToString("d MMM yyyy") : "Not set")} - Sale created: {(db.Sales.AsNoTracking().Any(x => x.JobId == job.Id) ? "Yes" : "No")}";
        TotalAmountText.Text = total.ToString("C", CultureInfo.CurrentCulture);
        PaidAmountText.Text = paid.ToString("C", CultureInfo.CurrentCulture);
        BalanceAmountText.Text = balance.ToString("C", CultureInfo.CurrentCulture);
        StatusText.Text = job.Status.ToString();
        PaymentAmountBox.Text = balance > 0m ? balance.ToString("0.##") : string.Empty;
        PaymentsGrid.ItemsSource = payments;
    }

    private Job? GetSelectedJob(AppDbContext db)
    {
        if (_selectedRow == null)
        {
            MessageBox.Show("Select a job first.", "Payment & Collection", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        return db.Jobs.FirstOrDefault(x => x.Id == _selectedRow.JobId);
    }

    private static decimal D(string? value) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result) || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0m;

    private void RecordPayment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            var amount = D(PaymentAmountBox.Text);
            if (amount <= 0m)
            {
                MessageBox.Show("Enter a payment amount greater than zero.", "Record payment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingPaymentsTotal = db.Payments.AsNoTracking().Where(x => x.JobId == job.Id).Select(x => x.Amount).ToList().Sum();
            var paidBefore = Math.Max(job.DepositPaid, existingPaymentsTotal);
            var method = PaymentMethodCombo.SelectedItem is PaymentMethod selectedMethod ? selectedMethod : PaymentMethod.Card;
            var payment = new Payment
            {
                CustomerId = job.CustomerId,
                JobId = job.Id,
                PaymentDate = PaymentDatePicker.SelectedDate ?? DateTime.Today,
                Amount = amount,
                Method = method,
                Reference = PaymentReferenceBox.Text.Trim(),
                Notes = $"Recorded from Payment & Collection workflow for {job.JobCode}."
            };
            db.Payments.Add(payment);

            var total = GetJobTotal(job);
            var paidAfter = paidBefore + amount;
            job.DepositPaid = paidAfter;
            job.BalanceOwing = Math.Max(0, total - paidAfter);
            if (job.BalanceOwing <= 0 && (job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip || job.Status == JobStatus.AwaitingCustomerApproval))
                AppendNote(job, "Balance paid in full.");
            db.SaveChanges();

            StatusMessageText.Text = $"Recorded payment {amount:C}. Balance now {job.BalanceOwing:C}.";
            LoadJobs();
            SelectJob(job.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Record payment", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateInvoice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;
            var path = DocumentExportService.CreateInvoiceFromJob(job);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusMessageText.Text = "Invoice / receipt generated.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invoice / Receipt", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyBalanceReminder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            var customer = job.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == job.CustomerId.Value) : null;
            var balance = CalculateCurrentBalance(db, job, out var total, out var paid);
            if (balance <= 0m)
            {
                MessageBox.Show("This job does not currently show a balance owing.", "Balance reminder", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = BuildBalanceReminderMessage(job, customer, total, paid, balance);
            Clipboard.SetText(message);
            StatusMessageText.Text = $"Copied balance reminder for {job.JobCode}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Balance reminder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateBalanceFollowUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            var balance = CalculateCurrentBalance(db, job, out var total, out var paid);
            if (balance <= 0m)
            {
                MessageBox.Show("This job does not currently show a balance owing.", "Balance follow-up", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var title = $"Balance reminder {job.JobCode}";
            if (TaskWorkflowService.OpenTaskExists(db, exactTitle: title, jobId: job.Id))
            {
                MessageBox.Show("An open balance follow-up already exists for this job.", "Balance follow-up", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var customer = job.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == job.CustomerId.Value) : null;
            var dueDate = DateTime.Today.AddDays(1);
            var task = new BusinessTask
            {
                TaskCode = TaskWorkflowService.GenerateTaskCode(),
                Title = title,
                Category = BusinessTaskCategory.CustomerFollowUp,
                Priority = job.Status is JobStatus.ReadyForPickup or JobStatus.ReadyToShip ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
                Status = BusinessTaskStatus.ToDo,
                DueDate = dueDate,
                ReminderDate = DateTime.Today,
                CustomerId = job.CustomerId,
                JobId = job.Id,
                Description = $"Send balance reminder for {job.JobCode} {job.JobTitle}. Balance owing: {balance:C}. Total: {total:C}. Paid: {paid:C}.",
                FollowUpNotes = BuildBalanceReminderMessage(job, customer, total, paid, balance),
                ShowOnDashboard = true
            };
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            StatusMessageText.Text = $"Created balance follow-up {task.TaskCode}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Balance follow-up", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static decimal CalculateCurrentBalance(AppDbContext db, Job job, out decimal total, out decimal paid)
    {
        total = GetJobTotal(job);
        var payments = db.Payments.AsNoTracking().Where(x => x.JobId == job.Id).Select(x => x.Amount).ToList().Sum();
        paid = Math.Max(job.DepositPaid, payments);
        return Math.Max(Math.Max(0, total - paid), Math.Max(0, job.BalanceOwing));
    }

    private static string BuildBalanceReminderMessage(Job job, Customer? customer, decimal total, decimal paid, decimal balance)
    {
        var name = string.IsNullOrWhiteSpace(customer?.FullName) ? "there" : customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? customer.FullName;
        var dueLine = job.DueDate.HasValue ? $" The current due/handover date is {job.DueDate.Value:dd MMM yyyy}." : string.Empty;
        return
            $"Hi {name},\n\n" +
            $"Just a quick note about {job.JobCode} {job.JobTitle}.\n\n" +
            $"The total is {total:C}, with {paid:C} recorded as paid. The remaining balance is {balance:C}.{dueLine}\n\n" +
            "Please let me know if you would like the payment details resent or if you have already paid and would like me to check the record.\n\n" +
            "Kind regards";
    }

    private void CreatePickupReminder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            const string titlePrefix = "Pickup / handover reminder";
            if (TaskWorkflowService.OpenTaskExists(db, titleStartsWith: titlePrefix, jobId: job.Id))
            {
                MessageBox.Show("An open pickup / handover reminder already exists for this job.", "Pickup reminder", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var task = new BusinessTask
            {
                TaskCode = TaskWorkflowService.GenerateTaskCode(),
                Title = $"{titlePrefix} - {job.JobCode}",
                Category = BusinessTaskCategory.CustomerFollowUp,
                Priority = BusinessTaskPriority.High,
                Status = BusinessTaskStatus.ToDo,
                DueDate = DateTime.Today.AddDays(1),
                ReminderDate = DateTime.Today,
                CustomerId = job.CustomerId,
                JobId = job.Id,
                Description = $"Contact customer about collection/shipping and outstanding balance for {job.JobTitle}.",
                FollowUpNotes = HandoverNotesBox.Text.Trim(),
                ShowOnDashboard = true
            };
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            StatusMessageText.Text = "Pickup / handover reminder created.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pickup reminder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarkReadyForCollection_Click(object sender, RoutedEventArgs e) => UpdateJobStatus(JobStatus.ReadyForPickup, "Marked ready for collection.");
    private void MarkReadyToShip_Click(object sender, RoutedEventArgs e) => UpdateJobStatus(JobStatus.ReadyToShip, "Marked ready to ship.");

    private void GenerateHandoverConfirmation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            var path = DocumentExportService.CreateHandoverConfirmationFromJob(job, HandoverNotesBox.Text.Trim());
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusMessageText.Text = "Handover confirmation generated.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Handover confirmation", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateThankYouFollowUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;

            var title = $"Thank-you follow-up - {job.JobCode}";
            if (TaskWorkflowService.OpenTaskExists(db, exactTitle: title, jobId: job.Id))
            {
                MessageBox.Show("An open thank-you follow-up already exists for this job.", "Thank-you follow-up", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var customer = job.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == job.CustomerId.Value) : null;
            var dueDate = DateTime.Today.AddDays(2);
            var task = new BusinessTask
            {
                TaskCode = TaskWorkflowService.GenerateTaskCode(),
                Title = title,
                Category = BusinessTaskCategory.CustomerFollowUp,
                Priority = BusinessTaskPriority.Normal,
                Status = BusinessTaskStatus.ToDo,
                DueDate = dueDate,
                ReminderDate = DateTime.Today.AddDays(1),
                CustomerId = job.CustomerId,
                JobId = job.Id,
                Description = $"Send a thank-you or after-care follow-up for {job.JobCode} {job.JobTitle}.",
                FollowUpNotes = BuildThankYouFollowUpMessage(job, customer),
                ShowOnDashboard = true
            };
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            StatusMessageText.Text = $"Created thank-you follow-up {task.TaskCode}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Thank-you follow-up", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarkCollected_Click(object sender, RoutedEventArgs e)
    {
        var allowBalance = ConfirmBalanceBeforeComplete();
        if (allowBalance != true) return;
        CompleteSelectedJob("Collected by customer and marked complete." + FormatHandoverNote(), allowOutstandingBalanceDefault: true);
    }

    private void MarkShipped_Click(object sender, RoutedEventArgs e)
    {
        var allowBalance = ConfirmBalanceBeforeComplete();
        if (allowBalance != true) return;
        CompleteSelectedJob("Shipped to customer and marked complete." + FormatHandoverNote(), allowOutstandingBalanceDefault: true);
    }

    private bool? ConfirmBalanceBeforeComplete()
    {
        if (_selectedRow == null) return false;
        if (_selectedRow.Balance <= 0m) return true;
        var result = MessageBox.Show($"This job still shows a balance of {_selectedRow.Balance:C}. Mark complete anyway?", "Outstanding balance", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private void UpdateJobStatus(JobStatus status, string note)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;
            job.Status = status;
            AppendNote(job, note + FormatHandoverNote());
            db.SaveChanges();
            StatusMessageText.Text = note;
            LoadJobs();
            SelectJob(job.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Update job", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CompleteSelectedJob(string note, bool allowOutstandingBalanceDefault)
    {
        if (_selectedRow == null)
        {
            MessageBox.Show("Select a job first.", "Payment & Collection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var jobId = _selectedRow.JobId;
        var dialog = new JobCompletionWindow(jobId, note, allowOutstandingBalanceDefault) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            StatusMessageText.Text = dialog.CompletionResult?.Summary ?? "Job completed.";
            LoadJobs();
            SelectJob(jobId);
        }
    }

    private void CreateSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var job = GetSelectedJob(db);
            if (job == null) return;
            var existing = db.Sales.FirstOrDefault(x => x.JobId == job.Id);
            if (existing != null)
            {
                MessageBox.Show($"A sale already exists for this job: Sale #{existing.Id}.", "Create sale", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var amount = GetJobTotal(job);
            var sale = new Sale
            {
                JobId = job.Id,
                CustomerId = job.CustomerId,
                SaleDate = DateTime.Today,
                SaleAmount = amount,
                CostOfGoods = job.MaterialCost + job.LabourCost,
                PaymentMethod = PaymentMethodCombo.SelectedItem is PaymentMethod method ? method : PaymentMethod.Card,
                SaleLocation = SaleLocation.CustomOrder,
                Notes = $"Created from Payment & Collection workflow for {job.JobCode}. {HandoverNotesBox.Text.Trim()}"
            };
            db.Sales.Add(sale);
            AppendNote(job, "Sale record created from job.");
            db.SaveChanges();
            StatusMessageText.Text = $"Created Sale #{sale.Id} from {job.JobCode}.";
            LoadJobs();
            SelectJob(job.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Create sale", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void AppendNote(Job job, string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;
        var stamped = $"[{DateTime.Now:g}] {note}";
        job.InternalNotes = string.IsNullOrWhiteSpace(job.InternalNotes) ? stamped : job.InternalNotes + Environment.NewLine + stamped;
    }

    private string FormatHandoverNote()
    {
        var note = HandoverNotesBox.Text.Trim();
        return string.IsNullOrWhiteSpace(note) ? string.Empty : $" Notes: {note}";
    }

    private static string BuildThankYouFollowUpMessage(Job job, Customer? customer)
    {
        var name = string.IsNullOrWhiteSpace(customer?.FullName) ? "there" : customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? customer.FullName;
        return
            $"Hi {name},\n\n" +
            $"Thank you again for choosing us for {job.JobCode} {job.JobTitle}.\n\n" +
            "I just wanted to check that everything is fitting and looking as expected. Please let me know if you have any questions about care, cleaning or future adjustments.\n\n" +
            "Kind regards";
    }

    private void SelectJob(int jobId)
    {
        ApplyFilters();
        var item = JobsList.Items.Cast<JobPaymentRow>().FirstOrDefault(x => x.JobId == jobId);
        if (item != null)
            JobsList.SelectedItem = item;
        else
            LoadSelectedJobDetails();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadJobs();
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_loading) ApplyFilters(); }
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_loading && IsLoaded) ApplyFilters(); }

    private sealed record JobPaymentRow(int JobId, string JobCode, string JobTitle, string CustomerName, JobStatus Status, DateTime? DueDate, decimal Total, decimal Paid, decimal Balance, bool HasSale)
    {
        public string TitleLine => $"{JobCode} {JobTitle}".Trim();
        public string CustomerLine => CustomerName;
        public string StatusLine => $"{Status} · Due {(DueDate.HasValue ? DueDate.Value.ToString("d MMM yyyy") : "not set")} · Sale {(HasSale ? "created" : "not yet")}";
        public string MoneyLine => $"Total {Total:C} · Paid {Paid:C} · Balance {Balance:C}";
    }
}
