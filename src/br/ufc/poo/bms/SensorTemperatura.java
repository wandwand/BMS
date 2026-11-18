package br.ufc.poo.bms;

/**
 * Sensor térmico responsável por monitorar pontos físicos do conjunto de baterias.
 */
public class SensorTemperatura extends ComponenteEletrico {
    private static final double LIMITE_FISICO_MIN = -40.0;
    private static final double LIMITE_FISICO_MAX = 125.0;
    private static final double LIMITE_OPERACIONAL_MAX = 60.0;

    private String localizacao;

    public SensorTemperatura(String id, double temperaturaCelsius, boolean operacional, String localizacao) {
        super(id, temperaturaCelsius, operacional);
        this.localizacao = localizacao;
    }

    public String getLocalizacao() {
        return localizacao;
    }

    public void setLocalizacao(String localizacao) {
        this.localizacao = localizacao;
    }

    public double simularLeituraTermica(double novaTemperaturaCelsius) {
        this.temperaturaCelsius = novaTemperaturaCelsius;
        return this.temperaturaCelsius;
    }

    @Override
    public boolean realizarAutodiagnostico() {
        boolean leituraValida = temperaturaCelsius >= LIMITE_FISICO_MIN && temperaturaCelsius <= LIMITE_FISICO_MAX;
        boolean dentroDoLimite = temperaturaCelsius <= LIMITE_OPERACIONAL_MAX;
        this.operacional = leituraValida && dentroDoLimite;
        return this.operacional;
    }
}
