package br.ufc.poo.bms;

import javax.swing.SwingUtilities;
import java.util.List;
import java.util.Locale;
import java.util.Scanner;

/**
 * Ponto de entrada da aplicação BMS (abre a interface Swing por padrão ou console com --console).
 */
public class Main {
    public static void main(String[] args) {
        Locale.setDefault(Locale.US);
        ControladorBMS controlador = inicializarSistema();

        if (args.length > 0 && "--console".equalsIgnoreCase(args[0])) {
            executarModoConsole(controlador);
            return;
        }

        SwingUtilities.invokeLater(() -> {
            BmsUI ui = new BmsUI(controlador);
            ui.setVisible(true);
        });
    }

    private static void executarModoConsole(ControladorBMS controlador) {
        Scanner scanner = new Scanner(System.in);

        int opcao = -1;
        while (opcao != 0) {
            exibirMenu();
            System.out.print("Escolha uma opção: ");

            String entrada = scanner.nextLine().trim();
            try {
                opcao = Integer.parseInt(entrada);
            } catch (NumberFormatException e) {
                System.out.println("Opção inválida. Digite um número de 0 a 6.");
                continue;
            }

            switch (opcao) {
                case 1 -> {
                    controlador.realizarVarreduraCompleta();
                    System.out.println(controlador.gerarRelatorioTelemetria());
                }
                case 2 -> simularCicloCarga(controlador);
                case 3 -> simularCicloDescarga(controlador);
                case 4 -> injetarFalhaSimulada(controlador, scanner);
                case 5 -> acionarBalanceamentoManual(controlador);
                case 6 -> exibirHistoricoAlertas(controlador);
                case 7 -> acionarCapacidadesEspeciais(controlador);
                case 0 -> System.out.println("Encerrando o Sistema de Gestão de Baterias (BMS)...");
                default -> System.out.println("Opção inválida. Escolha entre 0 e 7.");
            }
        }
        scanner.close();
    }

    private static ControladorBMS inicializarSistema() {
        ControladorBMS bms = new ControladorBMS();

        ModuloBateria moduloA = new ModuloBateria("MOD-A (LiFePO4)");
        moduloA.adicionarCelula(new CelulaLiFePO4("C01", 28.0, true, 3.28, 0.0, 100.0));
        moduloA.adicionarCelula(new CelulaLiFePO4("C02", 28.5, true, 3.31, 0.0, 100.0));
        moduloA.adicionarCelula(new CelulaLiFePO4("C03", 29.0, true, 3.39, 0.0, 100.0));

        ModuloBateria moduloB = new ModuloBateria("MOD-B (Li-Ion NMC)");
        moduloB.adicionarCelula(new CelulaLiIon("C04", 30.0, true, 3.75, 0.0, 60.0));
        moduloB.adicionarCelula(new CelulaLiIon("C05", 30.2, true, 3.82, 0.0, 60.0));
        moduloB.adicionarCelula(new CelulaLiIon("C06", 29.8, true, 3.74, 0.0, 60.0));

        bms.adicionarModulo(moduloA);
        bms.adicionarModulo(moduloB);

        bms.adicionarSensor(new SensorTemperatura("ST-01", 29.0, true, "Barramento Módulo A"));
        bms.adicionarSensor(new SensorTemperatura("ST-02", 30.5, true, "Barramento Módulo B"));

        return bms;
    }

    private static void exibirMenu() {
        System.out.println("\n---------------- MENU PRINCIPAL - BMS UFC ----------------");
        System.out.println("1) Visualizar telemetria completa do BMS");
        System.out.println("2) Simular ciclo de carga");
        System.out.println("3) Simular ciclo de descarga");
        System.out.println("4) Injetar falha simulada (sobretensão / superaquecimento)");
        System.out.println("5) Acionar balanceamento passivo nos módulos");
        System.out.println("6) Exibir histórico de alertas e total global");
        System.out.println("7) Ativar capacidades especiais (Turbo Li-Ion / Flutuação LiFePO4)");
        System.out.println("0) Sair");
        System.out.println("----------------------------------------------------------");
    }

    private static void simularCicloCarga(ControladorBMS controlador) {
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                celula.simularCarga(120.0, 15.0, 1.8);
            }
        }
        for (SensorTemperatura sensor : controlador.getSensores()) {
            sensor.simularLeituraTermica(sensor.getTemperaturaCelsius() + 1.5);
        }
        controlador.realizarVarreduraCompleta();
        System.out.println("Ciclo de carga simulado (+120 mV por célula) e varredura executada.");
    }

    private static void simularCicloDescarga(ControladorBMS controlador) {
        boolean algumaDescarregou = false;
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                if (celula.simularDescarga(150.0, 22.0, 1.2)) {
                    algumaDescarregou = true;
                }
            }
        }
        if (algumaDescarregou) {
            for (SensorTemperatura sensor : controlador.getSensores()) {
                sensor.simularLeituraTermica(sensor.getTemperaturaCelsius() + 1.0);
            }
            System.out.println("Ciclo de descarga simulado (-150 mV por célula) e varredura executada.");
        } else {
            System.out.println("Todas as células já estão em 0.0 V. Nenhuma descarga aplicada.");
        }
        controlador.realizarVarreduraCompleta();
    }

    private static void injetarFalhaSimulada(ControladorBMS controlador, Scanner scanner) {
        System.out.println("Escolha o tipo de falha para injetar:");
        System.out.println("  1) Sobretensão em célula do Módulo A (4.10 V em LiFePO4)");
        System.out.println("  2) Superaquecimento no Sensor ST-02 (72.0 °C)");
        System.out.print("Opção: ");
        String escolha = scanner.nextLine().trim();

        if ("1".equals(escolha)) {
            CelulaBateria celulaAlvo = controlador.getModulos().get(0).getCelulas().get(0);
            celulaAlvo.setTensaoAtual(4.10);
            System.out.println("Falha de sobretensão injetada na célula " + celulaAlvo.getId() + ".");
        } else {
            SensorTemperatura sensorAlvo = controlador.getSensores().get(1);
            sensorAlvo.simularLeituraTermica(72.0);
            System.out.println("Superaquecimento injetado no sensor " + sensorAlvo.getId() + ".");
        }

        controlador.realizarVarreduraCompleta();
        System.out.println("Varredura de autodiagnóstico concluída. Verifique o histórico de alertas (opção 6).");
    }

    private static void acionarBalanceamentoManual(ControladorBMS controlador) {
        for (ModuloBateria modulo : controlador.getModulos()) {
            double media = modulo.calcularTensaoMedia();
            modulo.executarBalanceamentoPassivo(media);
            System.out.printf("Balanceamento passivo executado em %s (alvo: %.3f V).%n",
                    modulo.getNomeModulo(), media);
        }
    }

    private static void exibirHistoricoAlertas(ControladorBMS controlador) {
        List<AlertaSistema> alertas = controlador.getHistoricoAlertas();
        System.out.println("\n=== HISTÓRICO DE ALERTAS DO BMS ===");
        System.out.println("Total global de alertas gerados (static): " + AlertaSistema.getTotalAlertasGerados());
        if (alertas.isEmpty()) {
            System.out.println("Nenhum alerta registrado até o momento.");
        } else {
            for (AlertaSistema alerta : alertas) {
                System.out.println("  " + alerta);
            }
        }
    }

    private static void acionarCapacidadesEspeciais(ControladorBMS controlador) {
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                if (celula instanceof CarregamentoTurbo turbo) {
                    turbo.ativarModoTurbo();
                    System.out.println("Célula " + celula.getId() + " -> Modo Turbo: "
                            + (turbo.isModoTurboAtivo() ? "ATIVADO" : "BLOQUEADO (Temp/SoC)"));
                } else if (celula instanceof ModoFlutuacao flutuacao) {
                    flutuacao.entrarModoFlutuacao();
                    System.out.println("Célula " + celula.getId() + " -> Modo Flutuação ATIVADO.");
                }
            }
        }
        controlador.realizarVarreduraCompleta();
    }
}
