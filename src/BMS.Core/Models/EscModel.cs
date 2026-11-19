namespace BMS.Core.Models;

/// <summary>
/// First-order Thevenin Equivalent Circuit Model (ECM), ported from bmslogic PyThevenin1RC.
/// Convention: discharge current is positive (&gt; 0), charge current is negative (&lt; 0).
/// </summary>
public static class Thevenin1RCModel
{
    public static double SocNext(double dt, double iApp, double socPrev, double capacityAh, double eta) =>
        socPrev - dt * eta * iApp / (3600.0 * capacityAh);

    public static double IR1Next(double dt, double iApp, double iR1Prev, double r1, double c1)
    {
        double expTerm = Math.Exp(-dt / (r1 * c1));
        return expTerm * iR1Prev + (1.0 - expTerm) * iApp;
    }

    public static double Voltage(double iApp, double ocv, double r0, double r1, double iR1) =>
        ocv - r1 * iR1 - r0 * iApp;
}

/// <summary>
/// Enhanced Self-Correcting (ESC) ECM model with hysteresis, ported from bmslogic PyESC.
/// </summary>
public static class EscModel
{
    public static int Sign(double num) => num < 0 ? -1 : (num > 0 ? 1 : 0);

    public static double InstantaneousHysteresisSign(double iApp, double sPrev) =>
        Math.Abs(iApp) > 0.0 ? Sign(iApp) : sPrev;

    public static double SocNext(double dt, double iApp, double socPrev, double capacityAh, double eta) =>
        Thevenin1RCModel.SocNext(dt, iApp, socPrev, capacityAh, eta);

    public static double IR1Next(double dt, double iApp, double iR1Prev, double r1, double c1) =>
        Thevenin1RCModel.IR1Next(dt, iApp, iR1Prev, r1, c1);

    public static double HysteresisNext(double dt, double iApp, double eta, double gamma, double capacityAh, double hPrev)
    {
        if (gamma <= 0.0) return hPrev;
        double expTerm = Math.Exp(-Math.Abs((eta * iApp * gamma * dt) / (3600.0 * capacityAh)));
        return expTerm * hPrev - (1.0 - expTerm) * Sign(iApp);
    }

    public static double Voltage(
        double iApp,
        double ocv,
        double r0,
        double r1,
        double iR1,
        double m0,
        double m,
        double h,
        double sPrev) =>
        ocv - r1 * iR1 - r0 * iApp + m * h + m0 * InstantaneousHysteresisSign(iApp, sPrev);
}
