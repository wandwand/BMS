using System.Text;
using BMS.Core.Simulation;

namespace BMS.Core.Domain;

/// <summary>
/// Central BMS Controller: manages battery modules, temperature sensors, diagnostics,
/// passive balancing, fault injection, alerts, and bmslogic cycler simulations.
/// </summary>
public class ControladorBMS : IMonitoravel
{
    private readonly List<ModuloBateria> _modulos = new();
    private readonly List<SensorTemperatura> _sensores = new();
    private readonly List<AlertaSistema> _historicoAlertas = new();

    public string NomeSistema { get; }
    public IReadOnlyList<ModuloBateria> Modulos => _modulos;
    public IReadOnlyList<SensorTemperatura> Sensores => _sensores;
    public IReadOnlyList<AlertaSistema> HistoricoAlertas => _historicoAlertas;

    public ControladorBMS(string nomeSistema = "BMS UFC + BMSLogic C#")
    {
        NomeSistema = nomeSistema;
    }

    public void AdicionarModulo(ModuloBateria modulo) => _modulos.Add(modulo);
    public void AdicionarSensor(SensorTemperatura sensor) => _sensores.Add(sensor);

    public IEnumerable<CelulaBateria> TodasCelulas => _modulos.SelectMany(m => m.Celulas);

    public CelulaBateria? BuscarCelulaPorId(string id) =>
        TodasCelulas.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void RegistrarAlerta(NivelAlerta nivel, string origemId, string mensagem) =>
        _historicoAlertas.Add(new AlertaSistema(nivel, origemId, mensagem));

    public void SimularCicloCarga(double dt = 60.0)
    {
        foreach (var celula in TodasCelulas)
        {
            bool turboAntes = celula is CelulaLiIon li && li.ModoTurboAtivo;
            celula.SimularCarga(dt);

            if (celula is CelulaLiIon liDepois && turboAntes && !liDepois.ModoTurboAtivo)
            {
                RegistrarAlerta(NivelAlerta.Aviso, liDepois.Id,
                    $"Modo Turbo desativado: {liDepois.UltimoMotivoDesativacaoTurbo ?? "proteção"}");
            }
        }
        RealizarVarreduraSeguranca();
    }

    public void SimularCicloDescarga(double dt = 60.0)
    {
        foreach (var celula in TodasCelulas)
            celula.SimularDescarga(dt);
        RealizarVarreduraSeguranca();
    }

    public EcmSimulationResult ExecutarCiclador(string celulaId, BaseCycler cycler, double dt = 1.0, bool isothermal = false)
    {
        var celula = BuscarCelulaPorId(celulaId)
            ?? throw new KeyNotFoundException($"Célula '{celulaId}' não encontrada.");
        var solver = new EscDtSolver(isothermal);
        var res = solver.Solve(celula, cycler, dt);
        RealizarVarreduraSeguranca();
        return res;
    }

    public int ExecutarBalanceamentoPassivo()
    {
        int balanceados = 0;
        foreach (var modulo in _modulos)
        {
            if (modulo.PrecisaBalanceamento())
            {
                double deltaAntes = modulo.CalcularDeltaTensao();
                modulo.ExecutarBalanceamento();
                double deltaDepois = modulo.CalcularDeltaTensao();
                balanceados++;
                RegistrarAlerta(NivelAlerta.Info, modulo.Id,
                    $"Balanceamento passivo executado (ΔV: {deltaAntes:F3}V -> {deltaDepois:F3}V).");
            }
        }
        return balanceados;
    }

    public void InjetarFalhaSimulada()
    {
        var lfp = TodasCelulas.OfType<CelulaLiFePO4>().FirstOrDefault();
        if (lfp is not null)
        {
            lfp.InjetarFalhaSobretensao(3.95);
            RegistrarAlerta(NivelAlerta.Critico, lfp.Id, "Falha injetada: Sobretensão simulada (3.950 V).");
        }

        var sensor = _sensores.FirstOrDefault();
        if (sensor is not null)
        {
            sensor.AtualizarLeitura(68.5);
            RegistrarAlerta(NivelAlerta.Critico, sensor.Id, "Falha injetada: Superaquecimento simulado (68.5 °C).");
        }

        RealizarVarreduraSeguranca();
    }

    public void AtivarModoTurboEmLiIon()
    {
        foreach (var li in TodasCelulas.OfType<CelulaLiIon>())
        {
            li.AtivarModoTurbo();
            if (li.ModoTurboAtivo)
                RegistrarAlerta(NivelAlerta.Info, li.Id, "Modo Turbo (2C) ativado.");
            else
                RegistrarAlerta(NivelAlerta.Aviso, li.Id, $"Modo Turbo recusado: {li.UltimoMotivoDesativacaoTurbo}");
        }
    }

    public void AtivarModoFlutuacaoEmLiFePO4()
    {
        foreach (var lfp in TodasCelulas.OfType<CelulaLiFePO4>())
        {
            lfp.EntrarModoFlutuacao();
            RegistrarAlerta(NivelAlerta.Info, lfp.Id, "Modo Flutuação (manutenção 3.45 V) ativado.");
        }
    }

    public List<string> RealizarVarreduraSeguranca()
    {
        var diagnosticos = new List<string>();
        foreach (var celula in TodasCelulas)
        {
            string diag = celula.RealizarAutodiagnostico();
            diagnosticos.Add(diag);
            if (!celula.VerificarSeguranca())
                RegistrarAlerta(NivelAlerta.Critico, celula.Id, diag);
        }

        foreach (var sensor in _sensores)
        {
            string diag = sensor.RealizarAutodiagnostico();
            diagnosticos.Add(diag);
            if (!sensor.VerificarSeguranca())
                RegistrarAlerta(NivelAlerta.Critico, sensor.Id, diag);
        }

        return diagnosticos;
    }

    public bool VerificarSeguranca() =>
        _modulos.All(m => m.VerificarSeguranca()) && _sensores.All(s => s.VerificarSeguranca());

    public string GerarRelatorioTelemetria()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {NomeSistema} ===");
        sb.AppendLine($"Status Geral: {(VerificarSeguranca() ? "SEGURO" : "ALERTA CRÍTICO")} | Alertas Globais: {AlertaSistema.TotalAlertasGerados}");
        foreach (var modulo in _modulos)
            sb.AppendLine(modulo.GerarRelatorioTelemetria());
        foreach (var sensor in _sensores)
            sb.AppendLine(sensor.GerarRelatorioTelemetria());
        return sb.ToString().TrimEnd();
    }

    public static ControladorBMS CriarConfiguracaoPadrao()
    {
        var bms = new ControladorBMS();

        var modLfp = new ModuloBateria("MOD-LFP", "Módulo LiFePO4");
        modLfp.AdicionarCelula(new CelulaLiFePO4("CELL-LFP-01", "Célula LiFePO4 #1", socInicial: 0.72));
        modLfp.AdicionarCelula(new CelulaLiFePO4("CELL-LFP-02", "Célula LiFePO4 #2", socInicial: 0.58));

        var modLi = new ModuloBateria("MOD-LI", "Módulo Li-Ion / NCA");
        modLi.AdicionarCelula(new CelulaLiIon("CELL-LI-01", "Célula Li-Ion #1", socInicial: 0.55));
        modLi.AdicionarCelula(new CelulaLiIon("CELL-LI-02", "Célula Li-Ion #2", socInicial: 0.62));
        modLi.AdicionarCelula(new CelulaNca2170("CELL-NCA-01", "Tesla NCA 2170 #1", socInicial: 0.68));

        bms.AdicionarModulo(modLfp);
        bms.AdicionarModulo(modLi);

        bms.AdicionarSensor(new SensorTemperatura("TEMP-01", "Barramento Principal", "Pack Frontal", 26.5));
        bms.AdicionarSensor(new SensorTemperatura("TEMP-02", "Barramento Secundário", "Pack Traseiro", 27.0));

        return bms;
    }
}
