namespace UsbScannerClient;

internal sealed class SettingsForm : Form
{
    private readonly Func<IWin32Window, Task>? checkForUpdatesAsync;
    private readonly TextBox serverHostTextBox = new();
    private readonly NumericUpDown serverPortNumericUpDown = new();
    private readonly NumericUpDown timeoutNumericUpDown = new();
    private readonly NumericUpDown scanIdleTimeoutNumericUpDown = new();
    private readonly CheckBox autoConnectCheckBox = new();
    private readonly CheckBox autoUpdateCheckBox = new();
    private readonly Button checkForUpdatesButton = new();

    public SettingsForm(
        AppSettings settings,
        Func<IWin32Window, Task>? checkForUpdatesAsync = null)
    {
        this.checkForUpdatesAsync = checkForUpdatesAsync;
        Settings = settings.Copy();
        InitializeComponent();
        LoadSettingsIntoForm();
    }

    public AppSettings Settings { get; private set; }

    private void InitializeComponent()
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10F);
        ClientSize = new Size(570, 340);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 7
        };

        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        serverPortNumericUpDown.Minimum = 1;
        serverPortNumericUpDown.Maximum = 65535;

        timeoutNumericUpDown.Minimum = 500;
        timeoutNumericUpDown.Maximum = 30000;
        timeoutNumericUpDown.Increment = 500;

        scanIdleTimeoutNumericUpDown.Minimum = 100;
        scanIdleTimeoutNumericUpDown.Maximum = 2000;
        scanIdleTimeoutNumericUpDown.Increment = 50;

        AddLabeledControl(rootLayout, "Server", serverHostTextBox, 0);
        AddLabeledControl(rootLayout, "Port", serverPortNumericUpDown, 1);
        AddLabeledControl(rootLayout, "Timeout ms", timeoutNumericUpDown, 2);
        AddLabeledControl(rootLayout, "Scan idle ms", scanIdleTimeoutNumericUpDown, 3);
        AddLabeledControl(rootLayout, "Startup", autoConnectCheckBox, 4);
        AddLabeledControl(rootLayout, "Updates", CreateUpdatesPanel(), 5);

        autoConnectCheckBox.Text = "Connect automatically";
        autoConnectCheckBox.Anchor = AnchorStyles.Left;
        autoConnectCheckBox.AutoSize = true;

        autoUpdateCheckBox.Text = "Check for updates automatically";
        autoUpdateCheckBox.Anchor = AnchorStyles.Left;
        autoUpdateCheckBox.AutoSize = true;

        var buttonPanel = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };

        var okButton = new Button
        {
            DialogResult = DialogResult.OK,
            Text = "OK",
            Height = 32,
            Width = 82
        };
        okButton.Click += OkButton_Click;

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Text = "Cancel",
            Height = 32,
            Width = 82
        };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        rootLayout.Controls.Add(buttonPanel, 1, 6);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(rootLayout);
    }

    private static void AddLabeledControl(
        TableLayoutPanel layout,
        string labelText,
        Control control,
        int row)
    {
        var label = new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Text = labelText
        };

        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private Control CreateUpdatesPanel()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        checkForUpdatesButton.Text = "Check now";
        checkForUpdatesButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        checkForUpdatesButton.Enabled = checkForUpdatesAsync is not null;
        checkForUpdatesButton.Height = 32;
        checkForUpdatesButton.Click += CheckForUpdatesButton_Click;

        panel.Controls.Add(autoUpdateCheckBox, 0, 0);
        panel.Controls.Add(checkForUpdatesButton, 1, 0);

        return panel;
    }

    private void LoadSettingsIntoForm()
    {
        serverHostTextBox.Text = Settings.ServerHost;
        serverPortNumericUpDown.Value = Settings.ServerPort;
        timeoutNumericUpDown.Value = Settings.SendTimeoutMilliseconds;
        scanIdleTimeoutNumericUpDown.Value = Settings.ScanIdleTimeoutMilliseconds;
        autoConnectCheckBox.Checked = Settings.AutoConnect;
        autoUpdateCheckBox.Checked = Settings.AutoUpdate;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        string serverHost = serverHostTextBox.Text.Trim();
        if (serverHost.Length == 0)
        {
            MessageBox.Show(
                this,
                "Server is required.",
                "Invalid Settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            serverHostTextBox.Focus();
            return;
        }

        Settings = new AppSettings
        {
            ServerHost = serverHost,
            ServerPort = decimal.ToInt32(serverPortNumericUpDown.Value),
            SendTimeoutMilliseconds = decimal.ToInt32(timeoutNumericUpDown.Value),
            ScanIdleTimeoutMilliseconds = decimal.ToInt32(scanIdleTimeoutNumericUpDown.Value),
            AutoConnect = autoConnectCheckBox.Checked,
            AutoUpdate = autoUpdateCheckBox.Checked
        };
    }

    private async void CheckForUpdatesButton_Click(object? sender, EventArgs e)
    {
        if (checkForUpdatesAsync is null)
        {
            return;
        }

        checkForUpdatesButton.Enabled = false;
        try
        {
            await checkForUpdatesAsync(this);
        }
        finally
        {
            if (!IsDisposed)
            {
                checkForUpdatesButton.Enabled = true;
            }
        }
    }
}
