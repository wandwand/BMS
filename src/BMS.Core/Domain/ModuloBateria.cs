using System.Text;

namespace BMS.Core.Domain;

/// <summary>
/// Battery module composed of cells, supporting telemetry and passive balancing (threshold &gt; 0.05 V).
/// </summary>
public class ModuloBateria : IMonitoravel, IBalanceavel
{
    public const double LimiarDesbalanceamentoVolts = 0.05;

    private readonly List<CelulaBateria> _celulas = new();

    public string Id { get; }
    public string Nome { get; }
    public IReadOnlyList<CelulaBateria> Celulas => _celulas;

    public ModuloBateria(string id, string nome)
    {
        Id = id;
        Nome = nome;
    }

    public void AdicionarCelula(CelulaBateria celula)
    {
        ArgumentNullException.ThrowIfNull(celula);
        _celulas.Add(celula);
    }

    public double CalcularTensaoTotal() => _celulas.Sum(c => c.TensaoAtual);

    public double CalcularTensaoMedia() => _celulas.Count == 0 ? 0.0 : _celulas.Average(c => c.TensaoAtual);

    public double CalcularDeltaTensao()
    {
        if (_celulas.Count < 2) return 0.0;
        return _celulas.Max(c => c.TensaoAtual) - _celulas.Min(c => c.TensaoAtual);
    }

    public bool PrecisaBalanceamento() => CalcularDeltaTensao() > LimiarDesbalanceamentoVolts;

    public void ExecutarBalanceamento()
    {
        if (!PrecisaBalanceamento()) return;
        double media = CalcularTensaoMedia();
        foreach (var celula in _celulas)
        {
            if (celula.TensaoAtual > media)
            {
                double excesso = (celula.TensaoAtual - media) * 0.6;
                celula.DissiparTensao(excesso);
            }
        }
    }

    public bool VerificarSeguranca() => _celulas.All(c => c.VerificarSeguranca());

    public string GerarRelatorioTelemetria()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Módulo {Id} ({Nome}) | V_total={CalcularTensaoTotal():F3}V | Delta={CalcularDeltaTensao():F3}V | " +
                      $"Balanceamento={(PrecisaBalanceamento() ? "NECESSÁRIO" : "OK")}");
        foreach (var celula in _celulas)
            sb.AppendLine($"  - {celula.GerarRelatorioTelemetria()}");
        return sb.ToString().TrimEnd();
    }
}
