using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SimpleSign.PAdES;
using SimpleSign.Core.Validation;
using SimpleSign.PAdES.Validation;

// Prueba aislada: NUNCA utiliza certificados reales del usuario.
// El certificado de prueba es temporal y se genera en memoria.
using RSA rsa = RSA.Create(2048);
var request = new CertificateRequest("CN=EduVirtual Multifirma Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
byte[] input = CreateMinimalPdf();
byte[] first = await PadesSigner.Document(input).WithCertificate(certificate).SignAsync();
byte[] second = await PadesSigner.Document(first).WithCertificate(certificate).SignAsync();

// Invariante: la revisión completa original debe permanecer sin modificación.
if (second.Length <= first.Length || !second.AsSpan(0, first.Length).SequenceEqual(first))
    throw new Exception("ERROR: la segunda firma modificó los bytes de la primera revisión.");

var validator = new PdfSignatureValidator(new ValidationOptions { CheckRevocation = false });
using var stream = new MemoryStream(second);
var results = await validator.ValidateAsync(stream);
var signatures = results.ToList();
if (signatures.Count != 2 || signatures.Any(sig => !sig.IsIntegrityValid))
    throw new Exception("ERROR: no se conservaron dos firmas criptográficamente íntegras.");

Console.WriteLine("OK: dos firmas, bytes previos conservados y ambas integridades verificadas.");
static byte[] CreateMinimalPdf()
{
    var data = new List<byte>();
    void Put(string str) => data.AddRange(Encoding.ASCII.GetBytes(str));
    Put("%PDF-1.4\n");
    var offsets = new List<int> { 0 };
    foreach (string obj in new [] {
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>"
    })
    {
        offsets.Add(data.Count);
        Put($"{offsets.Count - 1} 0 obj\n{obj}\nendobj\n");
    }
    int xref = data.Count;
    Put("xref\n0 4\n0000000000 65535 f \n");
    for (int i=1; i<=3; i++) Put($"{offsets[i]:D10} 00000 n \n");
    Put($"trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    return data.ToArray();
}
