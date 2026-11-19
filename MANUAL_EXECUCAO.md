# Manual de Execução — Sistema de Gestão e Telemetria de Baterias (BMS C# + BMSLogic)

Projeto atualizado para **C# / .NET 10**, combinando a arquitetura orientada a objetos do **BMS UFC** com o motor de simulação física e estimação de estado baseado no repositório **bmslogic**.

---

## 1. Visão Geral e Arquitetura

* **Camada de Domínio (`BMS.Core.Domain`):**
  * `ControladorBMS`: cérebro do sistema, gerencia módulos, sensores térmicos, varredura de segurança, injeção de falhas, histórico de alertas (`AlertaSistema`) e execução de cicladores.
  * `ModuloBateria`: agrega células e executa balanceamento passivo (`IBalanceavel`) quando o desbalanceamento ultrapassa `0.05 V`.
  * `CelulaBateria` e químicas:
    * `CelulaLiFePO4` (`2.50 V` – `3.65 V`, máx. `55 °C`, com `IModoFlutuacao`).
    * `CelulaLiIon` (`3.00 V` – `4.20 V`, máx. `45 °C`, com `ICarregamentoTurbo` 2C até 80% SoC ou 40 °C).
    * `CelulaNca2170` (`2.50 V` – `4.20 V`, `6.5 Ah`, baseada no conjunto de parâmetros `tesla_NCA_2170` do `bmslogic`).
  * `SensorTemperatura`: monitora barramentos com limite seguro de `<= 60 °C`.
* **Motor Baseado no `bmslogic` (`BMS.Core.Models`, `Simulation`, `Parameters`, `CalcHelpers`):**
  * **Modelo ECM / ESC (`Thevenin1RCModel`, `EscModel`):** circuito equivalente 1-RC com histerese dinâmica/instantânea e resistências dependentes da temperatura (Arrhenius).
  * **Modelo Térmico (`EcmLumpedThermalModel` + `OdeSolvers.Rk4`):** balanço térmico concentrado integrado via Runge-Kutta de 4ª ordem.
  * **Filtro de Kalman (`SigmaPointKalmanFilter`):** filtro CDKF (*Central-Difference Kalman Filter*) para estimação em tempo real do **SoC** a partir de tensão e corrente.
  * **Cicladores (`CcCycler`, `CcCvCycler`, `ChargeCycler`, `DischargeCycler` + `EscDtSolver`):** execução de ciclos CC e CC-CV.
  * **Parâmetros CSV (`EcmParameterSet`):** carregados de `src/BMS.Core/data/parameter_sets/`.

---

## 2. Pré-requisitos

* **.NET SDK 10.0** (ou superior) no Windows (para interface WinForms).
* Verifique com:
  ```powershell
  dotnet --version
  ```

---

## 3. Compilação, Testes e Execução

1. Na raiz do repositório (`C:\git\BMS`):
   ```powershell
   dotnet build
   ```
2. Executar os testes unitários (`xUnit`):
   ```powershell
   dotnet test
   ```
3. Executar a interface gráfica Desktop (WinForms):
   ```powershell
   dotnet run --project src/BMS.App
   ```
4. Executar no modo menu de console:
   ```powershell
   dotnet run --project src/BMS.App -- --console
   ```

---

## 4. Estrutura de Branches Git

* **`legacy_java`**: preserva integralmente a versão original em Java.
* **`main`**: branch principal com a nova versão em C# (.NET 10 + base `bmslogic`).
* **`dev`**: branch de desenvolvimento contínuo.
