using PdfSharp.Drawing;
using PdfSharp.Pdf.Annotations;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EduFirma.Services;

/// <summary>
/// Dibujos locales de apariencia de firma. El QR NO valida una firma criptográfica.
/// </summary>
public sealed class EduFirmaAppearanceHandler : IAnnotationAppearanceHandler
{
    private readonly string signer;
    private readonly string qrText;
    private readonly string signedAt;
    public const double Width = 248;
    public const double Height = 68;

    public EduFirmaAppearanceHandler(X509Certificate2 certificate, DateTimeOffset time)
    {
        signer = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (string.IsNullOrWhiteSpace(signer)) signer = certificate.Subject;
        signedAt = time.ToString("dd/MM/yyyy HH:mm:ss zzz");
        qrText = "EduVirtual Firmas" + "\n"
            + "Firmante: " + signer + "\n"
            + "Serie: " + certificate.SerialNumber + "\n"
            + "Fecha local declarada: " + signedAt + "\n"
            + "Verificar firma digital en el PDF.";
    }

    public void DrawAppearance(XGraphics gfx, XRect rect)
    {
        gfx.ScaleTransform(0.8, 0.8);
        var navy = XColor.FromArgb(15, 37, 83);
        var blue = XColor.FromArgb(20, 122, 219);
        var light = new XSolidBrush(XColor.FromArgb(245, 250, 255));
        gfx.DrawRectangle(XBrushes.White, 0, 0, 310, 85);
        gfx.DrawRectangle(new XPen(blue, 0.7), light, 0.5, 0.5, 309, 84);
        gfx.DrawLine(new XPen(blue, 0.7), 81, 7, 81, 78);
        gfx.DrawLine(new XPen(blue, 0.7), 224, 7, 224, 78);

        // Renderizamos los módulos del QR como rectángulos vectoriales.
        // Evita el error "Unsupported image format" de XImage.FromStream.
        using (var generator = new QRCodeGenerator())
        using (var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M))
        {
            int modules = data.ModuleMatrix.Count;
            const double qrSize = 70.0;
            double unit = qrSize / modules;
            for (int row = 0; row < modules; row++)
            {
                for (int col = 0; col < modules; col++)
                {
                    if (data.ModuleMatrix[row][col])
                        gfx.DrawRectangle(XBrushes.Black, 7 + col * unit, 7 + row * unit, unit + 0.015, unit + 0.015);
                }
            }
        }

        string[] words = signer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string line1 = signer, line2 = "";
        if (signer.Length > 19 && words.Length >= 2)
        {
            int cut = words.Length / 2;
            line1 = string.Join(" ", words.Take(cut));
            line2 = string.Join(" ", words.Skip(cut));
        }
        var nameFont = new XFont("Arial", line1.Length > 24 || line2.Length > 24 ? 8.0 : 9.4, XFontStyleEx.Bold);
        var smallFont = new XFont("Arial", 7.2, XFontStyleEx.Regular);
        var blueBrush = new XSolidBrush(navy);
        gfx.DrawString(line1, nameFont, blueBrush, new XRect(87, 12, 132, 15), XStringFormats.TopLeft);
        if (line2.Length > 0) gfx.DrawString(line2, nameFont, blueBrush, new XRect(87, 25, 132, 15), XStringFormats.TopLeft);
        gfx.DrawLine(new XPen(blue, 1), 87, 45, 215, 45);
        gfx.DrawString("Firmado electrónicamente", smallFont, blueBrush, new XRect(87, 50, 132, 12), XStringFormats.TopLeft);
        gfx.DrawString(signedAt, smallFont, blueBrush, new XRect(87, 65, 132, 12), XStringFormats.TopLeft);

        // Monograma original vectorial de EduFirmas: pluma sobre libro abierto.
        var emblem = new XSolidBrush(blue);
        gfx.DrawLine(new XPen(navy, 3), 244, 40, 265, 48);
        gfx.DrawLine(new XPen(blue, 3), 265, 48, 284, 40);
        gfx.DrawLine(new XPen(blue, 2), 265, 47, 265, 18);
        gfx.DrawLine(new XPen(navy, 3), 255, 37, 265, 13);
        gfx.DrawLine(new XPen(navy, 3), 275, 37, 265, 13);
        gfx.DrawEllipse(emblem, 262, 10, 6, 6);
        var logoFont = new XFont("Arial", 12, XFontStyleEx.Bold);
        var logoSmall = new XFont("Arial", 5.6, XFontStyleEx.Regular);
        gfx.DrawString("EduVirtual", logoFont, blueBrush, new XRect(228, 51, 79, 16), XStringFormats.TopCenter);
        gfx.DrawString("FIRMAS · ELECTRÓNICA", logoSmall, blueBrush, new XRect(228, 69, 79, 10), XStringFormats.TopCenter);
    }
}
