using BMS.Core.CalcHelpers;

namespace BMS.Core.Domain;

/// <summary>
/// Physical busbar/pack temperature sensor (safe limit &lt;= 60 °C).
/// </summary>
public class SensorTemperatura : ComponenteEletrico, IMonitoravel
{
    public const double LimiteSeguroCelsius = 60.0;

    public string Localizacao { get; }
    public double TemperaturaCelsius { get; private set; }
    public double TemperaturaKelvin => ConversorUnidades.CelsiusParaKelvin(TemperaturaCelsius);

    public SensorTemperatura(string id, string nome, string localizacao, double temperaturaInicialC = 25.0)
        : base(id, nome)
    {
        Localizacao = localizacao;
        TemperaturaCelsius = temperaturaInicialC;
    }

    public void AtualizarLeitura(double novaTemperaturaC) => TemperaturaCelsius = novaTemperaturaC;

    public bool VerificarSeguranca() => TemperaturaCelsius <= LimiteSeguroCelsius;

    public override string RealizarAutodiagnostico() =>
        VerificarSeguranca()
            ? $"Sensor {Id} ({Localizacao}): OK ({TemperaturaCelsius:F1} °C)"
            : $"Sensor {Id} ({Localizacao}): SUPERAQUECIMENTO ({TemperaturaCelsius:F1} °C > {LimiteSeguroCelsius:F1} °C)";

    public string GerarRelatorioTelemetria() =>
        $"Sensor {Id} [{Localizacao}] | Temp: {TemperaturaCelsius:F2} °C ({TemperaturaKelvin:F2} K) | {(VerificarSeguranca() ? "SEGURO" : "ALERTA")}";
}
