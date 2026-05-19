using UsbScannerClient.Controls;

namespace UsbScannerClient;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private GroupBox statusGroupBox = null!;
    private TableLayoutPanel statusPanel = null!;
    private Label serverStatusCaptionLabel = null!;
    private ConnectionIndicator serverIndicator = null!;
    private Label serverStatusValueLabel = null!;
    private Button connectButton = null!;
    private Button settingsButton = null!;
    private Label lastBarcodeCaptionLabel = null!;
    private Label lastBarcodeValueLabel = null!;
    private GroupBox scanGroupBox = null!;
    private TableLayoutPanel scanPanel = null!;
    private Label scanInputLabel = null!;
    private TextBox scanInputTextBox = null!;
    private Button sendButton = null!;
    private Button clearLogButton = null!;
    private DataGridView scanLogGrid = null!;
    private DataGridViewTextBoxColumn CapturedAtColumn = null!;
    private DataGridViewTextBoxColumn BarcodeColumn = null!;
    private DataGridViewTextBoxColumn StatusColumn = null!;
    private DataGridViewTextBoxColumn MessageColumn = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        statusGroupBox = new GroupBox();
        statusPanel = new TableLayoutPanel();
        serverStatusCaptionLabel = new Label();
        serverIndicator = new ConnectionIndicator();
        serverStatusValueLabel = new Label();
        connectButton = new Button();
        settingsButton = new Button();
        lastBarcodeCaptionLabel = new Label();
        lastBarcodeValueLabel = new Label();
        scanGroupBox = new GroupBox();
        scanPanel = new TableLayoutPanel();
        scanInputLabel = new Label();
        scanInputTextBox = new TextBox();
        sendButton = new Button();
        clearLogButton = new Button();
        scanLogGrid = new DataGridView();
        CapturedAtColumn = new DataGridViewTextBoxColumn();
        BarcodeColumn = new DataGridViewTextBoxColumn();
        StatusColumn = new DataGridViewTextBoxColumn();
        MessageColumn = new DataGridViewTextBoxColumn();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        rootLayout.SuspendLayout();
        statusGroupBox.SuspendLayout();
        statusPanel.SuspendLayout();
        scanGroupBox.SuspendLayout();
        scanPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)scanLogGrid).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(statusGroupBox, 0, 0);
        rootLayout.Controls.Add(scanGroupBox, 0, 1);
        rootLayout.Controls.Add(scanLogGrid, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(12);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.Size = new Size(1040, 619);
        rootLayout.TabIndex = 0;
        // 
        // statusGroupBox
        // 
        statusGroupBox.Controls.Add(statusPanel);
        statusGroupBox.Dock = DockStyle.Fill;
        statusGroupBox.Location = new Point(15, 15);
        statusGroupBox.Name = "statusGroupBox";
        statusGroupBox.Size = new Size(1010, 72);
        statusGroupBox.TabIndex = 0;
        statusGroupBox.TabStop = false;
        statusGroupBox.Text = "Connection Status";
        // 
        // statusPanel
        // 
        statusPanel.ColumnCount = 7;
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        statusPanel.Controls.Add(serverStatusCaptionLabel, 0, 0);
        statusPanel.Controls.Add(serverIndicator, 1, 0);
        statusPanel.Controls.Add(serverStatusValueLabel, 2, 0);
        statusPanel.Controls.Add(connectButton, 3, 0);
        statusPanel.Controls.Add(settingsButton, 4, 0);
        statusPanel.Controls.Add(lastBarcodeCaptionLabel, 5, 0);
        statusPanel.Controls.Add(lastBarcodeValueLabel, 6, 0);
        statusPanel.Dock = DockStyle.Fill;
        statusPanel.Location = new Point(3, 19);
        statusPanel.Name = "statusPanel";
        statusPanel.Padding = new Padding(10, 10, 10, 10);
        statusPanel.RowCount = 1;
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        statusPanel.Size = new Size(1004, 50);
        statusPanel.TabIndex = 0;
        // 
        // serverStatusCaptionLabel
        // 
        serverStatusCaptionLabel.Anchor = AnchorStyles.Left;
        serverStatusCaptionLabel.AutoSize = true;
        serverStatusCaptionLabel.Location = new Point(13, 17);
        serverStatusCaptionLabel.Name = "serverStatusCaptionLabel";
        serverStatusCaptionLabel.Size = new Size(104, 15);
        serverStatusCaptionLabel.TabIndex = 0;
        serverStatusCaptionLabel.Text = "Server connection";
        // 
        // serverIndicator
        // 
        serverIndicator.Anchor = AnchorStyles.None;
        serverIndicator.Location = new Point(137, 16);
        serverIndicator.MaximumSize = new Size(18, 18);
        serverIndicator.MinimumSize = new Size(18, 18);
        serverIndicator.Name = "serverIndicator";
        serverIndicator.Size = new Size(18, 18);
        serverIndicator.TabIndex = 1;
        // 
        // serverStatusValueLabel
        // 
        serverStatusValueLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        serverStatusValueLabel.AutoEllipsis = true;
        serverStatusValueLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        serverStatusValueLabel.Location = new Point(169, 14);
        serverStatusValueLabel.Name = "serverStatusValueLabel";
        serverStatusValueLabel.Size = new Size(126, 21);
        serverStatusValueLabel.TabIndex = 2;
        serverStatusValueLabel.Text = "Disconnected";
        serverStatusValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // connectButton
        // 
        connectButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        connectButton.Location = new Point(301, 12);
        connectButton.Name = "connectButton";
        connectButton.Size = new Size(92, 25);
        connectButton.TabIndex = 3;
        connectButton.Text = "Connect";
        connectButton.UseVisualStyleBackColor = true;
        connectButton.Click += ConnectButton_Click;
        // 
        // settingsButton
        // 
        settingsButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        settingsButton.Location = new Point(399, 12);
        settingsButton.Name = "settingsButton";
        settingsButton.Size = new Size(92, 25);
        settingsButton.TabIndex = 4;
        settingsButton.Text = "Settings...";
        settingsButton.UseVisualStyleBackColor = true;
        settingsButton.Click += SettingsButton_Click;
        // 
        // lastBarcodeCaptionLabel
        // 
        lastBarcodeCaptionLabel.Anchor = AnchorStyles.Left;
        lastBarcodeCaptionLabel.AutoSize = true;
        lastBarcodeCaptionLabel.Location = new Point(497, 17);
        lastBarcodeCaptionLabel.Name = "lastBarcodeCaptionLabel";
        lastBarcodeCaptionLabel.Size = new Size(73, 15);
        lastBarcodeCaptionLabel.TabIndex = 5;
        lastBarcodeCaptionLabel.Text = "Last barcode";
        // 
        // lastBarcodeValueLabel
        // 
        lastBarcodeValueLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        lastBarcodeValueLabel.AutoEllipsis = true;
        lastBarcodeValueLabel.Font = new Font("Consolas", 10F, FontStyle.Bold);
        lastBarcodeValueLabel.Location = new Point(609, 13);
        lastBarcodeValueLabel.Name = "lastBarcodeValueLabel";
        lastBarcodeValueLabel.Size = new Size(382, 24);
        lastBarcodeValueLabel.TabIndex = 6;
        lastBarcodeValueLabel.Text = "None";
        lastBarcodeValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // scanGroupBox
        // 
        scanGroupBox.Controls.Add(scanPanel);
        scanGroupBox.Dock = DockStyle.Fill;
        scanGroupBox.Location = new Point(15, 93);
        scanGroupBox.Name = "scanGroupBox";
        scanGroupBox.Size = new Size(1010, 76);
        scanGroupBox.TabIndex = 1;
        scanGroupBox.TabStop = false;
        scanGroupBox.Text = "USB Scanner Input";
        // 
        // scanPanel
        // 
        scanPanel.ColumnCount = 4;
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        scanPanel.Controls.Add(scanInputLabel, 0, 0);
        scanPanel.Controls.Add(scanInputTextBox, 1, 0);
        scanPanel.Controls.Add(sendButton, 2, 0);
        scanPanel.Controls.Add(clearLogButton, 3, 0);
        scanPanel.Dock = DockStyle.Fill;
        scanPanel.Location = new Point(3, 19);
        scanPanel.Name = "scanPanel";
        scanPanel.Padding = new Padding(10, 12, 10, 10);
        scanPanel.RowCount = 1;
        scanPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        scanPanel.Size = new Size(1004, 54);
        scanPanel.TabIndex = 0;
        // 
        // scanInputLabel
        // 
        scanInputLabel.Anchor = AnchorStyles.Left;
        scanInputLabel.AutoSize = true;
        scanInputLabel.Location = new Point(13, 19);
        scanInputLabel.Name = "scanInputLabel";
        scanInputLabel.Size = new Size(51, 15);
        scanInputLabel.TabIndex = 0;
        scanInputLabel.Text = "Barcode";
        // 
        // scanInputTextBox
        // 
        scanInputTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        scanInputTextBox.Font = new Font("Consolas", 10F);
        scanInputTextBox.Location = new Point(89, 15);
        scanInputTextBox.Name = "scanInputTextBox";
        scanInputTextBox.Size = new Size(702, 23);
        scanInputTextBox.TabIndex = 1;
        scanInputTextBox.KeyDown += ScanInputTextBox_KeyDown;
        // 
        // sendButton
        // 
        sendButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        sendButton.Location = new Point(797, 15);
        sendButton.Name = "sendButton";
        sendButton.Size = new Size(94, 25);
        sendButton.TabIndex = 2;
        sendButton.Text = "Send";
        sendButton.UseVisualStyleBackColor = true;
        sendButton.Click += SendButton_Click;
        // 
        // clearLogButton
        // 
        clearLogButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        clearLogButton.Location = new Point(897, 15);
        clearLogButton.Name = "clearLogButton";
        clearLogButton.Size = new Size(94, 25);
        clearLogButton.TabIndex = 3;
        clearLogButton.Text = "Clear";
        clearLogButton.UseVisualStyleBackColor = true;
        clearLogButton.Click += ClearLogButton_Click;
        // 
        // scanLogGrid
        // 
        scanLogGrid.AllowUserToAddRows = false;
        scanLogGrid.AllowUserToDeleteRows = false;
        scanLogGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        scanLogGrid.BackgroundColor = SystemColors.Window;
        scanLogGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        scanLogGrid.Columns.AddRange(new DataGridViewColumn[] { CapturedAtColumn, BarcodeColumn, StatusColumn, MessageColumn });
        scanLogGrid.Dock = DockStyle.Fill;
        scanLogGrid.Location = new Point(15, 175);
        scanLogGrid.MultiSelect = false;
        scanLogGrid.Name = "scanLogGrid";
        scanLogGrid.ReadOnly = true;
        scanLogGrid.RowHeadersVisible = false;
        scanLogGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        scanLogGrid.Size = new Size(1010, 429);
        scanLogGrid.TabIndex = 2;
        // 
        // CapturedAtColumn
        // 
        CapturedAtColumn.FillWeight = 120F;
        CapturedAtColumn.HeaderText = "Captured";
        CapturedAtColumn.Name = "CapturedAtColumn";
        CapturedAtColumn.ReadOnly = true;
        // 
        // BarcodeColumn
        // 
        BarcodeColumn.FillWeight = 190F;
        BarcodeColumn.HeaderText = "Barcode";
        BarcodeColumn.Name = "BarcodeColumn";
        BarcodeColumn.ReadOnly = true;
        // 
        // StatusColumn
        // 
        StatusColumn.FillWeight = 76F;
        StatusColumn.HeaderText = "Status";
        StatusColumn.Name = "StatusColumn";
        StatusColumn.ReadOnly = true;
        // 
        // MessageColumn
        // 
        MessageColumn.FillWeight = 240F;
        MessageColumn.HeaderText = "Message";
        MessageColumn.Name = "MessageColumn";
        MessageColumn.ReadOnly = true;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 619);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1040, 22);
        statusStrip.TabIndex = 1;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(39, 17);
        statusLabel.Text = "Ready";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1040, 641);
        Controls.Add(rootLayout);
        Controls.Add(statusStrip);
        MinimumSize = new Size(860, 520);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "USB Scanner Client";
        rootLayout.ResumeLayout(false);
        statusGroupBox.ResumeLayout(false);
        statusPanel.ResumeLayout(false);
        statusPanel.PerformLayout();
        scanGroupBox.ResumeLayout(false);
        scanPanel.ResumeLayout(false);
        scanPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)scanLogGrid).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
