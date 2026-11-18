package br.ufc.poo.bms;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import javax.swing.border.TitledBorder;
import java.awt.*;
import java.util.List;
import java.util.Locale;

/**
 * Interface gráfica Swing para o Sistema de Gestão e Telemetria de Baterias (BMS).
 */
public class BmsUI extends JFrame {
    private final ControladorBMS controlador;

    private JPanel painelModulos;
    private JPanel painelSensores;
    private JTextArea txtAlertas;
    private JTextArea txtTelemetria;
    private JLabel lblResumoStatus;

    public BmsUI(ControladorBMS controlador) {
        this.controlador = controlador;
        configurarJanela();
        inicializarComponentes();
        atualizarInterface();
    }

    private void configurarJanela() {
        setTitle("BMS UFC - Sistema de Gestão e Telemetria de Baterias");
        setSize(1120, 720);
        setMinimumSize(new Dimension(960, 620));
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setLocationRelativeTo(null);
    }

    private void inicializarComponentes() {
        JPanel raiz = new JPanel(new BorderLayout(10, 10));
        raiz.setBorder(new EmptyBorder(12, 12, 12, 12));
        raiz.setBackground(new Color(243, 245, 249));
        setContentPane(raiz);

        // Cabeçalho
        JPanel header = new JPanel(new BorderLayout());
        header.setBackground(new Color(24, 52, 94));
        header.setBorder(new EmptyBorder(12, 16, 12, 16));

        JLabel titulo = new JLabel("BMS UFC — Telemetria e Gestão de Baterias");
        titulo.setForeground(Color.WHITE);
        titulo.setFont(new Font("Segoe UI", Font.BOLD, 18));

        lblResumoStatus = new JLabel();
        lblResumoStatus.setForeground(new Color(210, 228, 255));
        lblResumoStatus.setFont(new Font("Segoe UI", Font.PLAIN, 13));

        header.add(titulo, BorderLayout.WEST);
        header.add(lblResumoStatus, BorderLayout.EAST);
        raiz.add(header, BorderLayout.NORTH);

        // Painel esquerdo de ações
        raiz.add(criarPainelAcoes(), BorderLayout.WEST);

        // Painel central com abas (Monitoramento Visual + Relatório Textual)
        JTabbedPane abas = new JTabbedPane();
        abas.setFont(new Font("Segoe UI", Font.BOLD, 12));

        JPanel abaVisual = new JPanel(new BorderLayout(8, 8));
        abaVisual.setBackground(Color.WHITE);
        abaVisual.setBorder(new EmptyBorder(8, 8, 8, 8));

        painelModulos = new JPanel();
        painelModulos.setLayout(new BoxLayout(painelModulos, BoxLayout.Y_AXIS));
        painelModulos.setBackground(Color.WHITE);

        JScrollPane scrollModulos = new JScrollPane(painelModulos);
        scrollModulos.setBorder(new TitledBorder("Módulos e Células"));

        painelSensores = new JPanel(new GridLayout(0, 2, 8, 8));
        painelSensores.setBackground(Color.WHITE);
        painelSensores.setBorder(new TitledBorder("Sensores Térmicos"));

        abaVisual.add(scrollModulos, BorderLayout.CENTER);
        abaVisual.add(painelSensores, BorderLayout.SOUTH);

        txtTelemetria = new JTextArea();
        txtTelemetria.setEditable(false);
        txtTelemetria.setFont(new Font("Consolas", Font.PLAIN, 12));
        JScrollPane scrollTelemetria = new JScrollPane(txtTelemetria);

        abas.addTab("Painel Visual", abaVisual);
        abas.addTab("Relatório de Telemetria (Texto)", scrollTelemetria);

        raiz.add(abas, BorderLayout.CENTER);

        // Painel inferior de histórico de alertas
        txtAlertas = new JTextArea(7, 40);
        txtAlertas.setEditable(false);
        txtAlertas.setFont(new Font("Consolas", Font.PLAIN, 12));
        txtAlertas.setBackground(new Color(252, 252, 252));

        JScrollPane scrollAlertas = new JScrollPane(txtAlertas);
        scrollAlertas.setBorder(new TitledBorder("Histórico de Alertas do Sistema"));
        raiz.add(scrollAlertas, BorderLayout.SOUTH);
    }

    private JPanel criarPainelAcoes() {
        JPanel painel = new JPanel();
        painel.setLayout(new BoxLayout(painel, BoxLayout.Y_AXIS));
        painel.setPreferredSize(new Dimension(245, 0));
        painel.setBorder(new TitledBorder("Operações do BMS"));
        painel.setBackground(Color.WHITE);

        painel.add(criarBotao("Varredura e Telemetria", new Color(33, 102, 172), e -> {
            controlador.realizarVarreduraCompleta();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(8));

        painel.add(criarBotao("Simular Carga (+120 mV)", new Color(46, 125, 50), e -> {
            executarCicloCarga();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(8));

        painel.add(criarBotao("Simular Descarga (-150 mV)", new Color(230, 126, 34), e -> {
            executarCicloDescarga();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(8));

        painel.add(criarBotao("Balanceamento Passivo", new Color(106, 27, 154), e -> {
            executarBalanceamento();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(14));

        JLabel lblCapacidades = new JLabel("Capacidades por Tipo:");
        lblCapacidades.setFont(new Font("Segoe UI", Font.BOLD, 12));
        lblCapacidades.setAlignmentX(Component.LEFT_ALIGNMENT);
        painel.add(lblCapacidades);
        painel.add(Box.createVerticalStrut(6));

        painel.add(criarBotao("Ativar Turbo (Li-Ion)", new Color(0, 131, 143), e -> {
            ativarTurboLiIon();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(8));

        painel.add(criarBotao("Modo Flutuação (LiFePO4)", new Color(21, 101, 192), e -> {
            ativarFlutuacaoLiFePO4();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(14));

        JLabel lblFalhas = new JLabel("Injeção de Falhas:");
        lblFalhas.setFont(new Font("Segoe UI", Font.BOLD, 12));
        lblFalhas.setAlignmentX(Component.LEFT_ALIGNMENT);
        painel.add(lblFalhas);
        painel.add(Box.createVerticalStrut(6));

        painel.add(criarBotao("Sobretensão C01 (4.10 V)", new Color(198, 40, 40), e -> {
            controlador.getModulos().get(0).getCelulas().get(0).setTensaoAtual(4.10);
            controlador.realizarVarreduraCompleta();
            atualizarInterface();
        }));
        painel.add(Box.createVerticalStrut(8));

        painel.add(criarBotao("Superaquecimento ST-02", new Color(183, 28, 28), e -> {
            controlador.getSensores().get(1).simularLeituraTermica(72.0);
            controlador.realizarVarreduraCompleta();
            atualizarInterface();
        }));

        painel.add(Box.createVerticalGlue());
        return painel;
    }

    private JButton criarBotao(String texto, Color corFundo, java.awt.event.ActionListener acao) {
        JButton btn = new JButton(texto);
        btn.setMaximumSize(new Dimension(Integer.MAX_VALUE, 38));
        btn.setAlignmentX(Component.LEFT_ALIGNMENT);
        btn.setBackground(corFundo);
        btn.setForeground(Color.WHITE);
        btn.setFocusPainted(false);
        btn.setFont(new Font("Segoe UI", Font.BOLD, 12));
        btn.addActionListener(acao);
        return btn;
    }

    private void executarCicloCarga() {
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                celula.simularCarga(120.0, 15.0, 1.8);
            }
        }
        for (SensorTemperatura sensor : controlador.getSensores()) {
            sensor.simularLeituraTermica(sensor.getTemperaturaCelsius() + 1.5);
        }
        controlador.realizarVarreduraCompleta();
    }

    private void executarCicloDescarga() {
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
        }
        controlador.realizarVarreduraCompleta();
    }

    private void executarBalanceamento() {
        for (ModuloBateria modulo : controlador.getModulos()) {
            modulo.executarBalanceamentoPassivo(modulo.calcularTensaoMedia());
        }
        controlador.realizarVarreduraCompleta();
    }

    private void ativarTurboLiIon() {
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                if (celula instanceof CarregamentoTurbo turbo) {
                    turbo.ativarModoTurbo();
                }
            }
        }
        controlador.realizarVarreduraCompleta();
    }

    private void ativarFlutuacaoLiFePO4() {
        for (ModuloBateria modulo : controlador.getModulos()) {
            for (CelulaBateria celula : modulo.getCelulas()) {
                if (celula instanceof ModoFlutuacao flutuacao) {
                    flutuacao.entrarModoFlutuacao();
                }
            }
        }
        controlador.realizarVarreduraCompleta();
    }

    private void atualizarInterface() {
        atualizarModulos();
        atualizarSensores();
        atualizarAlertas();
        txtTelemetria.setText(controlador.gerarRelatorioTelemetria());
        txtTelemetria.setCaretPosition(0);

        int totalCelulas = controlador.getModulos().stream().mapToInt(m -> m.getCelulas().size()).sum();
        lblResumoStatus.setText(String.format(Locale.US,
                "Módulos: %d  |  Células: %d  |  Sensores: %d  |  Alertas Globais (static): %d",
                controlador.getModulos().size(),
                totalCelulas,
                controlador.getSensores().size(),
                AlertaSistema.getTotalAlertasGerados()));
    }

    private void atualizarModulos() {
        painelModulos.removeAll();
        for (ModuloBateria modulo : controlador.getModulos()) {
            JPanel cardModulo = new JPanel(new BorderLayout(6, 6));
            cardModulo.setBackground(new Color(248, 250, 252));
            cardModulo.setBorder(BorderFactory.createCompoundBorder(
                    BorderFactory.createLineBorder(new Color(205, 214, 226)),
                    new EmptyBorder(8, 10, 8, 10)));

            JLabel lblTituloMod = new JLabel(String.format(Locale.US,
                    "%s  —  Total: %.3f V  |  Média: %.3f V  |  Desbalanceamento: %.3f V",
                    modulo.getNomeModulo(),
                    modulo.calcularTensaoTotal(),
                    modulo.calcularTensaoMedia(),
                    modulo.calcularDesbalanceamento()));
            lblTituloMod.setFont(new Font("Segoe UI", Font.BOLD, 13));
            cardModulo.add(lblTituloMod, BorderLayout.NORTH);

            JPanel gridCelulas = new JPanel(new GridLayout(1, 0, 8, 8));
            gridCelulas.setOpaque(false);

            for (CelulaBateria celula : modulo.getCelulas()) {
                gridCelulas.add(criarCardCelula(celula));
            }

            cardModulo.add(gridCelulas, BorderLayout.CENTER);
            painelModulos.add(cardModulo);
            painelModulos.add(Box.createVerticalStrut(10));
        }
        painelModulos.revalidate();
        painelModulos.repaint();
    }

    private JPanel criarCardCelula(CelulaBateria celula) {
        boolean segura = celula.verificarSeguranca();
        double soc = celula.calcularEstadoCarga();
        double tempK = ConversorUnidades.celsiusParaKelvin(celula.getTemperaturaCelsius());

        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(segura ? new Color(235, 248, 238) : new Color(253, 232, 232));
        card.setBorder(BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(segura ? new Color(76, 175, 80) : new Color(211, 47, 47), 2),
                new EmptyBorder(8, 10, 8, 10)));

        JLabel lblId = new JLabel(celula.getId() + " (" + celula.getClass().getSimpleName() + ")");
        lblId.setFont(new Font("Segoe UI", Font.BOLD, 12));

        JLabel lblTensao = new JLabel(String.format(Locale.US, "Tensão: %.3f V | I: %.1f A",
                celula.getTensaoAtual(), celula.getCorrenteAtual()));
        JLabel lblTemp = new JLabel(String.format(Locale.US, "Temp: %.1f °C (%.1f K)",
                celula.getTemperaturaCelsius(), tempK));
        JLabel lblStatus = new JLabel("Status: " + (segura ? "SEGURA" : "ALERTA / RISCO"));
        lblStatus.setForeground(segura ? new Color(27, 94, 32) : new Color(183, 28, 28));
        lblStatus.setFont(new Font("Segoe UI", Font.BOLD, 11));

        String textoModo = "";
        if (celula instanceof CarregamentoTurbo turbo) {
            textoModo = turbo.isModoTurboAtivo() ? "Modo: TURBO ATIVO (2x)" : "Modo: Normal (Turbo disp.)";
        } else if (celula instanceof ModoFlutuacao flutuacao) {
            textoModo = flutuacao.isEmFlutuacao() ? "Modo: FLUTUAÇÃO (Manut.)" : "Modo: Normal (Flut. disp.)";
        }
        JLabel lblModo = new JLabel(textoModo);
        lblModo.setFont(new Font("Segoe UI", Font.ITALIC, 11));
        lblModo.setForeground(new Color(55, 71, 79));

        JProgressBar barraSoc = new JProgressBar(0, 100);
        barraSoc.setValue((int) Math.round(soc));
        barraSoc.setStringPainted(true);
        barraSoc.setString(String.format(Locale.US, "SoC: %.1f%%", soc));

        card.add(lblId);
        card.add(Box.createVerticalStrut(4));
        card.add(lblTensao);
        card.add(lblTemp);
        card.add(Box.createVerticalStrut(4));
        card.add(barraSoc);
        card.add(Box.createVerticalStrut(4));
        card.add(lblStatus);
        card.add(lblModo);

        return card;
    }

    private void atualizarSensores() {
        painelSensores.removeAll();
        for (SensorTemperatura sensor : controlador.getSensores()) {
            boolean ok = sensor.isOperacional();
            double tempK = ConversorUnidades.celsiusParaKelvin(sensor.getTemperaturaCelsius());

            JLabel lblSensor = new JLabel(String.format(Locale.US,
                    "  %s (%s): %.2f °C / %.2f K — [%s]",
                    sensor.getId(), sensor.getLocalizacao(),
                    sensor.getTemperaturaCelsius(), tempK,
                    ok ? "OK" : "FALHA"));
            lblSensor.setOpaque(true);
            lblSensor.setBorder(new EmptyBorder(6, 8, 6, 8));
            lblSensor.setBackground(ok ? new Color(235, 248, 238) : new Color(253, 232, 232));
            lblSensor.setForeground(ok ? new Color(27, 94, 32) : new Color(183, 28, 28));
            lblSensor.setFont(new Font("Segoe UI", Font.BOLD, 12));
            painelSensores.add(lblSensor);
        }
        painelSensores.revalidate();
        painelSensores.repaint();
    }

    private void atualizarAlertas() {
        List<AlertaSistema> alertas = controlador.getHistoricoAlertas();
        StringBuilder sb = new StringBuilder();
        if (alertas.isEmpty()) {
            sb.append("Nenhum alerta registrado até o momento.");
        } else {
            for (int i = alertas.size() - 1; i >= 0; i--) {
                sb.append(alertas.get(i)).append('\n');
            }
        }
        txtAlertas.setText(sb.toString());
        txtAlertas.setCaretPosition(0);
    }
}
