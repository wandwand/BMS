using BMS.Core.CalcHelpers;
using BMS.Core.Models;
using BMS.Core.Parameters;

namespace BMS.Core.Domain;

/// <summary>
/// Abstract battery cell backed by bmslogic's ESC (1-RC + Hysteresis) ECM model,
/// Lumped Thermal RK4 model, and Sigma-Point Kalman Filter (CDKF) for SoC estimation.
/// </summary>
public abstract class CelulaBateria : ComponenteEletrico, IMonitoravel
{
    public EcmParameterSet Parametros { get; }
    public string Quimica { get; }

    public double TensaoMinima { get; }
    public double TensaoMaxima { get; }
    public double TemperaturaMaximaCelsius { get; }

    public double Soc { get; protected set; }
    public double SocPercentual => Math.Clamp(Soc * 100.0, 0.0, 100.0);
    public double SocEstimadoKalman { get; protected set; }
    public double SocKalmanPercentual => Math.Clamp(SocEstimadoKalman * 100.0, 0.0, 100.0);

    public double TensaoAtual { get; protected set; }
    public double CorrenteAtual { get; protected set; }
    public double TemperaturaKelvin { get; protected set; }
    public double TemperaturaCelsius => ConversorUnidades.KelvinParaCelsius(TemperaturaKelvin);
    public double TemperaturaAmbienteKelvin { get; }

    public double IR1 { get; protected set; }
    public double HistereseH { get; protected set; }
    public double HistereseS { get; protected set; }

    private double _tempoSimuladoS;
    private double _ultimoDt = 1.0;
    private readonly SigmaPointKalmanFilter _kalmanFilter;

    /// <summary>Arrhenius temperature-dependent ohmic resistance R0 [ohm].</summary>
    public double R0 =>
        Parametros.R0Ref * Math.Exp(-Parametros.EaR0 / Constants.R * (1.0 / TemperaturaKelvin - 1.0 / Parametros.TempRefK));

    /// <summary>Arrhenius temperature-dependent polarization resistance R1 [ohm].</summary>
    public double R1 =>
        Parametros.R1Ref * Math.Exp(-Parametros.EaR1 / Constants.R * (1.0 / TemperaturaKelvin - 1.0 / Parametros.TempRefK));

    public double DOcvDTemp => Parametros.FuncDOcvDTemp(Soc);
    public double Ocv => Parametros.FuncOcv(Soc) + DOcvDTemp * (TemperaturaKelvin - Parametros.TempRefK);
    public double Eta => Parametros.FuncEta(Soc, TemperaturaKelvin);

    protected CelulaBateria(
        string id,
        string nome,
        string quimica,
        EcmParameterSet parametros,
        double tensaoMinima,
        double tensaoMaxima,
        double temperaturaMaximaCelsius,
        double socInicial = 0.70,
        double temperaturaInicialC = 25.0)
        : base(id, nome)
    {
        Quimica = quimica;
        Parametros = parametros ?? throw new ArgumentNullException(nameof(parametros));
        TensaoMinima = tensaoMinima;
        TensaoMaxima = tensaoMaxima;
        TemperaturaMaximaCelsius = temperaturaMaximaCelsius;

        Soc = Math.Clamp(socInicial, 0.0, 1.0);
        // Intentionally initialize Kalman slightly offset (+5%) so convergence can be observed
        double socKalmanInit = Math.Clamp(Soc + 0.05, 0.0, 1.0);
        SocEstimadoKalman = socKalmanInit;

        TemperaturaKelvin = ConversorUnidades.CelsiusParaKelvin(temperaturaInicialC);
        TemperaturaAmbienteKelvin = TemperaturaKelvin;
        TensaoAtual = Ocv;
        CorrenteAtual = 0.0;

        _kalmanFilter = CriarFiltroKalman(socKalmanInit);
    }

    private SigmaPointKalmanFilter CriarFiltroKalman(double socInicial)
    {
        var x = NormalRandomVector.FromScalar(socInicial, 0.01);
        var w = NormalRandomVector.FromScalar(0.0, 1e-5);
        var v = NormalRandomVector.FromScalar(0.0, 5e-4);

        return new SigmaPointKalmanFilter(
            x: x,
            w: w,
            v: v,
            yDim: 1,
            stateEquation: (spX, u, spW) =>
            {
                int cols = spX.GetLength(1);
                var next = new double[1, cols];
                double eta = Eta;
                for (int j = 0; j < cols; j++)
                {
                    double zNext = EscModel.SocNext(_ultimoDt, u, spX[0, j], Parametros.CapacityAh, eta) + spW[0, j];
                    next[0, j] = Math.Clamp(zNext, 0.0, 1.0);
                }
                return next;
            },
            outputEquation: (spX, u, spV) =>
            {
                int cols = spX.GetLength(1);
                var y = new double[1, cols];
                double r0 = R0;
                double r1 = R1;
                double dOcvDT = DOcvDTemp;
                double dT = TemperaturaKelvin - Parametros.TempRefK;
                for (int j = 0; j < cols; j++)
                {
                    double z = Math.Clamp(spX[0, j], 0.0, 1.0);
                    double ocvEst = Parametros.FuncOcv(z) + dOcvDT * dT;
                    y[0, j] = EscModel.Voltage(u, ocvEst, r0, r1, IR1, Parametros.M0, Parametros.M, HistereseH, HistereseS) + spV[0, j];
                }
                return y;
            });
    }

    /// <summary>
    /// Advances the cell physics by dt seconds under applied current iApp (positive = discharge, negative = charge).
    /// </summary>
    public virtual void SimularPasso(double iApp, double dt = 1.0, bool isothermal = false)
    {
        if (dt <= 0.0) throw new ArgumentOutOfRangeException(nameof(dt));
        if (TensaoAtual <= 0.0 && iApp > 0.0)
        {
            CorrenteAtual = 0.0;
            return;
        }

        _ultimoDt = dt;
        CorrenteAtual = iApp;

        double eta = Eta;
        Soc = Math.Clamp(EscModel.SocNext(dt, iApp, Soc, Parametros.CapacityAh, eta), 0.0, 1.0);

        double iR1Prev = IR1;
        IR1 = EscModel.IR1Next(dt, iApp, IR1, R1, Parametros.C1);

        double hPrev = HistereseH;
        HistereseH = EscModel.HysteresisNext(dt, iApp, eta, Parametros.Gamma, Parametros.CapacityAh, HistereseH);

        double sPrev = HistereseS;
        HistereseS = EscModel.InstantaneousHysteresisSign(iApp, HistereseS);

        TensaoAtual = Math.Max(0.0, EscModel.Voltage(iApp, Ocv, R0, R1, iR1Prev, Parametros.M0, Parametros.M, hPrev, sPrev));

        if (!isothermal)
        {
            TemperaturaKelvin = EcmLumpedThermalModel.StepTemperatureRk4(
                tPrev: _tempoSimuladoS,
                dt: dt,
                tempPrevK: TemperaturaKelvin,
                voltage: TensaoAtual,
                iApp: iApp,
                rho: Parametros.Rho,
                vol: Parametros.Vol,
                cp: Parametros.Cp,
                ocv: Ocv,
                dOcvDT: DOcvDTemp,
                h: Parametros.H,
                area: Parametros.Area,
                tempAmbK: TemperaturaAmbienteKelvin);
        }

        _tempoSimuladoS += dt;

        // Update Kalman filter SoC estimate
        _kalmanFilter.Solve(iApp, TensaoAtual);
        SocEstimadoKalman = Math.Clamp(_kalmanFilter.State[0, 0], 0.0, 1.0);
    }

    public abstract void SimularCarga(double dt = 60.0);

    public virtual void SimularDescarga(double dt = 60.0)
    {
        if (TensaoAtual <= 0.0)
        {
            CorrenteAtual = 0.0;
            return;
        }

        // 1C discharge current (positive by ESC convention)
        double correnteDescarga = Parametros.CapacityAh;
        int passos = Math.Max(1, (int)Math.Ceiling(dt));
        double stepDt = dt / passos;
        for (int i = 0; i < passos; i++)
            SimularPasso(correnteDescarga, stepDt);
    }

    public void DissiparTensao(double deltaV)
    {
        if (deltaV <= 0.0) return;
        TensaoAtual = Math.Max(TensaoMinima, TensaoAtual - deltaV);
        Soc = Math.Clamp(Soc - deltaV * 0.05, 0.0, 1.0);
    }

    public void InjetarFalhaSobretensao(double tensaoFault) => TensaoAtual = tensaoFault;

    public virtual bool VerificarSeguranca() =>
        TensaoAtual >= TensaoMinima &&
        TensaoAtual <= TensaoMaxima &&
        TemperaturaCelsius <= TemperaturaMaximaCelsius;

    public override string RealizarAutodiagnostico()
    {
        if (TensaoAtual > TensaoMaxima)
            return $"Célula {Id} ({Quimica}): SOBRETENSÃO ({TensaoAtual:F3} V > {TensaoMaxima:F2} V)";
        if (TensaoAtual < TensaoMinima)
            return $"Célula {Id} ({Quimica}): SUBTENSÃO ({TensaoAtual:F3} V < {TensaoMinima:F2} V)";
        if (TemperaturaCelsius > TemperaturaMaximaCelsius)
            return $"Célula {Id} ({Quimica}): SUPERAQUECIMENTO ({TemperaturaCelsius:F1} °C > {TemperaturaMaximaCelsius:F1} °C)";
        return $"Célula {Id} ({Quimica}): OK";
    }

    public virtual string GerarRelatorioTelemetria() =>
        $"{Id} [{Quimica}] | V={TensaoAtual:F3}V | I={CorrenteAtual:F2}A | T={TemperaturaCelsius:F1}°C ({TemperaturaKelvin:F1}K) | " +
        $"SoC={SocPercentual:F1}% (KF={SocKalmanPercentual:F1}%) | {(VerificarSeguranca() ? "OK" : "FALHA")}";
}
