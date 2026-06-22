using System.Windows;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Apply any staged restore before SQLite/EF opens the active database.
        DatabaseBootstrapper.ApplyPendingRestoreIfNeeded();

        // Initialise folders and the SQLite database before WPF creates the startup window.
        // This avoids first-run failures when MainWindow loads dashboard counts immediately.
        DatabaseBootstrapper.Initialize();
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLogService.Log(args.Exception, "Unhandled WPF exception");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ErrorLogService.Log(ex, "Unhandled application exception");
        };
        base.OnStartup(e);
    }
}
