package br.ufc.poo.bms;

/**
 * Contrato para componentes ou módulos que suportam balanceamento passivo de tensão.
 */
public interface Balanceavel {
    void executarBalanceamentoPassivo(double tensaoAlvo);
}
