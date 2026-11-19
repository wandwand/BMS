using BMS.Core.Domain;
using BMS.Core.Simulation;

namespace BMS.App;

public static class ConsoleUI
{
    public static void Run(ControladorBMS bms)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool executando = true;

        while (executando)
        {
            Console.WriteLine();
            Console.WriteLine("==========================================================");
            Console.WriteLine("   BMS UFC + BMSLogic (C# / .NET 10) - Modo Console       ");
            Console.WriteLine("==========================================================");
            Console.WriteLine("1. Varredura e Telemetria Completa (SoC Real vs Kalman)");
            Console.WriteLine("2. Simular Ciclo de Carga (60s)");
            Console.WriteLine("3. Simular Ciclo de Descarga (60s)");
            Console.WriteLine("4. Executar Ciclador CC-CV (bmslogic) na Tesla NCA 2170");
            Console.WriteLine("5. Injetar Falha Simulada (Sobretensão + Superaquecimento)");
            Console.WriteLine("6. Executar Balanceamento Passivo");
            Console.WriteLine("7. Histórico de Alertas");
            Console.WriteLine("8. Ativar Modo Turbo (Li-Ion) e Flutuação (LiFePO4)");
            Console.WriteLine("0. Sair");
            Console.Write("Escolha uma opção: ");

            string? opcao = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (opcao)
            {
                case "1":
                    bms.RealizarVarreduraSeguranca();
                    Console.WriteLine(bms.GerarRelatorioTelemetria());
                    break;
                case "2":
                    bms.SimularCicloCarga(60.0);
                    Console.WriteLine("Ciclo de carga (60s) concluído.");
                    Console.WriteLine(bms.GerarRelatorioTelemetria());
                    break;
                case "3":
                    bms.SimularCicloDescarga(60.0);
                    Console.WriteLine("Ciclo de descarga (60s) concluído.");
                    Console.WriteLine(bms.GerarRelatorioTelemetria());
                    break;
                case "4":
                    var cycler = new CcCvCycler(
                        numCycles: 1,
                        chargeCurrentA: 3.25,
                        dischargeCurrentA: 3.25,
                        restTimeS: 10.0,
                        vMax: 4.20,
                        vMin: 2.50,
                        cvCutoffCurrentA: 0.1);
                    var sim = bms.ExecutarCiclador("CELL-NCA-01", cycler, dt: 5.0);
                    var ultimo = sim.Points[^1];
                    Console.WriteLine($"Ciclador CC-CV concluído: {sim.Points.Count} passos | t={ultimo.TimeS:F0}s | " +
                                      $"V={ultimo.VoltageV:F3}V | T={ultimo.TempC:F1}°C | " +
                                      $"SoC={ultimo.SocTrue * 100:F1}% (Kalman={ultimo.SocKalman * 100:F1}%)");
                    break;
                case "5":
                    bms.InjetarFalhaSimulada();
                    Console.WriteLine("Falhas simuladas injetadas.");
                    break;
                case "6":
                    int count = bms.ExecutarBalanceamentoPassivo();
                    Console.WriteLine($"Balanceamento passivo executado em {count} módulo(s).");
                    break;
                case "7":
                    Console.WriteLine($"Total global de alertas gerados: {AlertaSistema.TotalAlertasGerados}");
                    foreach (var alerta in bms.HistoricoAlertas)
                        Console.WriteLine($"  {alerta}");
                    break;
                case "8":
                    bms.AtivarModoTurboEmLiIon();
                    bms.AtivarModoFlutuacaoEmLiFePO4();
                    Console.WriteLine("Modos especiais acionados (Turbo Li-Ion / Flutuação LiFePO4).");
                    break;
                case "0":
                    executando = false;
                    break;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
    }
}
