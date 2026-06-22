using System.Diagnostics;
using System.Text;
using System.Windows;

namespace JewelleryBusinessManager.Views;

public partial class SendProposalWindow : Window
{
    private readonly string _proposalPath;

    public SendProposalWindow(
        string quoteCode,
        string quoteTitle,
        string? customerName,
        string? customerEmail,
        string proposalPath,
        string subject,
        string message,
        DateTime suggestedFollowUpDate)
    {
        InitializeComponent();

        _proposalPath = proposalPath;
        HeaderText.Text = $"{quoteCode} {quoteTitle}".Trim() + $" | Prepared for {customerName ?? "customer"}";
        ToBox.Text = customerEmail ?? string.Empty;
        SubjectBox.Text = subject;
        MessageBodyBox.Text = message;
        FollowUpDatePicker.SelectedDate = suggestedFollowUpDate;
        ProposalPathText.Text = $"Proposal output: {proposalPath}";
    }

    public string EmailTo => ToBox.Text.Trim();
    public string EmailSubject => SubjectBox.Text.Trim();
    public string EmailMessage => MessageBodyBox.Text.Trim();
    public bool CreateFollowUp => CreateFollowUpCheck.IsChecked == true;
    public DateTime? FollowUpDueDate => FollowUpDatePicker.SelectedDate;

    private void OpenProposal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_proposalPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open proposal", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyDraft_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildClipboardDraft());
            MessageBox.Show(this, "Proposal email draft copied to the clipboard.", "Copy email draft", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Copy email draft", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenEmailDraft_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildClipboardDraft());
            var mailto = BuildMailToUri();
            if (mailto.Length > 1800)
            {
                MessageBox.Show(this, "The email body is too long for a reliable mail draft link, so the draft was copied to the clipboard instead.", "Open email draft", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open email draft", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string BuildClipboardDraft()
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(EmailTo))
            builder.AppendLine($"To: {EmailTo}");
        if (!string.IsNullOrWhiteSpace(EmailSubject))
            builder.AppendLine($"Subject: {EmailSubject}");
        builder.AppendLine();
        builder.AppendLine(EmailMessage);
        builder.AppendLine();
        builder.AppendLine($"Proposal file: {_proposalPath}");
        return builder.ToString().Trim();
    }

    private string BuildMailToUri()
    {
        var to = Uri.EscapeDataString(EmailTo);
        var subject = Uri.EscapeDataString(EmailSubject);
        var body = Uri.EscapeDataString(EmailMessage + Environment.NewLine + Environment.NewLine + "Proposal file: " + _proposalPath);
        return $"mailto:{to}?subject={subject}&body={body}";
    }

    private void RecordSent_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailTo))
        {
            var result = MessageBox.Show(this, "No recipient email is entered. Record this proposal as sent anyway?", "Record sent", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
        }

        if (string.IsNullOrWhiteSpace(EmailSubject) || string.IsNullOrWhiteSpace(EmailMessage))
        {
            MessageBox.Show(this, "Enter both a subject and message before recording the proposal as sent.", "Record sent", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
