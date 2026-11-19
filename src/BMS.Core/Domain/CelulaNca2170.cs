using BMS.Core.Parameters;

namespace BMS.Core.Domain;

/// <summary>
/// Tesla NCA 2170 battery cell (2.50 V - 4.20 V, 6.5 Ah), using bmslogic's tesla_NCA_2170 parameter set.
/// </summary>
public class CelulaNca2170 : CelulaBateria
{
    public CelulaNca2170(
        string id,
        string nome,
        double socInicial = 0.65,
        double temperaturaInicialC = 25.0)
        : base(
            id: id,
            nome: nome,
            quimica: "NCA-2170",
            parametros: EcmParameterSet.CreateTeslaNca2170(),
            tensaoMinima: 2.50,
            tensaoMaxima: 4.20,
            temperaturaMaximaCelsius: 50.0,
            socInicial: socInicial,
            temperaturaInicialC: temperaturaInicialC)
    {
    }

    public override void SimularCarga(double dt = 60.0)
    {
        double correnteCarga = -Parametros.CapacityAh;
        int passos = Math.Max(1, (int)Math.Ceiling(dt));
        double stepDt = dt / passos;
        for (int i = 0; i < passos; i++)
            SimularPasso(correnteCarga, stepDt);
    }
}
