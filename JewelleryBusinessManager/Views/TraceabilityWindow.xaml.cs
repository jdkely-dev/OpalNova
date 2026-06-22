using System.Windows;

namespace JewelleryBusinessManager.Views;

public partial class TraceabilityWindow : Window
{
    public TraceabilityWindow(string traceText)
    {
        InitializeComponent();
        TraceTextBox.Text = traceText;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
