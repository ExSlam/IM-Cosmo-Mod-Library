using System.Runtime.InteropServices;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Gui;

namespace IMDataCore.DataMigrationTool;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        AppLog.EnsureInitialized();
        AppLog.Info("Process entry point reached. Arguments: " + (args.Length == 0 ? "(none)" : string.Join(" ", args)));

        if (args.Length == 0 || string.Equals(args[0], "gui", StringComparison.OrdinalIgnoreCase))
        {
            return RunGui();
        }

        ConsoleHost.AttachOrAllocate();
        try
        {
            AppLog.Info("Starting CLI mode.");
            int exitCode = Cli.Run(args);
            AppLog.Info("CLI exited with code " + exitCode + ".");
            return exitCode;
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled CLI exception.", ex);
            Console.Error.WriteLine("ERROR: " + ex.Message);
            Console.Error.WriteLine("Log: " + AppLog.SessionLogPath);
            return 2;
        }
    }

    private static int RunGui()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            AppLog.Error("Unhandled WinForms UI-thread exception.", e.Exception);
            ShowFatalUiError(e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Exception? ex = e.ExceptionObject as Exception;
            AppLog.Error("Unhandled AppDomain exception. Terminating=" + e.IsTerminating, ex);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Error("Unobserved task exception.", e.Exception);
            e.SetObserved();
        };

        try
        {
            string detectedLanguage = Localization.InitializeFromSystemCulture();
            AppLog.Info($"GUI language auto-detected from system UI culture '{System.Globalization.CultureInfo.CurrentUICulture.Name}' as '{detectedLanguage}'.");
            AppLog.Info("Initializing WinForms application configuration.");
            ApplicationConfiguration.Initialize();
            AppLog.Info("Constructing MainForm.");
            using var form = new MainForm();
            AppLog.Info("MainForm constructed successfully; entering message loop.");
            Application.Run(form);
            AppLog.Info("GUI message loop exited normally.");
            return 0;
        }
        catch (Exception ex)
        {
            AppLog.Error("Fatal exception while launching GUI.", ex);
            ShowFatalUiError(ex);
            return 3;
        }
    }

    private static void ShowFatalUiError(Exception ex)
    {
        try
        {
            MessageBox.Show(
                Localization.Format("startup_error_message", Localization.T("title"), ex.GetType().Name, ex.Message, AppLog.SessionLogPath),
                Localization.T("startup_error_title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Last-resort path. The log remains available even if MessageBox fails.
        }
    }
}

internal static class ConsoleHost
{
    private const uint AttachParentProcess = 0xFFFFFFFF;

    internal static void AttachOrAllocate()
    {
        if (!AttachConsole(AttachParentProcess)) AllocConsole();
        try
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }
        catch { }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}
