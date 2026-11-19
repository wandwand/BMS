using BMS.Core.Parameters;

namespace BMS.Core.Domain;

/// <summary>
/// Li-Ion battery cell (3.00 V - 4.20 V, max 45 °C) with Turbo Charging (2C up to 80% SoC or 40 °C).
/// </summary>
public class CelulaLiIon : CelulaBateria, ICarregamentoTurbo
{
    public bool ModoTurboAtivo { get; private set; }
    public string? UltimoMotivoDesativacaoTurbo { get; private set; }

    public CelulaLiIon(
        string id,
        string nome,
        double socInicial = 0.50,
        double temperaturaInicialC = 25.0)
        : base(
            id: id,
            nome: nome,
            quimica: "Li-Ion",
            parametros: EcmParameterSet.CreateEcmTest(),
            tensaoMinima: 3.00,
            tensaoMaxima: 4.20,
            temperaturaMaximaCelsius: 45.0,
            socInicial: socInicial,
            temperaturaInicialC: temperaturaInicialC)
    {
    }

    public void AtivarModoTurbo()
    {
        if (Soc >= 0.80)
        {
            DesativarModoTurbo("SoC >= 80% (transição para CC-CV)");
            return;
        }
        if (TemperaturaCelsius >= 40.0)
        {
            DesativarModoTurbo("Temperatura >= 40 °C (proteção térmica)");
            return;
        }

        UltimoMotivoDesativacaoTurbo = null;
        ModoTurboAtivo = true;
    }

    public void DesativarModoTurbo(string motivo)
    {
        ModoTurboAtivo = false;
        UltimoMotivoDesativacaoTurbo = motivo;
    }

    public override void SimularCarga(double dt = 60.0)
    {
        double fator = ModoTurboAtivo ? 2.0 : 1.0;
        double correnteCarga = -Parametros.CapacityAh * fator;

        int passos = Math.Max(1, (int)Math.Ceiling(dt));
        double stepDt = dt / passos;
        for (int i = 0; i < passos; i++)
        {
            if (ModoTurboAtivo && (Soc >= 0.80 || TemperaturaCelsius >= 40.0))
            {
                string motivo = Soc >= 0.80
                    ? "SoC atingiu 80% durante carga turbo"
                    : $"Temperatura atingiu {TemperaturaCelsius:F1} °C durante carga turbo";
                DesativarModoTurbo(motivo);
                correnteCarga = -Parametros.CapacityAh;
            }
            SimularPasso(correnteCarga, stepDt);
        }
    }
}
