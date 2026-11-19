namespace BMS.Core.Simulation;

public enum CycleStepType
{
    Rest,
    Charge,
    Discharge,
    CvCharge
}

/// <summary>
/// Base battery cell cycler, ported from bmslogic PyBaseCycler.
/// Note: Charge current is positive here; discharge current is negative (solver negates to match ESC sign convention).
/// </summary>
public abstract class BaseCycler
{
    public double TimeElapsed { get; set; }
    public double ChargeCurrent { get; protected init; }
    public double DischargeCurrent { get; protected init; }
    public double RestTime { get; protected init; }
    public double VMin { get; protected init; } = 2.5;
    public double VMax { get; protected init; } = 4.2;
    public double CvCutoffCurrent { get; protected init; } = 0.05;
    public int NumCycles { get; protected init; } = 1;
    public IReadOnlyList<CycleStepType> CycleSteps { get; protected init; } = Array.Empty<CycleStepType>();

    public virtual double GetCurrent(CycleStepType step, double t) => step switch
    {
        CycleStepType.Rest => 0.0,
        CycleStepType.Charge => ChargeCurrent,
        CycleStepType.Discharge => DischargeCurrent,
        CycleStepType.CvCharge => ChargeCurrent,
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Invalid cycle step.")
    };

    public virtual void Reset() => TimeElapsed = 0.0;
}

/// <summary>
/// Constant-Current (CC) cycler: Rest -> Charge -> Rest -> Discharge.
/// </summary>
public class CcCycler : BaseCycler
{
    public CcCycler(int numCycles, double chargeCurrentA, double dischargeCurrentA, double restTimeS, double vMax, double vMin)
    {
        NumCycles = numCycles;
        ChargeCurrent = Math.Abs(chargeCurrentA);
        DischargeCurrent = -Math.Abs(dischargeCurrentA);
        RestTime = restTimeS;
        VMax = vMax;
        VMin = vMin;
        CycleSteps = [CycleStepType.Rest, CycleStepType.Charge, CycleStepType.Rest, CycleStepType.Discharge];
    }
}

/// <summary>
/// Constant-Current Constant-Voltage (CC-CV) cycler: Charge (CC) -> CvCharge (CV) -> Rest -> Discharge.
/// </summary>
public class CcCvCycler : BaseCycler
{
    public CcCvCycler(
        int numCycles,
        double chargeCurrentA,
        double dischargeCurrentA,
        double restTimeS,
        double vMax,
        double vMin,
        double cvCutoffCurrentA = 0.05)
    {
        NumCycles = numCycles;
        ChargeCurrent = Math.Abs(chargeCurrentA);
        DischargeCurrent = -Math.Abs(dischargeCurrentA);
        RestTime = restTimeS;
        VMax = vMax;
        VMin = vMin;
        CvCutoffCurrent = Math.Abs(cvCutoffCurrentA);
        CycleSteps = [CycleStepType.Charge, CycleStepType.CvCharge, CycleStepType.Rest, CycleStepType.Discharge];
    }
}

/// <summary>
/// Single-step Charge cycler.
/// </summary>
public class ChargeCycler : BaseCycler
{
    public ChargeCycler(double chargeCurrentA, double vMax, double vMin = 2.5)
    {
        NumCycles = 1;
        ChargeCurrent = Math.Abs(chargeCurrentA);
        VMax = vMax;
        VMin = vMin;
        CycleSteps = [CycleStepType.Charge];
    }
}

/// <summary>
/// Single-step Discharge cycler.
/// </summary>
public class DischargeCycler : BaseCycler
{
    public DischargeCycler(double dischargeCurrentA, double vMin, double vMax = 4.2)
    {
        NumCycles = 1;
        DischargeCurrent = -Math.Abs(dischargeCurrentA);
        VMax = vMax;
        VMin = vMin;
        CycleSteps = [CycleStepType.Discharge];
    }
}
