package br.ufc.poo.bms;

/**
 * Célula de bateria Lítio-Íon NMC com limites específicos de segurança,
 * SoC e suporte a Carregamento Turbo (com proteção térmica e CC-CV).
 */
public class CelulaLiIon extends CelulaBateria implements CarregamentoTurbo {
    private static final double TENSAO_MIN = 3.00;
    private static final double TENSAO_MAX = 4.20;
    private static final double TEMPERATURA_MIN = 0.0;
    private static final double TEMPERATURA_MAX = 45.0;
    private static final double LIMITE_TEMP_TURBO = 40.0;

    private boolean modoTurboAtivo;

    public CelulaLiIon(
            String id,
            double temperaturaCelsius,
            boolean operacional,
            double tensaoAtual,
            double correnteAtual,
            double capacidadeAh) {
        super(id, temperaturaCelsius, operacional, tensaoAtual, correnteAtual, capacidadeAh);
        this.modoTurboAtivo = false;
    }

    @Override
    public void ativarModoTurbo() {
        if (temperaturaCelsius <= LIMITE_TEMP_TURBO && calcularEstadoCarga() < 80.0) {
            this.modoTurboAtivo = true;
        } else {
            this.modoTurboAtivo = false;
        }
    }

    @Override
    public void desativarModoTurbo() {
        this.modoTurboAtivo = false;
    }

    @Override
    public boolean isModoTurboAtivo() {
        return modoTurboAtivo;
    }

    @Override
    public void simularCarga(double deltaMilivolts, double correnteCarga, double deltaTemperatura) {
        // Desativa turbo automaticamente acima de 80% SoC (fase CV) ou acima de 40°C
        if (modoTurboAtivo && (temperaturaCelsius > LIMITE_TEMP_TURBO || calcularEstadoCarga() >= 80.0)) {
            desativarModoTurbo();
        }

        if (modoTurboAtivo) {
            super.simularCarga(deltaMilivolts * 2.0, correnteCarga * 2.0, deltaTemperatura * 1.6);
        } else {
            super.simularCarga(deltaMilivolts, correnteCarga, deltaTemperatura);
        }
    }

    @Override
    public boolean simularDescarga(double deltaMilivolts, double correnteDescarga, double deltaTemperatura) {
        desativarModoTurbo();
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
