namespace BMS.Core.Domain;

public enum NivelAlerta
{
    Info,
    Aviso,
    Critico
}

/// <summary>
/// Represents a BMS system alert with a thread-safe global counter.
/// </summary>
public class AlertaSistema
{
    private static int _totalAlertasGerados;

    public static int TotalAlertasGerados => Volatile.Read(ref _totalAlertasGerados);

    public int NumeroSequencial { get; }
    public DateTime Timestamp { get; }
    public NivelAlerta Nivel { get; }
    public string OrigemId { get; }
    public string Mensagem { get; }

    public AlertaSistema(NivelAlerta nivel, string origemId, string mensagem)
    {
        NumeroSequencial = Interlocked.Increment(ref _totalAlertasGerados);
        Timestamp = DateTime.Now;
        Nivel = nivel;
        OrigemId = origemId;
        Mensagem = mensagem;
    }

    public static void ResetarContadorGlobal() => Interlocked.Exchange(ref _totalAlertasGerados, 0);

    public override string ToString() =>
        $"[#{NumeroSequencial:D3} | {Timestamp:HH:mm:ss} | {Nivel.ToString().ToUpperInvariant()}] ({OrigemId}) {Mensagem}";
}
