package br.ufc.poo.bms;

import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

/**
 * Registro de alerta/falha detectada pelo controlador do BMS.
 */
public class AlertaSistema {
    private static int totalAlertasGerados = 0;

    private int idAlerta;
    private String tipoFalha;
    private String severidade;
    private String mensagem;
    private LocalDateTime timestamp;

    public AlertaSistema(String tipoFalha, String severidade, String mensagem) {
        totalAlertasGerados++;
        this.idAlerta = totalAlertasGerados;
        this.tipoFalha = tipoFalha;
        this.severidade = severidade;
        this.mensagem = mensagem;
        this.timestamp = LocalDateTime.now();
    }

    public static int getTotalAlertasGerados() {
        return totalAlertasGerados;
    }

    public int getIdAlerta() {
        return idAlerta;
    }

    public String getTipoFalha() {
        return tipoFalha;
    }

    public String getSeveridade() {
        return severidade;
    }

    public String getMensagem() {
        return mensagem;
    }

    public LocalDateTime getTimestamp() {
        return timestamp;
    }

    @Override
    public String toString() {
        String dataHora = timestamp.format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"));
        return String.format("[#%03d | %s] [%s] %s: %s", idAlerta, dataHora, severidade, tipoFalha, mensagem);
    }
}
