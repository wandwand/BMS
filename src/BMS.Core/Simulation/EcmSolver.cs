using BMS.Core.CalcHelpers;
using BMS.Core.Domain;
using BMS.Core.Models;

namespace BMS.Core.Simulation;

public sealed record EcmDataPoint(
    double TimeS,
    double CurrentA,
    double VoltageV,
    double TempK,
    double TempC,
    double SocTrue,
    double SocKalman,
    double IR1A,
    CycleStepType Step);

public sealed class EcmSimulationResult
{
    private readonly List<EcmDataPoint> _points = new();
    public IReadOnlyList<EcmDataPoint> Points => _points;

    public void Add(EcmDataPoint point) => _points.Add(point);
}

/// <summary>
/// Discrete-time ESC + Lumped Thermal + CDKF solver, ported from bmslogic PyESCDTSolver.
/// </summary>
public sealed class EscDtSolver
{
    public bool Isothermal { get; }

    public EscDtSolver(bool isothermal = false)
    {
        Isothermal = isothermal;
    }

    public EcmSimulationResult Solve(CelulaBateria cell, BaseCycler cycler, double dt = 1.0, int maxStepsPerPhase = 20000)
    {
        var result = new EcmSimulationResult();
        cycler.Reset();

        for (int cycle = 0; cycle < cycler.NumCycles; cycle++)
        {
            foreach (var step in cycler.CycleSteps)
            {
                double tPhase = 0.0;
                bool completed = false;
                int guard = 0;

                while (!completed && guard++ < maxStepsPerPhase)
                {
                    tPhase += dt;
                    double iApp;

                    if (step == CycleStepType.CvCharge)
                    {
                        // Constant-Voltage taper: compute current needed to hold V_max
                        double ocv = cell.Ocv;
                        double r0 = cell.R0;
                        double r1 = cell.R1;
                        double iCv = (ocv - r1 * cell.IR1 - cycler.VMax) / Math.Max(r0, 1e-6);
                        // Charge current is negative in ESC convention; clamp magnitude to ChargeCurrent
                        iApp = Math.Clamp(iCv, -cycler.ChargeCurrent, 0.0);
                        if (Math.Abs(iApp) <= cycler.CvCutoffCurrent || cell.Soc >= 0.999)
                            completed = true;
                    }
                    else
                    {
                        iApp = -cycler.GetCurrent(step, tPhase);
                    }

                    cell.SimularPasso(iApp, dt, isothermal: Isothermal);
                    cycler.TimeElapsed += dt;

                    result.Add(new EcmDataPoint(
                        TimeS: cycler.TimeElapsed,
                        CurrentA: iApp,
                        VoltageV: cell.TensaoAtual,
                        TempK: cell.TemperaturaKelvin,
                        TempC: cell.TemperaturaCelsius,
                        SocTrue: cell.Soc,
                        SocKalman: cell.SocEstimadoKalman,
                        IR1A: cell.IR1,
                        Step: step));

                    if (step == CycleStepType.Charge && (cell.TensaoAtual >= cycler.VMax || cell.Soc >= 0.999))
                        completed = true;
                    else if (step == CycleStepType.Discharge && (cell.TensaoAtual <= cycler.VMin || cell.Soc <= 0.001))
                        completed = true;
                    else if (step == CycleStepType.Rest && tPhase >= cycler.RestTime)
                        completed = true;
                }
            }
        }

        return result;
    }
}
