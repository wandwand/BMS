package br.ufc.poo.bms;

/**
 * Célula de bateria Lítio-Ferro-Fosfato (LiFePO4) com limites específicos de segurança,
 * SoC e suporte a Modo Flutuação (manutenção em carga cheia).
 */
public class CelulaLiFePO4 extends CelulaBateria implements ModoFlutuacao {
    private static final double TENSAO_MIN = 2.50;
    private static final double TENSAO_MAX = 3.65;
    private static final double TENSAO_FLUTUACAO = 3.45;
    private static final double TEMPERATURA_MIN = -10.0;
    private static final double TEMPERATURA_MAX = 55.0;

    private boolean emFlutuacao;

    public CelulaLiFePO4(
            String id,
            double temperaturaCelsius,
            boolean operacional,
            double tensaoAtual,
            double correnteAtual,
            double capacidadeAh) {
        super(id, temperaturaCelsius, operacional, tensaoAtual, correnteAtual, capacidadeAh);
        this.emFlutuacao = false;
    }

    @Override
    public void entrarModoFlutuacao() {
        this.emFlutuacao = true;
        if (this.tensaoAtual > TENSAO_FLUTUACAO) {
            this.tensaoAtual = TENSAO_FLUTUACAO;
        }
        this.correnteAtual = 0.5; // Corrente mínima de manutenção
    }

    @Override
    public void sairModoFlutuacao() {
        this.emFlutuacao = false;
    }

    @Override
    public boolean isEmFlutuacao() {
        return emFlutuacao;
    }

    @Override
    public void simularCarga(double deltaMilivolts, double correnteCarga, double deltaTemperatura) {
        if (emFlutuacao || calcularEstadoCarga() >= 95.0) {
            entrarModoFlutuacao();
            // Em flutuação, a célula estabiliza a tensão e resfria levemente
            this.temperaturaCelsius = Math.max(25.0, this.temperaturaCelsius - 0.3);
            return;
        }
        super.simularCarga(deltaMilivolts, correnteCarga, deltaTemperatura * 0.8);
    }

    @Override
    public boolean simularDescarga(double deltaMilivolts, double correnteDescarga, double deltaTemperatura) {
        sairModoFlutuacao();
        return super.simularDescarga(deltaMilivolts, correnteDescarga, deltaTemperatura);
    }

    @Override
    public double calcularEstadoCarga() {
        if (tensaoAtual <= TENSAO_MIN) {
            return 0.0;
        }
        if (tensaoAtual >= TENSAO_MAX) {
            return 100.0;
        }
        double normalizado = (tensaoAtual - TENSAO_MIN) / (TENSAO_MAX - TENSAO_MIN);
        return Math.round(normalizado * 1000.0) / 10.0;
    }

    @Override
    public boolean verificarSeguranca() {
        return tensaoAtual >= TENSAO_MIN
                && tensaoAtual <= TENSAO_MAX
                && temperaturaCelsius >= TEMPERATURA_MIN
                && temperaturaCelsius <= TEMPERATURA_MAX;
    }

    @Override
    public boolean realizarAutodiagnostico() {
        this.operacional = verificarSeguranca();
        return this.operacional;
    }
}
