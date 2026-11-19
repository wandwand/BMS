using BMS.Core.CalcHelpers;
using BMS.Core.Domain;
using BMS.Core.Models;
using BMS.Core.Parameters;
using BMS.Core.Simulation;

namespace BMS.Tests;

public class BmsTests
{
    [Fact]
    public void TheveninAndEscModel_DischargeDecreasesSocAndVoltage()
    {
        double soc0 = 0.80;
        double dt = 10.0;
        double iApp = 2.0; // positive = discharge
        double soc1 = EscModel.SocNext(dt, iApp, soc0, capacityAh: 2.0, eta: 1.0);
        double iR1 = EscModel.IR1Next(dt, iApp, iR1Prev: 0.0, r1: 0.01, c1: 100.0);
        double v = EscModel.Voltage(iApp, ocv: 3.9, r0: 0.05, r1: 0.01, iR1: iR1, m0: 0.0, m: 0.0, h: 0.0, sPrev: 0.0);

        Assert.True(soc1 < soc0);
        Assert.True(iR1 > 0.0);
        Assert.True(v < 3.9);
    }

    [Fact]
    public void ParameterSets_LoadFromCsvFiles()
    {
        var ecmTest = EcmParameterSet.CreateEcmTest();
        var nca = EcmParameterSet.CreateTeslaNca2170();
        var lfp = EcmParameterSet.CreateLiFePO4();

        Assert.Equal(1.65, ecmTest.CapacityAh, 2);
        Assert.Equal(6.5, nca.CapacityAh, 2);
        Assert.Equal(3.2, lfp.CapacityAh, 2);
        Assert.Equal(3.65, lfp.VMax, 2);
    }

    [Fact]
    public void LumpedThermalModel_HeatsUpDuringDischarge()
    {
        double t0 = 298.15;
        double t1 = EcmLumpedThermalModel.StepTemperatureRk4(
            tPrev: 0.0, dt: 10.0, tempPrevK: t0, voltage: 3.4, iApp: 10.0,
            rho: 1626.0, vol: 3.38e-5, cp: 750.0, ocv: 3.8, dOcvDT: -0.0002,
            h: 1.0, area: 0.085, tempAmbK: 298.15);

        Assert.True(t1 > t0);
    }

    [Fact]
    public void KalmanFilter_ConvergesTowardTrueSoc()
    {
        var cell = new CelulaLiIon("LI-KF", "Teste Kalman", socInicial: 0.60);
        double erroInicial = Math.Abs(cell.SocEstimadoKalman - cell.Soc);

        // Run 30 steps of 1s discharge
        for (int i = 0; i < 30; i++)
            cell.SimularPasso(1.65, dt: 1.0);

        double erroFinal = Math.Abs(cell.SocEstimadoKalman - cell.Soc);
        Assert.True(erroFinal < erroInicial, $"Expected Kalman error to decrease: initial={erroInicial:F4}, final={erroFinal:F4}");
    }

    [Fact]
    public void LiFePO4_EntersFloatModeNearFullCharge()
    {
        var lfp = new CelulaLiFePO4("LFP-1", "Célula LFP", socInicial: 0.96);
        lfp.SimularCarga(10.0);

        Assert.True(lfp.ModoFlutuacaoAtivo);
        Assert.True(lfp.TensaoAtual <= 3.45 + 1e-6);
    }

    [Fact]
    public void LiIon_TurboModeAutoDeactivatesAt80PercentSoc()
    {
        var li = new CelulaLiIon("LI-1", "Célula Li-Ion", socInicial: 0.79);
        li.AtivarModoTurbo();
        Assert.True(li.ModoTurboAtivo);

        li.SimularCarga(60.0);
        Assert.False(li.ModoTurboAtivo);
        Assert.NotNull(li.UltimoMotivoDesativacaoTurbo);
    }

    [Fact]
    public void ModuloBateria_ExecutesPassiveBalancingWhenDeltaExceedsThreshold()
    {
        var mod = new ModuloBateria("MOD-1", "Módulo Teste");
        var c1 = new CelulaLiIon("C1", "Célula 1", socInicial: 0.90);
        var c2 = new CelulaLiIon("C2", "Célula 2", socInicial: 0.30);
        mod.AdicionarCelula(c1);
        mod.AdicionarCelula(c2);

        double deltaAntes = mod.CalcularDeltaTensao();
        Assert.True(deltaAntes > ModuloBateria.LimiarDesbalanceamentoVolts);
        Assert.True(mod.PrecisaBalanceamento());

        mod.ExecutarBalanceamento();
        double deltaDepois = mod.CalcularDeltaTensao();
        Assert.True(deltaDepois < deltaAntes);
    }

    [Fact]
    public void EscDtSolver_RunsCcCvCyclerOnNca2170()
    {
        var nca = new CelulaNca2170("NCA-1", "Tesla NCA 2170", socInicial: 0.50);
        var cycler = new CcCvCycler(1, 3.25, 3.25, restTimeS: 10.0, vMax: 4.20, vMin: 2.50, cvCutoffCurrentA: 0.2);
        var solver = new EscDtSolver(isothermal: false);

        var result = solver.Solve(nca, cycler, dt: 10.0, maxStepsPerPhase: 5000);

        Assert.NotEmpty(result.Points);
        Assert.Contains(result.Points, p => p.Step == CycleStepType.Charge);
        Assert.Contains(result.Points, p => p.Step == CycleStepType.Discharge);
    }

    [Fact]
    public void ControladorBMS_FaultInjectionGeneratesAlertsAndFailsSafetyCheck()
    {
        var bms = ControladorBMS.CriarConfiguracaoPadrao();
        Assert.True(bms.VerificarSeguranca());

        int alertsBefore = AlertaSistema.TotalAlertasGerados;
        bms.InjetarFalhaSimulada();

        Assert.False(bms.VerificarSeguranca());
        Assert.True(AlertaSistema.TotalAlertasGerados > alertsBefore);
        Assert.NotEmpty(bms.HistoricoAlertas);
    }
}
