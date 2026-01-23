using System.Diagnostics;
using System.ServiceProcess;

namespace KeyRecorder.UI;

public class ServiceManager
{
    private const string ServiceName = "KeyRecorder Service";

    public static ServiceControllerStatus? GetServiceStatus()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            return service.Status;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsServiceInstalled()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            var _ = service.Status;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsServiceRunning()
    {
        var status = GetServiceStatus();
        return status == ServiceControllerStatus.Running;
    }

    public static (bool success, string message) TryStartService()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return (false, "KeyRecorder Service is not installed. Please run the installer.");
            }

            var status = GetServiceStatus();
            if (status == ServiceControllerStatus.Running)
            {
                return (true, "Service is already running.");
            }

            // Try to start using sc.exe with admin privileges
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"start \"{ServiceName}\"",
                Verb = "runas", // Request admin elevation
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit(5000);

            // Wait a bit for service to start
            Thread.Sleep(2000);

            if (IsServiceRunning())
            {
                return (true, "Service started successfully.");
            }

            return (false, "Service failed to start. Please check Windows Event Log for details.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
            return (false, "Administrator privileges required to start the service.");
        }
        catch (Exception ex)
        {
            return (false, $"Error starting service: {ex.Message}");
        }
    }

    public static (bool success, string message) TryRestartService()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return (false, "KeyRecorder Service is not installed.");
            }

            // Try to stop first
            var stopInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"stop \"{ServiceName}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var stopProcess = Process.Start(stopInfo);
            stopProcess?.WaitForExit(5000);

            Thread.Sleep(2000);

            // Now start
            return TryStartService();
        }
        catch (Exception ex)
        {
            return (false, $"Error restarting service: {ex.Message}");
        }
    }
}
