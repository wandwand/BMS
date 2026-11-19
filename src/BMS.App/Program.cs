using BMS.Core.Domain;

namespace BMS.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var controlador = ControladorBMS.CriarConfiguracaoPadrao();

        if (args.Any(a => a.Equals("--console", StringComparison.OrdinalIgnoreCase)))
        {
            ConsoleUI.Run(controlador);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(controlador));
    }
}
