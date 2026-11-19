namespace BMS.Core.CalcHelpers;

/// <summary>
/// Numerical ODE solvers (ported from bmslogic/calc_helpers/ode_solvers.py).
/// </summary>
public static class OdeSolvers
{
    /// <summary>
    /// Fourth-order Runge-Kutta (RK4) integration step for dy/dt = f(y, t).
    /// </summary>
    public static double Rk4(Func<double, double, double> func, double tPrev, double yPrev, double stepSize)
    {
        double k1 = stepSize * func(yPrev, tPrev);
        double k2 = stepSize * func(yPrev + 0.5 * k1, tPrev + 0.5 * stepSize);
        double k3 = stepSize * func(yPrev + 0.5 * k2, tPrev + 0.5 * stepSize);
        double k4 = stepSize * func(yPrev + k3, tPrev + stepSize);
        return yPrev + (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0;
    }
}
