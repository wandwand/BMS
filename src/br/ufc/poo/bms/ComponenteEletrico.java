package br.ufc.poo.bms;

/**
 * Classe base abstrata para todos os componentes elétricos monitorados pelo BMS.
 */
public abstract class ComponenteEletrico {
    protected String id;
    protected double temperaturaCelsius;
    protected boolean operacional;

    protected ComponenteEletrico(String id, double temperaturaCelsius, boolean operacional) {
        this.id = id;
        this.temperaturaCelsius = temperaturaCelsius;
        this.operacional = operacional;
    }

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public double getTemperaturaCelsius() {
        return temperaturaCelsius;
    }

    public void setTemperaturaCelsius(double temperaturaCelsius) {
        this.temperaturaCelsius = temperaturaCelsius;
    }

    public boolean isOperacional() {
        return operacional;
    }

    public void setOperacional(boolean operacional) {
        this.operacional = operacional;
    }

    public abstract boolean realizarAutodiagnostico();
}
