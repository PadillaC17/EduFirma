using System.Security.Cryptography.X509Certificates;
using SimpleSign.Core.Validation;
using SimpleSign.PAdES;
using SimpleSign.PAdES.Signing;
using SimpleSign.PAdES.Validation;

namespace EduFirma.Services;

/// <summary>
/// Motor incremental experimental. Una salida sólo se publica si se preservaron los
/// bytes previos y se comprobó integridad criptográfica de TODAS las firmas.
/// </summary>
public static class IncrementalPdfSigningService
{
    public static async Task<string> SignAsync(
        string pdf, string p12, string password, int page, double x, double y)
    {
        byte[] original = await File.ReadAllBytesAsync(pdf);
        using var certificate = new X509Certificate2(p12, password, X509KeyStorageFlags.EphemeralKeySet);
        if (!certificate.HasPrivateKey) throw new InvalidOperationException("El P12 no contiene clave privada.");
        var validator = new PdfSignatureValidator(new ValidationOptions { CheckRevocation = false });
        async Task<List<(string Field, bool Integrity, bool Cryptographic)>> Inspect(byte[] document)
        {
            using var stream = new MemoryStream(document);
            var results = await validator.ValidateAsync(stream);
            return results.Select(s => (s.FieldName, s.IsIntegrityValid, s.IsSignatureValid)).ToList();
        }

        var before = await Inspect(original);
        if (before.Any(s => !s.Integrity || !s.Cryptographic))
            throw new InvalidOperationException("Hay firmas previas que no superaron la verificación criptográfica. Se cancela.");

        var appearance = new SignatureAppearance
        {
            Page = page,
            X = (float)x,
            Y = (float)y,
            ShowDate = true,
            TextColor = (0.02f, 0.15f, 0.34f)
        };
        byte[] signed = await PadesSigner.Document(original)
            .WithCertificate(certificate)
            .WithAppearance(appearance)
            .SignAsync();

        if (signed.Length <= original.Length || !signed.AsSpan(0, original.Length).SequenceEqual(original))
            throw new InvalidOperationException("La firma incremental modificó el documento de origen.");
        var after = await Inspect(signed);
        if (after.Count != before.Count + 1 || after.Any(s => !s.Integrity || !s.Cryptographic))
            throw new InvalidOperationException("Una de las firmas del documento resultante no superó la verificación.");

        var originalNames = before.Select(s => s.Field).ToHashSet(StringComparer.Ordinal);
        if (before.Any(s => !after.Any(a => a.Field == s.Field)))
            throw new InvalidOperationException("Se perdió algún campo de firma anterior.");

        string folder = Path.GetDirectoryName(pdf)!;
        string name = Path.GetFileNameWithoutExtension(pdf);
        string target = Path.Combine(folder, name + "_EduVirtual_Multifirma.pdf");
        for (int i = 2; File.Exists(target); i++)
            target = Path.Combine(folder, name + "_EduVirtual_Multifirma_" + i + ".pdf");
        string temp = target + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, signed);
            File.Move(temp, target);
            return target;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
