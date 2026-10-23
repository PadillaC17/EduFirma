using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace EduFirma;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private const string Version = "0.1.0";
    private const string Repo = "PadillaC17/EduFirma";
    private readonly TextBox pdfPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly TextBox p12Path = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly TextBox password = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    private readonly TextBox certificateInfo = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Label status = new() { Text = "Preparado. La firma criptográfica PDF se implementará en una versión posterior.", AutoSize = true };
    private readonly Button updateButton = new() { Text = "Buscar actualizaciones", AutoSize = true };

    public MainForm()
    {
        Text = "EduFirma — v" + Version;
        Width = 790;
        Height = 550;
        MinimumSize = new Size(660, 470);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 8 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 51));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        root.Controls.Add(new Label { Text = "EduFirma  |  Firma electrónica local para Windows", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
        root.Controls.Add(FileRow("Documento PDF", pdfPath, () => SelectFile(pdfPath, "PDF (*.pdf)|*.pdf")), 0, 1);
        root.Controls.Add(FileRow("Certificado P12 / PFX", p12Path, () => SelectFile(p12Path, "Certificados (*.p12;*.pfx)|*.p12;*.pfx")), 0, 2);
        var pwdRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        pwdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        pwdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pwdRow.Controls.Add(new Label { Text = "Contraseña del certificado", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        pwdRow.Controls.Add(password, 1, 0);
        root.Controls.Add(pwdRow, 0, 3);

        var inspect = new Button { Text = "Inspeccionar certificado", AutoSize = true };
        inspect.Click += (_, _) => InspectCertificate();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(inspect);
        buttons.Controls.Add(updateButton);
        root.Controls.Add(buttons, 0, 4);
        root.Controls.Add(certificateInfo, 0, 5);
        root.Controls.Add(new Label { Text = "Firma de PDF: aún no disponible. Esta versión NO altera ni firma el PDF.", AutoSize = true, ForeColor = Color.DarkRed, Anchor = AnchorStyles.Left }, 0, 6);
        root.Controls.Add(status, 0, 7);
        Controls.Add(root);
        updateButton.Click += async (_, _) => await CheckUpdatesAsync();
    }

    private static Control FileRow(string name, TextBox field, Action select)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        row.Controls.Add(new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        row.Controls.Add(field, 1, 0);
        var button = new Button { Text = "Examinar", Dock = DockStyle.Fill };
        button.Click += (_, _) => select();
        row.Controls.Add(button, 2, 0);
        return row;
    }

    private void SelectFile(TextBox field, string filter)
    {
        using var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) field.Text = dialog.FileName;
    }

    private void InspectCertificate()
    {
        if (!File.Exists(p12Path.Text))
        {
            MessageBox.Show(this, "Selecciona primero un archivo P12/PFX.", "EduFirma");
            return;
        }
        try
        {
            using var cert = new X509Certificate2(p12Path.Text, password.Text, X509KeyStorageFlags.EphemeralKeySet);
            certificateInfo.Text = "Titular: " + cert.Subject + Environment.NewLine
                + "Emisor: " + cert.Issuer + Environment.NewLine
                + "Válido desde: " + cert.NotBefore + Environment.NewLine
                + "Válido hasta: " + cert.NotAfter + Environment.NewLine
                + "Clave privada: " + (cert.HasPrivateKey ? "Sí" : "No");
            status.Text = "Certificado leído localmente; no se ha enviado a Internet.";
        }
        catch (Exception)
        {
            certificateInfo.Clear();
            MessageBox.Show(this, "No se pudo abrir el certificado. Comprueba el archivo y la contraseña.", "EduFirma", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            password.Clear();
        }
    }

    private async Task CheckUpdatesAsync()
    {
        updateButton.Enabled = false;
        status.Text = "Buscando versiones en GitHub...";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EduFirma", Version));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await client.GetAsync("https://api.github.com/repos/" + Repo + "/releases/latest");
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                MessageBox.Show(this,
                    "No se pudo consultar la última versión. El repositorio es privado, no hay un Release publicado o GitHub limita la consulta anónima.\n\nEduFirma no pide ni guarda tu token de GitHub. Puedes abrir la página de versiones desde tu navegador.",
                    "Actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenReleasesPage();
                status.Text = "Comprobación no disponible sin acceso público al Release.";
                return;
            }
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var tag = document.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
            if (!System.Version.TryParse(tag, out var latest))
            {
                MessageBox.Show(this, "GitHub publicó una versión con formato desconocido: " + tag, "Actualizaciones");
                return;
            }
            var current = new System.Version(Version);
            if (latest > current)
            {
                if (MessageBox.Show(this, $"Nueva versión: {latest}\nVersión instalada: {current}\n\n¿Abrir las versiones oficiales de GitHub?", "Actualización disponible", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    OpenReleasesPage();
                status.Text = "Nueva versión disponible: " + latest;
            }
            else
            {
                status.Text = "Tienes la última versión publicada.";
                MessageBox.Show(this, "EduFirma está actualizado (v" + Version + ").", "Actualizaciones");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            status.Text = "No fue posible verificar las actualizaciones.";
            MessageBox.Show(this, "No se pudo consultar GitHub. Revisa tu conexión y vuelve a intentarlo.", "EduFirma");
        }
        finally { updateButton.Enabled = true; }
    }

    private static void OpenReleasesPage()
    {
        Process.Start(new ProcessStartInfo("https://github.com/" + Repo + "/releases") { UseShellExecute = true });
    }
}
