using System.Security.Cryptography.X509Certificates;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace EduFirma.Services;

public static class PdfSigningService
{
    // No se guarda el P12 ni la contraseña. Sin sello de tiempo remoto.
    public static string Sign(string pdf, string p12, string password, string reason, string location, int pageNumber, double x = 40, double y = 40)
    {
        if (!File.Exists(pdf) || !File.Exists(p12)) throw new FileNotFoundException("Falta el PDF o el certificado.");
        using var cert = new X509Certificate2(p12, password, X509KeyStorageFlags.EphemeralKeySet);
        if (!cert.HasPrivateKey) throw new InvalidOperationException("El certificado no contiene una clave privada.");
        if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
            throw new InvalidOperationException("El certificado está fuera de su período de validez.");

        // PDFsharp puede reescribir el PDF completo al guardarlo. Evitar firmas previas.
        using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify);
        if (pageNumber < 1 || pageNumber > document.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "La página seleccionada no existe.");

        var options = new DigitalSignatureOptions
        {
            AppName = "EduFirma",
            Reason = reason ?? "",
            Location = location ?? "",
            PageIndex = pageNumber - 1,
            Rectangle = new XRect(x, y, 185, 65)
        };
        DigitalSignatureHandler.ForDocument(document, new PdfSharpDefaultSigner(cert, PdfMessageDigestType.SHA256), options);

        string folder = Path.GetDirectoryName(pdf)!;
        string baseName = Path.GetFileNameWithoutExtension(pdf);
        string output = Path.Combine(folder, baseName + "_EduFirma.pdf");
        for (int n = 2; File.Exists(output); n++)
            output = Path.Combine(folder, baseName + "_EduFirma_" + n + ".pdf");

        try { document.Save(output); return output; }
        catch { if (File.Exists(output)) File.Delete(output); throw; }
    }
}
