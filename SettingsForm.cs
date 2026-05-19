namespace UsbScannerClient;

internal sealed class SettingsForm : Form
{
    private readonly TextBox serverHostTextBox = new();
    private readonly NumericUpDown serverPortNumericUpDown = new();
    private readonly NumericUpDown timeoutNumericUpDown = new();
    private readonly CheckBox autoConnectCheckBox = new();

    public SettingsForm(AppSettings settings)
    {
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
        ClientSize = new Size(430, 220);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 5
        };

        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        serverPortNumericUpDown.Minimum = 1;
        serverPortNumericUpDown.Maximum = 65535;

        timeoutNumericUpDown.Minimum = 500;
        timeoutNumericUpDown.Maximum = 30000;
        timeoutNumericUpDown.Increment = 500;

        AddLabeledControl(rootLayout, "Server", serverHostTextBox, 0);
        AddLabeledControl(rootLayout, "Port", serverPortNumericUpDown, 1);
        AddLabeledControl(rootLayout, "Timeout ms", timeoutNumericUpDown, 2);
        AddLabeledControl(rootLayout, "Startup", autoConnectCheckBox, 3);

        autoConnectCheckBox.Text = "Connect automatically";
        autoConnectCheckBox.Anchor = AnchorStyles.Left;
        autoConnectCheckBox.AutoSize = true;

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
            Width = 82
        };
        okButton.Click += OkButton_Click;

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Text = "Cancel",
            Width = 82
        };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        rootLayout.Controls.Add(buttonPanel, 1, 4);

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

    private void LoadSettingsIntoForm()
    {
        serverHostTextBox.Text = Settings.ServerHost;
        serverPortNumericUpDown.Value = Settings.ServerPort;
        timeoutNumericUpDown.Value = Settings.SendTimeoutMilliseconds;
        autoConnectCheckBox.Checked = Settings.AutoConnect;
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
            AutoConnect = autoConnectCheckBox.Checked
        };
    }
}
