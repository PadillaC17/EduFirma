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
        // PDFsharp Core 6.2 needs an explicit resolver for Windows fonts.
        PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private const string Version = "0.5.2";
    private const string Repo = "PadillaC17/EduFirma";
    private readonly ListBox files = new() { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, AllowDrop = true };
    private readonly TextBox p12Path = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly TextBox password = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    private readonly TextBox certInfo = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly Label status = new() { Text = "Arrastra documentos PDF o pulsa Agregar documentos.", Dock = DockStyle.Fill };
    private readonly Button updateButton = new() { Text = "Buscar actualizaciones", AutoSize = true };
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly Color accent = Color.FromArgb(87, 84, 174);

    public MainForm()
    {
        Text = "EduFirma — v" + Version;
        Width = 940; Height = 680; MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;

        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(13) };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        top.Controls.Add(new Label { Text = "EduFirma", Font = new Font("Segoe UI", 22, FontStyle.Bold), Width = 180, Height = 45 });
        top.Controls.Add(new Label { Text = "Firma electrónica local  |  v" + Version, ForeColor = Color.DimGray, Width = 285, Height = 43, TextAlign = ContentAlignment.MiddleLeft });
        top.Controls.Add(updateButton);
        updateButton.Click += async (_, _) => await CheckUpdatesAsync();
        shell.Controls.Add(top, 0, 0);

        tabs.TabPages.Add(CreateSignTab());
        tabs.TabPages.Add(CreateVerifyTab());
        tabs.TabPages.Add(CreateCertificateTab());
        shell.Controls.Add(tabs, 0, 1);
        status.ForeColor = Color.DarkSlateGray;
        shell.Controls.Add(status, 0, 2);
        Controls.Add(shell);

        files.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        files.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] incoming) AddDocuments(incoming);
        };
    }

    private TabPage CreateSignTab()
    {
        var tab = new TabPage("Firmar documentos");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 4, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var p12Row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        p12Row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        p12Row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        p12Row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        p12Row.Controls.Add(new Label { Text = "Certificado (.p12/.pfx)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        p12Row.Controls.Add(p12Path, 1, 0);
        p12Row.Controls.Add(MakeButton("Elegir certificado", () => SelectCert()), 2, 0);
        layout.Controls.Add(p12Row, 0, 0);

        var pwRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        pwRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        pwRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pwRow.Controls.Add(new Label { Text = "Contraseña", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        pwRow.Controls.Add(password, 1, 0);
        layout.Controls.Add(pwRow, 0, 1);

        var filePanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        filePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        filePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill };
        tools.Controls.Add(MakeButton("Agregar PDF", SelectPdfs));
        tools.Controls.Add(MakeButton("Quitar seleccionado", () => { foreach (var item in files.SelectedItems.Cast<object>().ToArray()) files.Items.Remove(item); }));
        tools.Controls.Add(MakeButton("Abrir PDF", OpenSelectedPdf));
        tools.Controls.Add(new Label { Text = "Arrastra PDF a la lista", AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(10, 8, 0, 0) });
        filePanel.Controls.Add(tools, 0, 0);
        filePanel.Controls.Add(files, 0, 1);
        layout.Controls.Add(filePanel, 0, 2);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var signButton = MakeButton("Firmar PDF con P12", SignDocuments);
        signButton.BackColor = accent; signButton.ForeColor = Color.White;
        footer.Controls.Add(signButton);
        layout.Controls.Add(footer, 0, 3);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage CreateVerifyTab()
    {
        var tab = new TabPage("Verificar documento");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Verificación de PDF firmados", Font = new Font("Segoe UI", 15, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(new Label { Text = "Esta función aún no valida firmas, integridad ni revocación. Se incorporará con el motor de firmas PDF. Puedes abrir el PDF con tu lector habitual para verificarlo mientras tanto.", Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(MakeButton("Abrir PDF en el lector de Windows", () => OpenPdfDialog()), 0, 2);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage CreateCertificateTab()
    {
        var tab = new TabPage("Validar certificado");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Información del certificado local", Font = new Font("Segoe UI", 15, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(MakeButton("Inspeccionar certificado P12", InspectCertificate), 0, 1);
        layout.Controls.Add(certInfo, 0, 2);
        tab.Controls.Add(layout);
        return tab;
    }

    private static Button MakeButton(string title, Action action)
    {
        var b = new Button { Text = title, AutoSize = true, Padding = new Padding(5), MinimumSize = new Size(100, 35) };
        b.Click += (_, _) => action();
        return b;
    }

    private void SignDocuments()
    {
        if (files.Items.Count == 0 || !File.Exists(p12Path.Text) || string.IsNullOrEmpty(password.Text))
        {
            MessageBox.Show(this, "Agrega PDF, selecciona el P12 e introduce la contraseña.", "EduFirma");
            return;
        }
        var succeeded = new List<string>();
        var failed = new List<string>();
        try
        {
            foreach (string pdf in files.Items)
            {
                try
                {
                    using var dialog = new EduFirma.Forms.PdfPlacementForm(pdf, 1);
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        status.Text = "Operación cancelada; no se firmaron los documentos restantes.";
                        break;
                    }
                    if (MessageBox.Show(this, "Se creará una copia _EduFirma.pdf con firma experimental. No utilices un PDF que ya esté firmado; comprueba el resultado con un verificador independiente. ¿Firmar ahora?", "Confirmar firma", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        status.Text = "Firma cancelada.";
                        break;
                    }
                    var result = EduFirma.Services.PdfSigningService.Sign(pdf, p12Path.Text, password.Text, "", "", dialog.SelectedPage, dialog.SelectedX, dialog.SelectedY);
                    succeeded.Add(result);
                }
                catch (Exception ex) { failed.Add(Path.GetFileName(pdf) + ": " + ex.Message); }
            }
        }
        finally { password.Clear(); }
        var report = "PDF generados: " + succeeded.Count + Environment.NewLine +
            string.Join(Environment.NewLine, succeeded.Select(Path.GetFileName));
        if (failed.Count > 0) report += Environment.NewLine + Environment.NewLine + "Fallos:" + Environment.NewLine + string.Join(Environment.NewLine, failed);
        MessageBox.Show(this, report, "EduFirma — resultado", MessageBoxButtons.OK,
            failed.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        status.Text = $"Firmas procesadas: {succeeded.Count} correctas, {failed.Count} fallidas (verifica los PDF).";
    }

    private void SelectCert()
    {
        using var d = new OpenFileDialog { Filter = "Certificados PKCS#12 (*.p12;*.pfx)|*.p12;*.pfx" };
        if (d.ShowDialog(this) == DialogResult.OK) p12Path.Text = d.FileName;
    }

    private void SelectPdfs()
    {
        using var d = new OpenFileDialog { Filter = "Documentos PDF (*.pdf)|*.pdf", Multiselect = true };
        if (d.ShowDialog(this) == DialogResult.OK) AddDocuments(d.FileNames);
    }

    private void AddDocuments(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) continue;
            if (!files.Items.Cast<string>().Contains(path, StringComparer.OrdinalIgnoreCase)) files.Items.Add(path);
        }
        status.Text = $"{files.Items.Count} documentos PDF seleccionados. Los archivos permanecen en tu equipo.";
    }

    private void OpenSelectedPdf()
    {
        if (files.SelectedItem is string path && File.Exists(path)) OpenFile(path);
        else MessageBox.Show(this, "Selecciona un PDF de la lista.", "EduFirma");
    }

    private void OpenPdfDialog()
    {
        using var d = new OpenFileDialog { Filter = "Documentos PDF (*.pdf)|*.pdf" };
        if (d.ShowDialog(this) == DialogResult.OK) OpenFile(d.FileName);
    }

    private static void OpenFile(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    private void InspectCertificate()
    {
        if (!File.Exists(p12Path.Text))
        {
            MessageBox.Show(this, "Primero selecciona el certificado en la pestaña Firmar documentos.", "EduFirma");
            tabs.SelectedIndex = 0; return;
        }
        try
        {
            using var cert = new X509Certificate2(p12Path.Text, password.Text, X509KeyStorageFlags.EphemeralKeySet);
            certInfo.Text = $"Titular: {cert.Subject}\r\nEmisor: {cert.Issuer}\r\nVigente desde: {cert.NotBefore}\r\nVigente hasta: {cert.NotAfter}\r\nClave privada disponible: {(cert.HasPrivateKey ? "Sí" : "No")}\r\nFecha actual dentro del periodo: {(DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter ? "Sí" : "No")}\r\n\r\nAdvertencia: no se ha comprobado la revocación ni la cadena de confianza.";
            status.Text = "Inspección local completada. Contraseña borrada del formulario.";
            tabs.SelectedIndex = 2;
        }
        catch (Exception)
        {
            certInfo.Clear();
            MessageBox.Show(this, "No se pudo abrir el P12. Comprueba el archivo y su contraseña.", "EduFirma", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { password.Clear(); }
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
                MessageBox.Show(this, "El repositorio es privado o aún no tiene un Release. Se abrirá la página de versiones en tu navegador. No se solicita un token.", "Actualizaciones");
                OpenReleases();
                status.Text = "Revisión disponible desde el navegador con tu sesión de GitHub.";
                return;
            }
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var tag = json.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
            if (!System.Version.TryParse(tag, out var latest))
            {
                MessageBox.Show(this, "Etiqueta de versión no reconocida: " + tag, "Actualizaciones");
                return;
            }
            if (latest > new System.Version(Version))
            {
                if (MessageBox.Show(this, $"Hay una nueva versión: {latest}. ¿Abrir GitHub Releases?", "EduFirma", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    OpenReleases();
                status.Text = "Nueva versión disponible: " + latest;
            }
            else
            {
                MessageBox.Show(this, "Ya tienes la última versión publicada.", "EduFirma");
                status.Text = "EduFirma actualizado.";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            status.Text = "No fue posible comprobar actualizaciones.";
            MessageBox.Show(this, "No fue posible acceder a GitHub. Comprueba tu conexión.", "EduFirma");
        }
        finally { updateButton.Enabled = true; }
    }

    private static void OpenReleases() => Process.Start(new ProcessStartInfo("https://github.com/" + Repo + "/releases") { UseShellExecute = true });
}
