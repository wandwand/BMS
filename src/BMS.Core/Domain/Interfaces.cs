namespace BMS.Core.Domain;

public interface IMonitoravel
{
    string GerarRelatorioTelemetria();
    bool VerificarSeguranca();
}

public interface IBalanceavel
{
    bool PrecisaBalanceamento();
    void ExecutarBalanceamento();
}

public interface ICarregamentoTurbo
{
    bool ModoTurboAtivo { get; }
    void AtivarModoTurbo();
    void DesativarModoTurbo(string motivo);
}

public interface IModoFlutuacao
{
    bool ModoFlutuacaoAtivo { get; }
    void EntrarModoFlutuacao();
    void SairModoFlutuacao();
}
