package br.ufc.poo.bms;

/**
 * Capacidade para células que suportam carregamento rápido (Modo Turbo).
 */
public interface CarregamentoTurbo {
    void ativarModoTurbo();
    void desativarModoTurbo();
    boolean isModoTurboAtivo();
}
