package br.ufc.poo.bms;

/**
 * Capacidade para células que suportam carga de manutenção (Modo Flutuação).
 */
public interface ModoFlutuacao {
    void entrarModoFlutuacao();
    void sairModoFlutuacao();
    boolean isEmFlutuacao();
}
