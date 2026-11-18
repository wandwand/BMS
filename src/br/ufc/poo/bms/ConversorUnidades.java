package br.ufc.poo.bms;

/**
 * Classe utilitária com conversões de unidades físicas usadas no BMS.
 */
public final class ConversorUnidades {

    private ConversorUnidades() {
        // Classe utilitária não deve ser instanciada
    }

    public static double celsiusParaKelvin(double celsius) {
        return celsius + 273.15;
    }

    public static double milivoltsParaVolts(double mv) {
        return mv / 1000.0;
    }
}
