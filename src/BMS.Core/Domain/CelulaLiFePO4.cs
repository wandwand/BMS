using BMS.Core.Parameters;

namespace BMS.Core.Domain;

/// <summary>
/// LiFePO4 battery cell (2.50 V - 3.65 V, max 55 °C) with Float Maintenance Mode.
/// </summary>
public class CelulaLiFePO4 : CelulaBateria, IModoFlutuacao
{
    public bool ModoFlutuacaoAtivo { get; private set; }

    public CelulaLiFePO4(
        string id,
        string nome,
        double socInicial = 0.70,
        double temperaturaInicialC = 25.0)
        : base(
            id: id,
            nome: nome,
            quimica: "LiFePO4",
            parametros: EcmParameterSet.CreateLiFePO4(),
            tensaoMinima: 2.50,
            tensaoMaxima: 3.65,
            temperaturaMaximaCelsius: 55.0,
            socInicial: socInicial,
            temperaturaInicialC: temperaturaInicialC)
    {
    }

    public void EntrarModoFlutuacao()
    {
        ModoFlutuacaoAtivo = true;
        CorrenteAtual = -0.5;
        TensaoAtual = Math.Min(TensaoAtual, 3.45);
    }

    public void SairModoFlutuacao() => ModoFlutuacaoAtivo = false;

    public override void SimularCarga(double dt = 60.0)
    {
        if (Soc >= 0.95 || ModoFlutuacaoAtivo)
        {
            EntrarModoFlutuacao();
            // Gentle trickle charge (negative current in ESC convention)
            int passosFloat = Math.Max(1, (int)Math.Ceiling(dt / 2.0));
            double stepDt = dt / passosFloat;
            for (int i = 0; i < passosFloat; i++)
                SimularPasso(-0.5, stepDt);
            TensaoAtual = Math.Min(TensaoAtual, 3.45);
            return;
        }

        // Standard 1C charge (negative current in ESC convention)
        double correnteCarga = -Parametros.CapacityAh;
        int passos = Math.Max(1, (int)Math.Ceiling(dt));
        double dtStep = dt / passos;
        for (int i = 0; i < passos; i++)
        {
            SimularPasso(correnteCarga, dtStep);
            if (Soc >= 0.95)
            {
                EntrarModoFlutuacao();
                break;
            }
        }
    }
}
