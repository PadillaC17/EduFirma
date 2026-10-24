using System.Security.Cryptography.X509Certificates;
using SimpleSign.Core.Validation;
using SimpleSign.PAdES;
using SimpleSign.PAdES.Validation;

namespace EduVirtualFirmas2;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainWindow());
    }
}

public sealed class MainWindow : Form
{
    private readonly TextBox pdf = new() { ReadOnly = true, Width = 420 };
    private readonly TextBox p12 = new() { ReadOnly = true, Width = 420 };
    private readonly TextBox password = new() { UseSystemPasswordChar = true, Width = 420 };
    private readonly Button sign = new() { Text = "Firmar y conservar firmas", AutoSize = true };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(650, 0), Text = "Motor incremental — versión experimental. No uses documentos oficiales todavía." };

    public MainWindow()
    {
        Text = "EduVirtual Firmas 2 — Motor incremental experimental";
        Width = 760; Height = 375; StartPosition = FormStartPosition.CenterScreen;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(24), AutoScroll = true };
        layout.Controls.Add(new Label { Text = "EduVirtual Firmas 2", Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(7, 65, 145) });
        layout.Controls.Add(new Label { Text = "Documento PDF", AutoSize = true });
        layout.Controls.Add(MakeRow(pdf, () => Pick(pdf, "PDF|*.pdf")));
        layout.Controls.Add(new Label { Text = "Certificado P12 / PFX", AutoSize = true });
        layout.Controls.Add(MakeRow(p12, () => Pick(p12, "Certificado|*.p12;*.pfx")));
        layout.Controls.Add(new Label { Text = "Contraseña del certificado", AutoSize = true });
        layout.Controls.Add(password);
        layout.Controls.Add(sign);
        layout.Controls.Add(status);
        Controls.Add(layout);
        sign.Click += async (_, _) => await SignAsync();
    }
    private static Control MakeRow(TextBox input, Action browse)
    {
        var row = new FlowLayoutPanel { Width = 650, Height = 38, WrapContents = false };
        row.Controls.Add(input);
        var button = new Button { Text = "Examinar", AutoSize = true };
        button.Click += (_, _) => browse();
        row.Controls.Add(button);
        return row;
    }
    private static void Pick(TextBox input, string filter)
    {
        using var picker = new OpenFileDialog { Filter = filter };
        if (picker.ShowDialog() == DialogResult.OK) input.Text = picker.FileName;
    }
    private async Task SignAsync()
    {
        if (!File.Exists(pdf.Text) || !File.Exists(p12.Text) || password.Text.Length == 0)
        { MessageBox.Show("Selecciona PDF, P12 y contraseña."); return; }
        sign.Enabled = false;
        try
        {
            byte[] original = await File.ReadAllBytesAsync(pdf.Text);
            using var cert = new X509Certificate2(p12.Text, password.Text, X509KeyStorageFlags.EphemeralKeySet);
            if (!cert.HasPrivateKey) throw new InvalidOperationException("El certificado carece de clave privada.");
            var validator = new PdfSignatureValidator(new ValidationOptions { CheckRevocation = false });
            var before = await CheckAsync(validator, original);
            if (before.Any(x => !x.Valid)) throw new InvalidOperationException("Se detectó una firma anterior no íntegra. Se cancela.");

            byte[] signed = await PadesSigner.Document(original).WithCertificate(cert).SignAsync();
            if (signed.Length <= original.Length || !signed.AsSpan(0, original.Length).SequenceEqual(original))
                throw new InvalidOperationException("La actualización modificó bytes de una revisión anterior.");

            var after = await CheckAsync(validator, signed);
            if (after.Count != before.Count + 1 || after.Any(x => !x.Valid) || before.Any(b => !after.Any(a => a.Field == b.Field)))
                throw new InvalidOperationException("No se conservaron todas las firmas verificables.");

            string output = Path.Combine(Path.GetDirectoryName(pdf.Text)!, Path.GetFileNameWithoutExtension(pdf.Text) + "_Multifirma.pdf");
            for (int i = 2; File.Exists(output); i++)
                output = Path.Combine(Path.GetDirectoryName(pdf.Text)!, Path.GetFileNameWithoutExtension(pdf.Text) + "_Multifirma_" + i + ".pdf");
            string temp = output + ".tmp";
            try { await File.WriteAllBytesAsync(temp, signed); File.Move(temp, output); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            status.Text = $"Archivo creado. Firmas previas: {before.Count}; ahora: {after.Count}. Revisa también con FirmaEC: {output}";
            MessageBox.Show(status.Text, "Operación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "No se produjo un PDF final: " + ex.Message;
            MessageBox.Show(status.Text, "Firma cancelada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { password.Clear(); sign.Enabled = true; }
    }

    private static async Task<List<(string Field, bool Valid)>> CheckAsync(PdfSignatureValidator validator, byte[] data)
    {
        using var source = new MemoryStream(data);
        var result = await validator.ValidateAsync(source);
        return result.Select(v => (v.FieldName, v.IsIntegrityValid)).ToList();
    }
}
