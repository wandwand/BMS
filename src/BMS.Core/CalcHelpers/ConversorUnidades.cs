namespace BMS.Core.CalcHelpers;

/// <summary>
/// Utility methods for physical unit conversions in the BMS.
/// </summary>
public static class ConversorUnidades
{
    public static double CelsiusParaKelvin(double celsius) => celsius + Constants.TAbs;

    public static double KelvinParaCelsius(double kelvin) => kelvin - Constants.TAbs;

    public static double MilivoltsParaVolts(double mv) => mv / 1000.0;

    public static double VoltsParaMilivolts(double volts) => volts * 1000.0;

    public static double AhParaCoulombs(double ah) => ah * 3600.0;
}
