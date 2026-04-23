using System.Linq;
using CoreCodeSwitcher = ModeSwitcher.Core.CodeSwitcher;
using ModeSwitcher.Core;

namespace ModeSwitcher.UI;

static class Program
{
    private static bool _enableLogging = false;

    [STAThread]
    static void Main(string[] args)
    {
        // Enable logging only with /debug flag
        _enableLogging = args.Any(a => a.Equals("/debug", StringComparison.OrdinalIgnoreCase) ||
                                     a.Equals("--debug", StringComparison.OrdinalIgnoreCase) ||
                                     a.Equals("//debug", StringComparison.OrdinalIgnoreCase));

        try
        {
            Log("Application starting...");
            ApplicationConfiguration.Initialize();

            Log("Creating CodeSwitcher...");
            var switcher = new CoreCodeSwitcher();

            // Test mode: init only, then exit
            if (args.Length > 0 && args[0] == "--test")
            {
                Log("Test mode: initializing MainForm without showing...");
                var form = new MainForm(switcher);
                Log("Test mode: MainForm created successfully");
                Log("Application exiting normally (test mode).");
                Environment.Exit(0);
                return;
            }

            Log("Creating MainForm...");
            Application.Run(new MainForm(switcher));

            Log("Application exited normally.");
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Ошибка запуска:\n{ex.Message}\n\nПодробнее в log.txt рядом с программой.",
                "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Log(string message)
    {
        if (!_enableLogging) return;

        try
        {
            // Use current directory for log file
            var logDir = Environment.CurrentDirectory;

            var logPath = Path.Combine(logDir, "log.txt");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Silently fail if logging fails
        }
    }
}
