package br.ufc.poo.bms;

/**
 * Classe abstrata que representa uma célula eletroquímica de bateria.
 */
public abstract class CelulaBateria extends ComponenteEletrico {
    protected double tensaoAtual;
    protected double correnteAtual;
    protected double capacidadeAh;

    protected CelulaBateria(
            String id,
            double temperaturaCelsius,
            boolean operacional,
            double tensaoAtual,
            double correnteAtual,
            double capacidadeAh) {
        super(id, temperaturaCelsius, operacional);
        this.tensaoAtual = tensaoAtual;
        this.correnteAtual = correnteAtual;
        this.capacidadeAh = capacidadeAh;
    }

    public double getTensaoAtual() {
        return tensaoAtual;
    }

    public void setTensaoAtual(double tensaoAtual) {
        this.tensaoAtual = tensaoAtual;
    }

    public double getCorrenteAtual() {
        return correnteAtual;
    }

    public void setCorrenteAtual(double correnteAtual) {
        this.correnteAtual = correnteAtual;
    }

    public double getCapacidadeAh() {
        return capacidadeAh;
    }

    public void setCapacidadeAh(double capacidadeAh) {
        this.capacidadeAh = capacidadeAh;
    }

    /**
     * Simula um incremento de carga em milivolts, elevando tensão e temperatura.
     */
    public void simularCarga(double deltaMilivolts, double correnteCarga, double deltaTemperatura) {
        this.tensaoAtual += ConversorUnidades.milivoltsParaVolts(Math.abs(deltaMilivolts));
        this.correnteAtual = Math.abs(correnteCarga);
        this.temperaturaCelsius += Math.abs(deltaTemperatura);
    }

    /**
     * Simula um ciclo de descarga em milivolts, reduzindo tensão e elevando levemente a temperatura.
     * Se a tensão já estiver em 0.0 V, não realiza descarga nem altera temperatura/corrente.
     *
     * @return true se houve descarga; false se a célula já estava totalmente descarregada (0.0 V).
     */
    public boolean simularDescarga(double deltaMilivolts, double correnteDescarga, double deltaTemperatura) {
        if (this.tensaoAtual <= 0.0) {
            this.tensaoAtual = 0.0;
            this.correnteAtual = 0.0;
            return false;
        }

        double deltaVolts = ConversorUnidades.milivoltsParaVolts(Math.abs(deltaMilivolts));
        double tensaoAnterior = this.tensaoAtual;
        this.tensaoAtual = Math.max(0.0, tensaoAnterior - deltaVolts);

        if (this.tensaoAtual <= 0.0) {
            this.correnteAtual = 0.0;
            double fator = (deltaVolts > 0.0) ? (tensaoAnterior / deltaVolts) : 0.0;
            this.temperaturaCelsius += Math.abs(deltaTemperatura) * fator;
        } else {
            this.correnteAtual = -Math.abs(correnteDescarga);
            this.temperaturaCelsius += Math.abs(deltaTemperatura);
        }
        return true;
    }

    public abstract double calcularEstadoCarga();

    public abstract boolean verificarSeguranca();
}
