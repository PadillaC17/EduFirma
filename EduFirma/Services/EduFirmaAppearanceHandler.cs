using PdfSharp.Drawing;
using PdfSharp.Pdf.Annotations;
using QRCoder;
using System.Security.Cryptography.X509Certificates;

namespace EduFirma.Services;

public sealed class EduFirmaAppearanceHandler : IAnnotationAppearanceHandler
{
    // Disposición compacta: nombre | QR | marca; sin rectángulo ni fondo.
    public const double Width = 228;
    public const double Height = 59;
    private readonly string signer;
    private readonly string signedAt;
    private readonly string qrText;
    private static readonly string LogoResource = "EduFirma.Assets.MarcaEduVirtual.png";

    public EduFirmaAppearanceHandler(X509Certificate2 certificate, DateTimeOffset time)
    {
        signer = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (string.IsNullOrWhiteSpace(signer)) signer = certificate.Subject;
        signedAt = time.ToString("dd/MM/yyyy HH:mm:ss");
        string version = typeof(EduFirmaAppearanceHandler).Assembly.GetName().Version?.ToString(3) ?? "desconocida";
        string windows = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
        qrText = "FIRMADO POR: " + signer
            + "\nRAZON: No especificada"
            + "\nLOCALIZACION: No especificada"
            + "\nFECHA: " + time.ToString("yyyy-MM-ddTHH:mm:sszzz")
            + "\nVERIFICAR CON: Aplicacion compatible con firmas PDF"
            + "\nFirmado digitalmente con EduVirtual Firmas v" + version + " - " + windows;
    }

    public void DrawAppearance(XGraphics gfx, XRect rect)
    {
        var navy = XColor.FromArgb(7, 32, 79);
        var cyan = XColor.FromArgb(18, 164, 234);
        var textBrush = new XSolidBrush(navy);
        var dateBrush = new XSolidBrush(XColor.FromArgb(36, 42, 54));
        // NO pintar fondo ni rectángulo exterior: el contenido PDF permanece intacto.
        var words = signer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string line1 = signer, line2 = "";
        if (signer.Length > 20 && words.Length >= 2)
        {
            int half = words.Length / 2;
            line1 = string.Join(" ", words.Take(half));
            line2 = string.Join(" ", words.Skip(half));
        }
        var font = new XFont("Arial", 7.1, XFontStyleEx.Bold);
        var small = new XFont("Arial", 6.1, XFontStyleEx.Regular);
        gfx.DrawString(line1, font, textBrush, new XRect(2, 6, 91, 12), XStringFormats.TopLeft);
        if (!string.IsNullOrEmpty(line2))
            gfx.DrawString(line2, font, textBrush, new XRect(2, 17, 91, 12), XStringFormats.TopLeft);
        gfx.DrawLine(new XPen(cyan, 0.7), 2, 32, 93, 32);
        gfx.DrawString("Firmado electrónicamente", small, dateBrush, new XRect(2, 35, 91, 10), XStringFormats.TopLeft);
        gfx.DrawString(signedAt, small, dateBrush, new XRect(2, 46, 91, 10), XStringFormats.TopLeft);

        // QR vectorial, con quiet zone blanca solo dentro del propio QR.
        using (var gen = new QRCodeGenerator())
        using (var data = gen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M))
        {
            int n = data.ModuleMatrix.Count;
            const double cellArea = 47;
            double m = cellArea / n;
            gfx.DrawRectangle(XBrushes.White, 99, 6, 49, 49);
            for (int row = 0; row < n; row++)
                for (int col = 0; col < n; col++)
                    if (data.ModuleMatrix[row][col])
                        gfx.DrawRectangle(XBrushes.Black, 100 + col * m, 7 + row * m, m + 0.01, m + 0.01);
        }

        gfx.DrawLine(new XPen(navy, 0.7), 153, 6, 153, 53);
        // Incorporar exactamente la imagen del usuario como recurso del ensamblado.
        var names = new[] { "MarcaEduVirtual.png", "LogoEduVirtual.png", "MarcaEduVirtual.jpg" };
        using var embedded = typeof(EduFirmaAppearanceHandler).Assembly.GetManifestResourceStream(LogoResource);
        string? external = names.Select(n => Path.Combine(AppContext.BaseDirectory, "Assets", n)).FirstOrDefault(File.Exists);
        if (embedded == null && external == null)
            throw new InvalidOperationException("Falta el logo. Coloca MarcaEduVirtual.png en la carpeta Assets que está junto a EduFirma.exe y vuelve a abrir la aplicación.");
        using var source = embedded ?? File.OpenRead(external!);
        // Conversión a JPEG en memoria: PDFsharp reconoce esta imagen sin depender del formato PNG.
        using var bitmap = new System.Drawing.Bitmap(source);
        using var converted = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height);
        using (var canvas = System.Drawing.Graphics.FromImage(converted))
        {
            canvas.Clear(System.Drawing.Color.White);
            canvas.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        }
        using var encoded = new MemoryStream();
        converted.Save(encoded, System.Drawing.Imaging.ImageFormat.Jpeg);
        encoded.Position = 0;
        using var logo = XImage.FromStream(encoded);
        gfx.DrawImage(logo, 159, 3, 66, 54);
    }
}
