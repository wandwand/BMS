package br.ufc.poo.bms;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Módulo de bateria que agrega por composição um conjunto exclusivo de células.
 */
public class ModuloBateria implements Balanceavel {
    private String nomeModulo;
    private List<CelulaBateria> celulas;

    public ModuloBateria(String nomeModulo) {
        this.nomeModulo = nomeModulo;
        this.celulas = new ArrayList<>();
    }

    public String getNomeModulo() {
        return nomeModulo;
    }

    public void setNomeModulo(String nomeModulo) {
        this.nomeModulo = nomeModulo;
    }

    public List<CelulaBateria> getCelulas() {
        return Collections.unmodifiableList(celulas);
    }

    public void adicionarCelula(CelulaBateria celula) {
        if (celula != null) {
            celulas.add(celula);
        }
    }

    public double calcularTensaoTotal() {
        double total = 0.0;
        for (CelulaBateria celula : celulas) {
            total += celula.getTensaoAtual();
        }
        return total;
    }

    public double calcularTensaoMedia() {
        if (celulas.isEmpty()) {
            return 0.0;
        }
        return calcularTensaoTotal() / celulas.size();
    }

    public double calcularDesbalanceamento() {
        if (celulas.size() < 2) {
            return 0.0;
        }
        double min = Double.MAX_VALUE;
        double max = Double.MIN_VALUE;
        for (CelulaBateria celula : celulas) {
            double v = celula.getTensaoAtual();
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return max - min;
    }

    @Override
    public void executarBalanceamentoPassivo(double tensaoAlvo) {
        for (CelulaBateria celula : celulas) {
            double tensao = celula.getTensaoAtual();
            if (tensao > tensaoAlvo) {
                // Dissipa 50% do excedente por ciclo de balanceamento passivo
                double reducao = (tensao - tensaoAlvo) * 0.5;
                celula.setTensaoAtual(tensao - reducao);
            }
        }
    }
}
