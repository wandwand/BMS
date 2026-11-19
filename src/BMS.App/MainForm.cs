using System.Drawing;
using BMS.Core.Domain;
using BMS.Core.Simulation;

namespace BMS.App;

public sealed class MainForm : Form
{
    private readonly ControladorBMS _bms;
    private readonly Label _lblStatus;
    private readonly DataGridView _gridCelulas;
    private readonly ListBox _listAlertas;
    private readonly Label _lblSensores;

    public MainForm(ControladorBMS bms)
    {
        _bms = bms;
        Text = "BMS UFC + BMSLogic (C# / .NET 10)";
        Size = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 26, 32);
        ForeColor = Color.FromArgb(235, 238, 245);
        Font = new Font("Segoe UI", 9.5f);

        // Top header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.FromArgb(32, 36, 46),
            Padding = new Padding(16, 12, 16, 12)
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        headerPanel.Controls.Add(_lblStatus);

        // Action buttons bar
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(12, 8, 12, 6),
            BackColor = Color.FromArgb(28, 31, 39),
            WrapContents = true
        };

        buttonPanel.Controls.Add(CriarBotao("Varredura & Telemetria", Color.FromArgb(52, 120, 246), (_, _) =>
        {
            _bms.RealizarVarreduraSeguranca();
            AtualizarTela("Varredura de segurança e telemetria concluída.");
        }));

        buttonPanel.Controls.Add(CriarBotao("Simular Carga (60s)", Color.FromArgb(46, 160, 67), (_, _) =>
        {
            _bms.SimularCicloCarga(60.0);
            AtualizarTela("Ciclo de carga (60s) simulado via modelo ESC + Térmico.");
        }));

        buttonPanel.Controls.Add(CriarBotao("Simular Descarga (60s)", Color.FromArgb(210, 130, 35), (_, _) =>
        {
            _bms.SimularCicloDescarga(60.0);
            AtualizarTela("Ciclo de descarga (60s) simulado via modelo ESC + Térmico.");
        }));

        buttonPanel.Controls.Add(CriarBotao("Ciclador CC-CV (NCA 2170)", Color.FromArgb(115, 92, 220), (_, _) =>
        {
            var cycler = new CcCvCycler(1, 3.25, 3.25, 10.0, 4.20, 2.50, 0.1);
            var sim = _bms.ExecutarCiclador("CELL-NCA-01", cycler, dt: 5.0);
            var last = sim.Points[^1];
            AtualizarTela($"CC-CV NCA 2170: {sim.Points.Count} passos ({last.TimeS:F0}s) | V={last.VoltageV:F3}V | SoC={last.SocTrue * 100:F1}% | KF={last.SocKalman * 100:F1}%");
        }));

        buttonPanel.Controls.Add(CriarBotao("Balanceamento Passivo", Color.FromArgb(40, 150, 160), (_, _) =>
        {
            int n = _bms.ExecutarBalanceamentoPassivo();
            AtualizarTela($"Balanceamento passivo executado em {n} módulo(s).");
        }));

        buttonPanel.Controls.Add(CriarBotao("Turbo (Li-Ion)", Color.FromArgb(165, 95, 210), (_, _) =>
        {
            _bms.AtivarModoTurboEmLiIon();
            AtualizarTela("Modo Turbo acionado nas células Li-Ion.");
        }));

        buttonPanel.Controls.Add(CriarBotao("Flutuação (LiFePO4)", Color.FromArgb(65, 135, 195), (_, _) =>
        {
            _bms.AtivarModoFlutuacaoEmLiFePO4();
            AtualizarTela("Modo Flutuação acionado nas células LiFePO4.");
        }));

        buttonPanel.Controls.Add(CriarBotao("Injetar Falha", Color.FromArgb(205, 55, 65), (_, _) =>
        {
            _bms.InjetarFalhaSimulada();
            AtualizarTela("Falha simulada injetada (Sobretensão + Superaquecimento).");
        }));

        // Sensors strip
        _lblSensores = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(16, 4, 16, 4),
            BackColor = Color.FromArgb(30, 33, 42),
            ForeColor = Color.FromArgb(180, 190, 210),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Cells Grid
        _gridCelulas = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.FromArgb(24, 26, 32),
            GridColor = Color.FromArgb(48, 52, 64),
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };
        _gridCelulas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(38, 42, 54);
        _gridCelulas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _gridCelulas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
        _gridCelulas.DefaultCellStyle.BackColor = Color.FromArgb(28, 31, 38);
        _gridCelulas.DefaultCellStyle.ForeColor = Color.FromArgb(230, 234, 242);
        _gridCelulas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 90, 160);

        _gridCelulas.Columns.Add("Id", "Célula");
        _gridCelulas.Columns.Add("Modulo", "Módulo");
        _gridCelulas.Columns.Add("Quimica", "Química");
        _gridCelulas.Columns.Add("Tensao", "Tensão (V)");
        _gridCelulas.Columns.Add("Corrente", "Corrente (A)");
        _gridCelulas.Columns.Add("TempC", "Temp (°C)");
        _gridCelulas.Columns.Add("TempK", "Temp (K)");
        _gridCelulas.Columns.Add("SocReal", "SoC Real (%)");
        _gridCelulas.Columns.Add("SocKalman", "SoC Kalman (%)");
        _gridCelulas.Columns.Add("R0", "R0 (mΩ)");
        _gridCelulas.Columns.Add("Status", "Status");

        // Alert log panel
        var alertPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 190,
            Padding = new Padding(12, 6, 12, 12),
            BackColor = Color.FromArgb(28, 31, 39)
        };
        var lblAlertsTitle = new Label
        {
            Text = "Histórico de Alertas & Eventos (bmslogic + BMS):",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 9.5f)
        };
        _listAlertas = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 22, 28),
            ForeColor = Color.FromArgb(220, 225, 235),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f)
        };
        alertPanel.Controls.Add(_listAlertas);
        alertPanel.Controls.Add(lblAlertsTitle);

        Controls.Add(_gridCelulas);
        Controls.Add(_lblSensores);
        Controls.Add(buttonPanel);
        Controls.Add(headerPanel);
        Controls.Add(alertPanel);

        AtualizarTela("Sistema inicializado com motor ECM + Térmico + Kalman (base bmslogic).");
    }

    private static Button CriarBotao(string texto, Color corFundo, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = texto,
            AutoSize = true,
            Height = 32,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(4, 2, 4, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = corFundo,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += onClick;
        return btn;
    }

    private void AtualizarTela(string? evento = null)
    {
        bool seguro = _bms.VerificarSeguranca();
        double vTotal = _bms.Modulos.Sum(m => m.CalcularTensaoTotal());
        _lblStatus.Text = $"BMS UFC + BMSLogic  |  Status: {(seguro ? "SEGURO" : "ALERTA CRÍTICO")}  |  " +
                          $"Tensão Pack: {vTotal:F3} V  |  Alertas Globais: {AlertaSistema.TotalAlertasGerados}";
        _lblStatus.ForeColor = seguro ? Color.FromArgb(95, 220, 140) : Color.FromArgb(255, 105, 105);

        _lblSensores.Text = string.Join("   |   ", _bms.Sensores.Select(s => s.GerarRelatorioTelemetria()));

        _gridCelulas.Rows.Clear();
        foreach (var mod in _bms.Modulos)
        {
            foreach (var c in mod.Celulas)
            {
                int rowIdx = _gridCelulas.Rows.Add(
                    c.Id,
                    mod.Id,
                    c.Quimica,
                    $"{c.TensaoAtual:F3}",
                    $"{c.CorrenteAtual:F2}",
                    $"{c.TemperaturaCelsius:F1}",
                    $"{c.TemperaturaKelvin:F1}",
                    $"{c.SocPercentual:F1}%",
                    $"{c.SocKalmanPercentual:F1}%",
                    $"{c.R0 * 1000.0:F2}",
                    c.VerificarSeguranca() ? "OK" : "FALHA");

                if (!c.VerificarSeguranca())
                    _gridCelulas.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 115, 115);
            }
        }

        _listAlertas.Items.Clear();
        if (!string.IsNullOrWhiteSpace(evento))
            _listAlertas.Items.Add($"[EVENTO] {evento}");

        foreach (var alerta in _bms.HistoricoAlertas.AsEnumerable().Reverse())
            _listAlertas.Items.Add(alerta.ToString());
    }
}
