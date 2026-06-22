using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Views;

public partial class ProductionBoardWindow : Window
{
    private sealed record Lane(JobStatus Status, string Title, string Hint);
    private sealed record BoardJob(Job Job, string Customer, string QuoteCode);

    private static readonly Lane[] Lanes =
    {
        new(JobStatus.Enquiry, "Enquiry", "New work to assess"),
        new(JobStatus.Quoted, "Awaiting Approval", "Quote sent to customer"),
        new(JobStatus.Approved, "Approved", "Approved, ready to plan"),
        new(JobStatus.DepositPaid, "Deposit Paid", "Ready to schedule"),
        new(JobStatus.AwaitingMaterials, "Materials Required", "Waiting on stock or supply"),
        new(JobStatus.InProgress, "In Production", "Active bench work"),
        new(JobStatus.Setting, "Setting", "Stone setting stage"),
        new(JobStatus.Polishing, "Polishing", "Finishing and polish"),
        new(JobStatus.QualityCheck, "Quality Check", "Final inspection"),
        new(JobStatus.AwaitingCustomerApproval, "Customer Check", "Waiting for customer decision"),
        new(JobStatus.ReadyForPickup, "Ready for Collection", "Finished and ready"),
        new(JobStatus.ReadyToShip, "Ready to Ship", "Packed for dispatch"),
        new(JobStatus.Completed, "Completed", "Finished work")
    };

    private List<BoardJob> _jobs = new();
    private BoardJob? _selected;

    public ProductionBoardWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadBoard();
    }

    private void LoadBoard()
    {
        using var db = new AppDbContext();
        var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x.FullName);
        var quoteByJob = db.CustomQuotes.AsNoTracking()
            .Where(x => x.LinkedJobId.HasValue)
            .AsEnumerable()
            .GroupBy(x => x.LinkedJobId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First().QuoteCode);

        _jobs = db.Jobs.AsNoTracking().AsEnumerable()
            .Select(j => new BoardJob(
                j,
                j.CustomerId.HasValue && customers.TryGetValue(j.CustomerId.Value, out var name) ? name : "No customer",
                quoteByJob.TryGetValue(j.Id, out var quote) ? quote : string.Empty))
            .ToList();
        RenderBoard();
    }

    private void RenderBoard()
    {
        BoardPanel.Children.Clear();
        var filtered = FilteredJobs().ToList();
        foreach (var lane in Lanes)
        {
            var laneJobs = filtered.Where(x => x.Job.Status == lane.Status).ToList();
            BoardPanel.Children.Add(CreateLane(lane, laneJobs));
        }

        var cancelled = filtered.Where(x => x.Job.Status == JobStatus.Cancelled).ToList();
        if (cancelled.Count > 0)
            BoardPanel.Children.Add(CreateLane(new Lane(JobStatus.Cancelled, "Cancelled", "Closed without completion"), cancelled));

        var overdue = filtered.Count(x => IsOverdue(x.Job));
        SummaryText.Text = $"{filtered.Count} job(s) shown  •  {overdue} overdue";
    }

    private IEnumerable<BoardJob> FilteredJobs()
    {
        IEnumerable<BoardJob> query = _jobs;
        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => $"{x.Job.JobCode} {x.Job.JobTitle} {x.Customer} {x.QuoteCode}".Contains(search, StringComparison.OrdinalIgnoreCase));
        if (OverdueOnlyCheck.IsChecked == true)
            query = query.Where(x => IsOverdue(x.Job));
        if (ActiveOnlyCheck.IsChecked == true)
            query = query.Where(x => x.Job.Status != JobStatus.Completed && x.Job.Status != JobStatus.Cancelled);

        var sort = (SortCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return sort switch
        {
            "Last updated" => query.OrderByDescending(x => x.Job.UpdatedAt),
            "Customer" => query.OrderBy(x => x.Customer).ThenBy(x => x.Job.DueDate ?? DateTime.MaxValue),
            _ => query.OrderBy(x => x.Job.DueDate ?? DateTime.MaxValue).ThenBy(x => x.Job.JobCode)
        };
    }

    private Border CreateLane(Lane lane, IReadOnlyCollection<BoardJob> jobs)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = $"{lane.Title}  {jobs.Count}", Foreground = new SolidColorBrush(Color.FromRgb(239, 234, 224)), FontSize = 16, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = lane.Hint, Foreground = new SolidColorBrush(Color.FromRgb(184, 173, 158)), FontSize = 11, Margin = new Thickness(0, 3, 0, 10), TextWrapping = TextWrapping.Wrap });
        foreach (var job in jobs) stack.Children.Add(CreateCard(job));
        if (jobs.Count == 0)
            stack.Children.Add(new TextBlock { Text = "No jobs", Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), Margin = new Thickness(4, 12, 4, 0) });

        return new Border
        {
            Width = 270,
            Margin = new Thickness(0, 0, 12, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(36, 50, 68)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack }
        };
    }

    private Border CreateCard(BoardJob item)
    {
        var overdue = IsOverdue(item.Job);
        var selected = _selected?.Job.Id == item.Job.Id;
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(item.Job.JobCode) ? $"Job #{item.Job.Id}" : item.Job.JobCode, Foreground = new SolidColorBrush(Color.FromRgb(184, 173, 158)), FontSize = 11 });
        panel.Children.Add(new TextBlock { Text = item.Job.JobTitle, Foreground = new SolidColorBrush(Color.FromRgb(239, 234, 224)), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 5), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = item.Customer, Foreground = new SolidColorBrush(Color.FromRgb(216, 206, 190)), FontSize = 12, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = item.Job.DueDate.HasValue ? $"Due {item.Job.DueDate:dd MMM yyyy}" : "No due date", Foreground = overdue ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : new SolidColorBrush(Color.FromRgb(184, 173, 158)), FontSize = 12, Margin = new Thickness(0, 5, 0, 0) });
        if (!string.IsNullOrWhiteSpace(item.QuoteCode))
            panel.Children.Add(new TextBlock { Text = $"Quote {item.QuoteCode}", Foreground = new SolidColorBrush(Color.FromRgb(125, 211, 252)), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
        var balance = item.Job.BalanceOwing > 0 ? item.Job.BalanceOwing : Math.Max(0, (item.Job.FinalPrice > 0 ? item.Job.FinalPrice : item.Job.QuoteAmount) - item.Job.DepositPaid);
        if (balance > 0)
            panel.Children.Add(new TextBlock { Text = $"Balance {balance:C}", Foreground = new SolidColorBrush(Color.FromRgb(184, 137, 46)), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });

        var card = new Border
        {
            Tag = item,
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 9),
            Background = new SolidColorBrush(selected ? Color.FromRgb(30, 64, 92) : Color.FromRgb(30, 41, 59)),
            BorderBrush = new SolidColorBrush(selected ? Color.FromRgb(56, 189, 248) : overdue ? Color.FromRgb(185, 28, 28) : Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(selected ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Cursor = Cursors.Hand,
            Child = panel
        };
        card.MouseLeftButtonUp += Card_Click;
        card.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) EditSelectedJob(item);
        };
        return card;
    }

    private static bool IsOverdue(Job job) => job.DueDate.HasValue && job.DueDate.Value.Date < DateTime.Today && job.Status != JobStatus.Completed && job.Status != JobStatus.Cancelled;

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: BoardJob item }) return;
        _selected = item;
        var next = GetAdjacentStatus(item.Job.Status, 1);
        SelectionText.Text = $"Selected: {item.Job.JobCode} {item.Job.JobTitle} • {item.Customer} • Current stage: {StageTitle(item.Job.Status)}" +
                             (next != item.Job.Status ? $" • Next: {StageTitle(next)}" : string.Empty);
        RenderBoard();
    }

    private void MoveForward_Click(object sender, RoutedEventArgs e) => MoveSelected(1);
    private void MoveBack_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void MoveSelected(int direction)
    {
        if (_selected == null)
        {
            MessageBox.Show("Select a job card first.", "Production Board", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var next = GetAdjacentStatus(_selected.Job.Status, direction);
        if (next == _selected.Job.Status)
        {
            MessageBox.Show("That job is already at the end of this workflow direction.", "Production Board", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Move '{_selected.Job.JobTitle}' from {StageTitle(_selected.Job.Status)} to {StageTitle(next)}?", "Move Job", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        using var db = new AppDbContext();
        var job = db.Jobs.Find(_selected.Job.Id);
        if (job == null) return;
        job.Status = next;
        if (next == JobStatus.Completed) job.BalanceOwing = Math.Max(0, job.BalanceOwing);
        db.SaveChanges();
        LoadBoard();
        _selected = _jobs.FirstOrDefault(x => x.Job.Id == job.Id);
        RenderBoard();
    }

    private static JobStatus GetAdjacentStatus(JobStatus status, int direction)
    {
        var pipeline = Lanes.Select(x => x.Status).ToList();
        var index = pipeline.IndexOf(status);
        if (index < 0) return direction > 0 ? JobStatus.Enquiry : status;
        var newIndex = Math.Clamp(index + direction, 0, pipeline.Count - 1);
        return pipeline[newIndex];
    }

    private static string StageTitle(JobStatus status) => Lanes.FirstOrDefault(x => x.Status == status)?.Title ?? status.ToString();

    private void EditSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null)
        {
            MessageBox.Show("Select a job card first.", "Production Board", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        EditSelectedJob(_selected);
    }

    private void EditSelectedJob(BoardJob item)
    {
        using var db = new AppDbContext();
        var job = db.Jobs.Find(item.Job.Id);
        if (job == null) return;
        var editor = new EditEntityWindow(job) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            db.Update(job);
            db.SaveChanges();
            LoadBoard();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadBoard();
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) RenderBoard(); }
    private void Filter_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) RenderBoard(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) RenderBoard(); }
}
