using System.Globalization;
using System.Windows;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class JobCompletionWindow : Window
{
    private readonly int _jobId;
    private readonly bool _allowOutstandingBalanceDefault;
    private JobCompletionReview? _review;

    public JobCompletionResult? CompletionResult { get; private set; }

    public JobCompletionWindow(int jobId, string completionNote = "", bool allowOutstandingBalanceDefault = false)
    {
        InitializeComponent();
        _jobId = jobId;
        _allowOutstandingBalanceDefault = allowOutstandingBalanceDefault;
        CompletionNoteBox.Text = completionNote;
        Loaded += (_, _) => LoadReview();
    }

    private void LoadReview()
    {
        try
        {
            _review = JobCompletionService.BuildReview(_jobId);
            JobTitleText.Text = _review.JobTitle;
            JobDetailText.Text = $"Customer: {_review.CustomerName}";
            QuoteDetailText.Text = _review.QuoteLine;
            TotalText.Text = _review.Total.ToString("C", CultureInfo.CurrentCulture);
            PaidText.Text = _review.Paid.ToString("C", CultureInfo.CurrentCulture);
            BalanceText.Text = _review.Balance.ToString("C", CultureInfo.CurrentCulture);
            MaterialsGrid.ItemsSource = _review.Materials;
            StonesGrid.ItemsSource = _review.Stones;

            ConsumeMaterialsCheck.IsEnabled = _review.HasReservedMaterials;
            ConsumeMaterialsCheck.IsChecked = _review.HasReservedMaterials;
            MarkStonesSetCheck.IsEnabled = _review.HasReservedStones;
            MarkStonesSetCheck.IsChecked = _review.HasReservedStones;
            AllowBalanceCheck.IsChecked = _allowOutstandingBalanceDefault;

            var warnings = new List<string>();
            if (_review.Balance > 0m)
                warnings.Add($"Outstanding balance: {_review.Balance:C}.");
            if (_review.Materials.Any(x => x.WillGoNegative))
                warnings.Add("One or more materials will go below zero if consumed.");
            if (!string.IsNullOrWhiteSpace(_review.Notes))
                warnings.Add(_review.Notes);

            WarningText.Text = string.Join(Environment.NewLine, warnings);
            SummaryText.Text = $"Reserved material lines: {_review.ReservedMaterialCount}. Reserved stone lines: {_review.ReservedStoneCount}. The completion action records material movements and updates reservation states in one transaction.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Job Completion", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
        }
    }

    private void CompleteJob_Click(object sender, RoutedEventArgs e)
    {
        if (_review == null)
            return;

        var balanceWarning = _review.Balance > 0m && AllowBalanceCheck.IsChecked != true;
        if (balanceWarning)
        {
            MessageBox.Show("This job still has an outstanding balance. Tick the balance confirmation before completing it.", "Job Completion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_review.Materials.Any(x => x.WillGoNegative) && AllowNegativeStockCheck.IsChecked != true)
        {
            MessageBox.Show("One or more materials will go below zero. Tick the negative-stock confirmation before completing it.", "Job Completion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var message = "Complete this job and apply the selected inventory actions?";
        if (MessageBox.Show(message, "Complete Job", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            CompletionResult = JobCompletionService.CompleteJob(_jobId, new JobCompletionOptions
            {
                ConsumeReservedMaterials = ConsumeMaterialsCheck.IsChecked == true,
                MarkReservedStonesSet = MarkStonesSetCheck.IsChecked == true,
                ReleaseUnconsumedReservations = ReleaseReservationsCheck.IsChecked == true,
                AllowNegativeMaterialStock = AllowNegativeStockCheck.IsChecked == true,
                AllowOutstandingBalance = AllowBalanceCheck.IsChecked == true,
                CompletionNote = CompletionNoteBox.Text.Trim()
            });
            StatusText.Text = CompletionResult.Summary;
            MessageBox.Show(CompletionResult.Summary, "Job Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Job Completion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
