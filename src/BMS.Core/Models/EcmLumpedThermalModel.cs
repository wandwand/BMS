using BMS.Core.CalcHelpers;

namespace BMS.Core.Models;

/// <summary>
/// Lumped thermal model for ECM battery cells, ported from bmslogic PyECMLumped + RK4 solver.
/// </summary>
public static class EcmLumpedThermalModel
{
    public static double ReversibleHeat(double iApp, double tempK, double dOcvDT) =>
        iApp * tempK * dOcvDT;

    public static double IrreversibleHeat(double iApp, double voltage, double ocv) =>
        iApp * (ocv - voltage);

    public static double HeatFlux(double tempK, double h, double area, double tempAmbK) =>
        h * area * (tempK - tempAmbK);

    public static double StepTemperatureRk4(
        double tPrev,
        double dt,
        double tempPrevK,
        double voltage,
        double iApp,
        double rho,
        double vol,
        double cp,
        double ocv,
        double dOcvDT,
        double h,
        double area,
        double tempAmbK)
    {
        double thermalMass = rho * vol * cp;
        if (thermalMass <= 0.0) return tempPrevK;

        double mainCoeff = 1.0 / thermalMass;
        double HeatBalance(double tempK, double _) =>
            mainCoeff * (
                ReversibleHeat(iApp, tempK, dOcvDT) +
                IrreversibleHeat(iApp, voltage, ocv) -
                HeatFlux(tempK, h, area, tempAmbK));

        int subSteps = Math.Max(1, (int)Math.Ceiling(dt / 0.5));
        double subDt = dt / subSteps;
        double t = tPrev;
        double temp = tempPrevK;
        for (int i = 0; i < subSteps; i++)
        {
            double next = OdeSolvers.Rk4(HeatBalance, t, temp, subDt);
            if (double.IsNaN(next) || double.IsInfinity(next))
                break;
            temp = Math.Clamp(next, 200.0, 500.0);
            t += subDt;
        }
        return temp;
    }
}
