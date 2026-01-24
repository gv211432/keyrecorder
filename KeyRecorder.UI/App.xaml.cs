using System.Windows;

namespace KeyRecorder.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static bool StartMinimized { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
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
}
