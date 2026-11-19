using System.Globalization;

namespace BMS.Core.Parameters;

/// <summary>
/// ECM + Thermal parameter set loaded from bmslogic-compatible CSV files or built-in chemistry presets.
/// </summary>
public class EcmParameterSet
{
    public string Name { get; init; } = "ecm_test";

    // Equivalent Circuit Model parameters
    public double R0Ref { get; init; } = 0.005;      // [ohm]
    public double R1Ref { get; init; } = 0.001;      // [ohm]
    public double C1 { get; init; } = 0.03;          // [F]
    public double TempRefK { get; init; } = 298.15;  // [K]
    public double EaR0 { get; init; } = 400.0;       // [J/mol]
    public double EaR1 { get; init; } = 400.0;       // [J/mol]

    // Thermal & geometry parameters
    public double Rho { get; init; } = 1626.0;       // [kg/m3]
    public double Vol { get; init; } = 3.38e-5;      // [m3]
    public double Cp { get; init; } = 750.0;         // [J/(K·kg)]
    public double H { get; init; } = 1.0;            // [W/(m2·K)] / [J/(s·K·m2)]
    public double Area { get; init; } = 0.085;       // [m2]

    // Cell capacity & voltage limits
    public double CapacityAh { get; init; } = 1.65;  // [Ah]
    public double VMax { get; init; } = 4.2;         // [V]
    public double VMin { get; init; } = 2.5;         // [V]

    // ESC hysteresis parameters
    public double M0 { get; init; } = 0.0;           // [V]
    public double M { get; init; } = 0.0;            // [V]
    public double Gamma { get; init; } = 0.0;        // [-]

    // Electrochemical / OCV / Entropic functions
    public Func<double, double, double> FuncEta { get; init; } = (_, _) => 1.0;
    public Func<double, double> FuncOcv { get; init; } = DefaultPolyOcv;
    public Func<double, double> FuncDOcvDTemp { get; init; } = DefaultPolyDOcvDTemp;

    /// <summary>
    /// Exact OCV polynomial from bmslogic/parameter_sets/parameter_sets_ecm/test/funcs.py.
    /// </summary>
    public static double DefaultPolyOcv(double soc)
    {
        double z = Math.Clamp(soc, 0.0, 1.0);
        double[] coeffs =
        [
            7.83002260e+03, -4.72721395e+04, 1.25237092e+05, -1.91403553e+05,
            1.86656077e+05, -1.21342077e+05, 5.33553293e+04, -1.57665852e+04,
            3.04748964e+03, -3.65636105e+02, 2.50281588e+01, 2.93438431e+00
        ];
        return EvalPoly(coeffs, z);
    }

    /// <summary>
    /// Exact dOCV/dT polynomial from bmslogic/parameter_sets/parameter_sets_ecm/test/funcs.py.
    /// </summary>
    public static double DefaultPolyDOcvDTemp(double soc)
    {
        double z = Math.Clamp(soc, 0.0, 1.0);
        double[] coeffs =
        [
            -1.62911207e+01, 9.66871309e+01, -2.50145828e+02, 3.70280039e+02,
            -3.46021781e+02, 2.12461660e+02, -8.64646004e+01, 2.29561374e+01,
            -3.80515739e+00, 3.58298325e-01, -1.40044753e-02, -6.86713653e-04
        ];
        return EvalPoly(coeffs, z);
    }

    /// <summary>
    /// Realistic LiFePO4 OCV curve (2.50 V - 3.65 V) with characteristic flat plateau around 3.28-3.33 V.
    /// </summary>
    public static double LiFePO4Ocv(double soc)
    {
        double z = Math.Clamp(soc, 0.0, 1.0);
        return 3.25 + 0.14 * z - 0.55 * Math.Exp(-18.0 * z) + 0.26 * Math.Exp(-25.0 * (1.0 - z));
    }

    /// <summary>
    /// Realistic Tesla NCA 2170 OCV curve (2.50 V - 4.20 V).
    /// </summary>
    public static double Nca2170Ocv(double soc)
    {
        double z = Math.Clamp(soc, 0.0, 1.0);
        return 3.05 + 1.12 * z - 0.45 * Math.Exp(-14.0 * z) + 0.08 * Math.Sin(Math.PI * z);
    }

    private static double EvalPoly(double[] coeffs, double x)
    {
        double result = 0.0;
        foreach (double c in coeffs)
            result = result * x + c;
        return result;
    }

    public static EcmParameterSet LoadFromCsv(
        string csvPath,
        string? name = null,
        Func<double, double>? funcOcv = null,
        Func<double, double>? funcDOcvDTemp = null)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"Parameter CSV file not found: {csvPath}", csvPath);

        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadLines(csvPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            var parts = rawLine.Split(',');
            if (parts.Length < 2) continue;

            string key = parts[0].Trim();
            string valStr = parts[1].Trim();
            if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                values[key] = val;
        }

        double Get(string[] keys, double fallback)
        {
            foreach (var k in keys)
                if (values.TryGetValue(k, out double v)) return v;
            return fallback;
        }

        return new EcmParameterSet
        {
            Name = name ?? Path.GetFileName(Path.GetDirectoryName(csvPath)) ?? "custom",
            R0Ref = Get(["R0 ref [ohm]", "R0_ref [ohm]"], 0.005),
            R1Ref = Get(["R1_ref [ohm]", "R1 ref [ohm]"], 0.001),
            C1 = Get(["C1 [F]"], 0.03),
            TempRefK = Get(["temp_ref [K]"], 298.15),
            EaR0 = Get(["Ea_R0 [J/mol]"], 400.0),
            EaR1 = Get(["Ea_R1 [J/mol]"], 400.0),
            Rho = Get(["rho [kg/m3]", "Density [kg m^-3]"], 1626.0),
            Vol = Get(["vol [m3]", "Volume [m^3]"], 3.38e-5),
            Cp = Get(["C_p [J/(Kkg)]", "Specific Heat [J K^-1 kg^-1]"], 750.0),
            H = Get(["h [J/(SK)]", "Heat Transfer Coefficient [J s^-1 K^-1]"], 1.0),
            Area = Get(["area [m2]", "Surface Area [m^2]"], 0.085),
            CapacityAh = Get(["cap [Ahr]", "Capacity [A hr]"], 1.65),
            VMax = Get(["V_max [V]", "Maximum Potential Cut-off [V]"], 4.2),
            VMin = Get(["V_min [V]", "Minimum Potential Cut-off [V]"], 2.5),
            M0 = Get(["M_0 [V]"], 0.0),
            M = Get(["M [V]"], 0.0),
            Gamma = Get(["gamma"], 0.0),
            FuncOcv = funcOcv ?? DefaultPolyOcv,
            FuncDOcvDTemp = funcDOcvDTemp ?? DefaultPolyDOcvDTemp
        };
    }

    public static EcmParameterSet CreateEcmTest()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "parameter_sets", "ecm_test", "param.csv");
        return File.Exists(path)
            ? LoadFromCsv(path, "ecm_test", DefaultPolyOcv, DefaultPolyDOcvDTemp)
            : new EcmParameterSet { Name = "ecm_test" };
    }

    public static EcmParameterSet CreateLiFePO4()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "parameter_sets", "lifepo4_default", "param.csv");
        return File.Exists(path)
            ? LoadFromCsv(path, "lifepo4_default", LiFePO4Ocv, _ => -0.0002)
            : new EcmParameterSet
            {
                Name = "lifepo4_default",
                R0Ref = 0.008,
                R1Ref = 0.003,
                C1 = 1200.0,
                CapacityAh = 3.2,
                VMax = 3.65,
                VMin = 2.50,
                M0 = 0.015,
                M = 0.025,
                Gamma = 2.0,
                FuncOcv = LiFePO4Ocv,
                FuncDOcvDTemp = _ => -0.0002
            };
    }

    public static EcmParameterSet CreateTeslaNca2170()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "parameter_sets", "tesla_NCA_2170", "param_battery-cell.csv");
        if (File.Exists(path))
        {
            var loaded = LoadFromCsv(path, "tesla_NCA_2170", Nca2170Ocv, _ => -0.0004);
            // bmslogic's tesla_NCA_2170 CSV lists Specific Heat as 0.32 (J/g·K scale); normalize to J/(kg·K) if < 10
            if (loaded.Cp < 10.0)
            {
                return new EcmParameterSet
                {
                    Name = loaded.Name,
                    R0Ref = loaded.R0Ref,
                    R1Ref = loaded.R1Ref,
                    C1 = loaded.C1,
                    TempRefK = loaded.TempRefK,
                    EaR0 = loaded.EaR0,
                    EaR1 = loaded.EaR1,
                    Rho = loaded.Rho,
                    Vol = loaded.Vol,
                    Cp = loaded.Cp * 1000.0,
                    H = loaded.H,
                    Area = loaded.Area,
                    CapacityAh = loaded.CapacityAh,
                    VMax = loaded.VMax,
                    VMin = loaded.VMin,
                    M0 = loaded.M0,
                    M = loaded.M,
                    Gamma = loaded.Gamma,
                    FuncEta = loaded.FuncEta,
                    FuncOcv = loaded.FuncOcv,
                    FuncDOcvDTemp = loaded.FuncDOcvDTemp
                };
            }
            return loaded;
        }

        return new EcmParameterSet
        {
            Name = "tesla_NCA_2170",
            R0Ref = 0.012,
            R1Ref = 0.005,
            C1 = 800.0,
            Rho = 2680.938465,
            Vol = 2.42e-5,
            Cp = 320.0,
            H = 11.0,
            Area = 0.005310862,
            CapacityAh = 6.5,
            VMax = 4.2,
            VMin = 2.5,
            FuncOcv = Nca2170Ocv,
            FuncDOcvDTemp = _ => -0.0004
        };
    }
}
