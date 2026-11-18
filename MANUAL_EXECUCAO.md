# Manual de Execução — Sistema de Gestão e Telemetria de Baterias (BMS)

Projeto desenvolvido em **Java puro** (sem frameworks externos) para a disciplina **SMD0017 - Programação Orientada a Objetos (UFC - IUVI)**.

---

## 1. Visão Geral e Funcionamento do BMS (Alto Nível)

O **BMS (*Battery Management System*)** tem como objetivo monitorar, proteger e otimizar o funcionamento de um banco de baterias composto por diferentes químicas celulares e sensores térmicos.

### Arquitetura Conceitual
* **Controlador Central (`ControladorBMS`):** atua como o cérebro do sistema. Ele agrega os módulos de bateria e os sensores térmicos, executa varreduras periódicas de segurança e gera relatórios de telemetria (`Monitoravel`).
* **Módulos e Células (`ModuloBateria` e `CelulaBateria`):** cada módulo reúne um conjunto exclusivo de células (composição). Todas as células compartilham atributos e operações comuns (`tensaoAtual`, `correnteAtual`, `temperaturaCelsius`, `simularCarga`, `simularDescarga`), mas cada química implementa suas próprias regras de cálculo de carga (**SoC**) e segurança:
  * **`CelulaLiFePO4`:** opera entre `2.50 V` e `3.65 V` (máx. `55 °C`) e suporta **`ModoFlutuacao`**, entrando em manutenção (`3.45 V` / `0.5 A`) ao atingir carga cheia (`>= 95%` SoC) para evitar sobrecarga.
  * **`CelulaLiIon`:** opera entre `3.00 V` e `4.20 V` (máx. `45 °C`) e suporta **`CarregamentoTurbo`**, carregando em velocidade `2x` até `80%` de SoC ou `40 °C`, desativando-se automaticamente por proteção térmica/CC-CV.
* **Sensores Térmicos (`SensorTemperatura`):** monitoram a temperatura física dos barramentos e validam se estão dentro da faixa segura (`<= 60 °C`).

### Fluxo de Operação
1. **Ciclos de Carga e Descarga:** ao simular carga ou descarga, tensão, corrente e temperatura variam conforme a química da célula. Se uma célula atingir `0.0 V`, novas descargas são bloqueadas para não alterar temperatura ou corrente indevidamente.
2. **Varredura e Autodiagnóstico:** o controlador executa `realizarAutodiagnostico()` e `verificarSeguranca()` de forma polimórfica em todos os componentes. Qualquer violação (sobretensão, superaquecimento ou desativação de turbo) gera um `AlertaSistema` no histórico e incrementa o contador global estático.
3. **Balanceamento Passivo:** se a diferença de tensão entre as células de um mesmo módulo ultrapassar `0.05 V`, o controlador aciona `Balanceavel` para dissipar parte do excesso das células acima da média e equalizar o módulo.

---

## 2. Pré-requisitos

* **JDK (Java Development Kit)** versão **17 ou superior** (testado com OpenJDK 21).
* Verifique se `javac` e `java` estão disponíveis no terminal:
  ```bash
  javac -version
  java -version
  ```

---

## 3. Compilação e Execução via Terminal

1. Abra o terminal (**PowerShell**, **CMD** ou **Bash**) na raiz do projeto (`C:\git\BMS`):
   ```bash
   cd C:\git\BMS
   ```

2. Compile todos os arquivos-fonte para a pasta `out/`:
   ```bash
   javac -encoding UTF-8 -d out src/br/ufc/poo/bms/*.java
   ```
   *(No Windows PowerShell, o comando acima funciona diretamente; caso prefira listar explicitamente em sistemas sem globbing, utilize `javac -encoding UTF-8 -d out (Get-ChildItem -Recurse -Filter *.java src).FullName`.)*

3. Execute a aplicação (interface gráfica Swing por padrão):
   ```bash
   java -cp out br.ufc.poo.bms.Main
   ```

   *(Opcional)* Para executar no modo menu de console:
   ```bash
   java -cp out br.ufc.poo.bms.Main --console
   ```

4. Na interface gráfica (**BMS UFC**) ou no menu do console, você pode:
   * **Varredura e Telemetria / Opção 1:** Visualizar telemetria completa (tensão, corrente, temperatura em °C e K, SoC).
   * **Simular Carga / Opção 2:** Simular ciclo de carga (+120 mV por célula).
   * **Simular Descarga / Opção 3:** Simular ciclo de descarga (-150 mV por célula).
   * **Injeção de Falhas / Opção 4:** Injetar falha simulada (sobretensão em célula LiFePO4 ou superaquecimento em sensor).
   * **Balanceamento Passivo / Opção 5:** Acionar balanceamento passivo nos módulos.
   * **Histórico de Alertas / Opção 6:** Exibir histórico de alertas e o total global de alertas gerados (`AlertaSistema.getTotalAlertasGerados()`).
   * **Capacidades por Tipo / Opção 7:** Ativar **Modo Turbo** nas células `Li-Ion` (2x velocidade de carga com proteção térmica/SoC) e **Modo Flutuação** nas células `LiFePO4` (manutenção segura em carga cheia).

---

## 4. Como Renderizar o Diagrama de Classes (`diagrama_classes.puml`)

O arquivo `diagrama_classes.puml` está escrito em **PlantUML** e pode ser visualizado ou exportado para PNG/SVG de três formas:

1. **PlantUML Online (sem instalação):**
   * Acesse [https://www.plantuml.com/plantuml/uml/](https://www.plantuml.com/plantuml/uml/) e cole o conteúdo de `diagrama_classes.puml`.
2. **Extensão no VS Code:**
   * Instale a extensão **PlantUML** (por *jebbs*), abra `diagrama_classes.puml` e pressione `Alt + D` para pré-visualizar ou use o comando *PlantUML: Export Current Diagram*.
3. **Via CLI do PlantUML (se instalado localmente com Graphviz):**
   ```bash
   plantuml -tpng diagrama_classes.puml
   ```
