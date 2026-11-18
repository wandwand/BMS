package br.ufc.poo.bms;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Locale;

/**
 * Controlador central do Sistema de Gestão e Telemetria de Baterias (BMS).
 */
public class ControladorBMS implements Monitoravel {
    private static final double LIMITE_DESBALANCEAMENTO_V = 0.05;

    private List<ModuloBateria> modulos;
    private List<SensorTemperatura> sensores;
    private List<AlertaSistema> historicoAlertas;

    public ControladorBMS() {
        this.modulos = new ArrayList<>();
        this.sensores = new ArrayList<>();
        this.historicoAlertas = new ArrayList<>();
    }

    public void adicionarModulo(ModuloBateria modulo) {
        if (modulo != null) {
            modulos.add(modulo);
        }
    }

    public void adicionarSensor(SensorTemperatura sensor) {
        if (sensor != null) {
            sensores.add(sensor);
        }
    }

    public List<ModuloBateria> getModulos() {
        return Collections.unmodifiableList(modulos);
    }

    public List<SensorTemperatura> getSensores() {
        return Collections.unmodifiableList(sensores);
    }

    public List<AlertaSistema> getHistoricoAlertas() {
        return Collections.unmodifiableList(historicoAlertas);
    }

    /**
     * Executa varredura completa do sistema, realizando autodiagnóstico polimórfico,
     * verificação de segurança, cálculo de SoC e balanceamento automático se necessário.
     */
    public void realizarVarreduraCompleta() {
        for (SensorTemperatura sensor : sensores) {
            if (!sensor.realizarAutodiagnostico()) {
                double tempK = ConversorUnidades.celsiusParaKelvin(sensor.getTemperaturaCelsius());
                historicoAlertas.add(new AlertaSistema(
                        "FALHA_TERMICA_SENSOR",
                        "ALTA",
                        String.format(Locale.US, "Sensor %s (%s) fora da faixa operacional: %.2f °C (%.2f K).",
                                sensor.getId(), sensor.getLocalizacao(), sensor.getTemperaturaCelsius(), tempK)
                ));
            }
        }

        for (ModuloBateria modulo : modulos) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                boolean diagnosticoOk = celula.realizarAutodiagnostico();
                boolean segura = celula.verificarSeguranca();
                double soc = celula.calcularEstadoCarga();

                if (celula instanceof CarregamentoTurbo turbo && turbo.isModoTurboAtivo()
                        && (celula.getTemperaturaCelsius() > 40.0 || soc >= 80.0)) {
                    turbo.desativarModoTurbo();
                    historicoAlertas.add(new AlertaSistema(
                            "PROTECAO_TURBO",
                            "MEDIA",
                            String.format(Locale.US,
                                    "Modo Turbo desativado automaticamente na célula %s (Temp: %.1f°C | SoC: %.1f%%).",
                                    celula.getId(), celula.getTemperaturaCelsius(), soc)
                    ));
                }

                if (celula instanceof ModoFlutuacao flutuacao && soc >= 95.0 && !flutuacao.isEmFlutuacao()) {
                    flutuacao.entrarModoFlutuacao();
                }

                if (!diagnosticoOk || !segura) {
                    double tempK = ConversorUnidades.celsiusParaKelvin(celula.getTemperaturaCelsius());
                    historicoAlertas.add(new AlertaSistema(
                            "ANOMALIA_CELULA",
                            "CRITICA",
                            String.format(Locale.US,
                                    "Módulo %s | Célula %s (%s) em risco! Tensão: %.3fV | Temp: %.2f°C (%.2fK) | SoC: %.1f%%",
                                    modulo.getNomeModulo(), celula.getId(), celula.getClass().getSimpleName(),
                                    celula.getTensaoAtual(), celula.getTemperaturaCelsius(), tempK, soc)
                    ));
                }
            }

            double desbalanceamento = modulo.calcularDesbalanceamento();
            if (desbalanceamento > LIMITE_DESBALANCEAMENTO_V) {
                double tensaoMedia = modulo.calcularTensaoMedia();
                Balanceavel alvoBalanceamento = modulo;
                alvoBalanceamento.executarBalanceamentoPassivo(tensaoMedia);

                historicoAlertas.add(new AlertaSistema(
                        "DESBALANCEAMENTO_MODULO",
                        "MEDIA",
                        String.format(Locale.US,
                                "Módulo %s apresentou desbalanceamento de %.3fV (> %.2fV). Balanceamento passivo acionado para %.3fV.",
                                modulo.getNomeModulo(), desbalanceamento, LIMITE_DESBALANCEAMENTO_V, tensaoMedia)
                ));
            }
        }
    }

    @Override
    public String gerarRelatorioTelemetria() {
        StringBuilder sb = new StringBuilder();
        sb.append("=====================================================================\n");
        sb.append("                 RELATÓRIO DE TELEMETRIA - BMS UFC                   \n");
        sb.append("=====================================================================\n");

        sb.append("\n[SENSORES TÉRMICOS]\n");
        for (SensorTemperatura sensor : sensores) {
            double tempK = ConversorUnidades.celsiusParaKelvin(sensor.getTemperaturaCelsius());
            sb.append(String.format(Locale.US,
                    "  - %-6s | Local: %-22s | Temp: %6.2f °C (%6.2f K) | Status: %s%n",
                    sensor.getId(),
                    sensor.getLocalizacao(),
                    sensor.getTemperaturaCelsius(),
                    tempK,
                    sensor.isOperacional() ? "OK" : "FALHA"));
        }

        sb.append("\n[MÓDULOS E CÉLULAS]\n");
        for (ModuloBateria modulo : modulos) {
            sb.append(String.format(Locale.US,
                    "\n  >> %s | Tensão Total: %.3f V | Tensão Média: %.3f V | Desbal.: %.3f V%n",
                    modulo.getNomeModulo(),
                    modulo.calcularTensaoTotal(),
                    modulo.calcularTensaoMedia(),
                    modulo.calcularDesbalanceamento()));

            for (CelulaBateria celula : modulo.getCelulas()) {
                double tempK = ConversorUnidades.celsiusParaKelvin(celula.getTemperaturaCelsius());
                String extra = "";
                if (celula instanceof CarregamentoTurbo turbo) {
                    extra = turbo.isModoTurboAtivo() ? " | [TURBO ON]" : " | [TURBO OFF]";
                } else if (celula instanceof ModoFlutuacao flutuacao) {
                    extra = flutuacao.isEmFlutuacao() ? " | [FLUTUACAO]" : " | [CARGA NORMAL]";
                }
                sb.append(String.format(Locale.US,
                        "     * %-6s [%-13s] | V: %5.3f V | I: %6.2f A | Temp: %5.2f °C (%6.2f K) | SoC: %5.1f%% | %s%s%n",
                        celula.getId(),
                        celula.getClass().getSimpleName(),
                        celula.getTensaoAtual(),
                        celula.getCorrenteAtual(),
                        celula.getTemperaturaCelsius(),
                        tempK,
                        celula.calcularEstadoCarga(),
                        celula.verificarSeguranca() ? "SEGURA" : "ALERTA",
                        extra));
            }
        }

        sb.append("\n=====================================================================\n");
        return sb.toString();
    }
}
