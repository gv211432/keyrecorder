using System.Windows;

namespace KeyRecorder.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    public static bool StartMinimized { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance check using Mutex
        const string mutexName = "KeyRecorder_SingleInstance_Mutex";
        bool createdNew;

        _mutex = new Mutex(true, mutexName, out createdNew);

        if (!createdNew)
        {
            // Another instance is already running - bring it to focus
            System.Windows.MessageBox.Show(
                "KeyRecorder is already running.\n\nCheck the system tray for the KeyRecorder icon.",
                "KeyRecorder Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Check for --minimized argument
        foreach (var arg in e.Args)
        {
            if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-m", StringComparison.OrdinalIgnoreCase))
            {
                StartMinimized = true;
                break;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
