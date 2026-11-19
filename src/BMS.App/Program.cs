using System.Runtime.InteropServices;
using BMS.Core.Domain;

namespace BMS.App;

internal static class Program
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [STAThread]
    private static void Main(string[] args)
    {
        var controlador = ControladorBMS.CriarConfiguracaoPadrao();

        if (args.Any(a => a.Equals("--console", StringComparison.OrdinalIgnoreCase)))
        {
            if (!AttachConsole(AttachParentProcess))
                AllocConsole();
            ConsoleUI.Run(controlador);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(controlador));
    }
}
