using UsbScannerClient.Services;

namespace UsbScannerClient;

internal sealed class UpdateProgressForm : Form
{
    private readonly Label statusLabel = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label progressLabel = new();

    public UpdateProgressForm()
    {
        InitializeComponent();
    }

    public void ReportProgress(UpdateDownloadProgress progress)
    {
        if (progress.Percent.HasValue)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = Math.Clamp(progress.Percent.Value, progressBar.Minimum, progressBar.Maximum);
        }
        else
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }

        progressLabel.Text = progress.TotalBytes.HasValue
            ? $"{FormatBytes(progress.BytesReceived)} / {FormatBytes(progress.TotalBytes.Value)}"
            : FormatBytes(progress.BytesReceived);
    }

    public void SetStatus(string status)
    {
        statusLabel.Text = status;
    }

    private void InitializeComponent()
    {
        Text = "Downloading Update";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10F);
        ClientSize = new Size(450, 140);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 3,
            ColumnCount = 1
        };

        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Text = "Preparing download...";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        progressBar.Dock = DockStyle.Fill;
        progressBar.Minimum = 0;
        progressBar.Maximum = 100;

        progressLabel.Dock = DockStyle.Fill;
        progressLabel.Text = "0 B";
        progressLabel.TextAlign = ContentAlignment.MiddleRight;

        rootLayout.Controls.Add(statusLabel, 0, 0);
        rootLayout.Controls.Add(progressBar, 0, 1);
        rootLayout.Controls.Add(progressLabel, 0, 2);

        Controls.Add(rootLayout);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.0} {units[unitIndex]}";
    }
}
