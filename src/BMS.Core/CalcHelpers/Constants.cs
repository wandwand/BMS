namespace BMS.Core.CalcHelpers;

/// <summary>
/// Physical constants used in electrochemical, thermal, and ECM simulations (ported from bmslogic).
/// </summary>
public static class Constants
{
    /// <summary>Faraday constant [C/mol].</summary>
    public const double F = 96487.0;

    /// <summary>Universal gas constant [J/(mol·K)].</summary>
    public const double R = 8.3145;

    /// <summary>Absolute zero offset [K] (0 °C = 273.15 K).</summary>
    public const double TAbs = 273.15;

    /// <summary>Standard gravitational acceleration [m/s²].</summary>
    public const double G = 9.81;
}
