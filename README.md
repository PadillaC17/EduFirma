# EduVirtual Firmas v0.6.4 (experimental)

Aplicación de escritorio en C# / .NET 8 para Windows x64. Código abierto bajo licencia MIT.

## Funciones actuales
- Selección múltiple de PDF y archivos de certificado PKCS#12 (.p12/.pfx).
- Firma PDF local con certificado y contraseña, usando PDFsharp 6.2.4; el original no se sobrescribe.
- Visor PDF integrado: mover el sello con el cursor, fijar la ubicación con un clic y confirmar la firma.
- Sello personalizado con nombre del titular, fecha, QR informativo generado localmente y logotipo de **EduVirtual Firmas**.
- El código QR no requiere una página web y no constituye por sí solo una verificación criptográfica.
- Inspección local del certificado y botón de consulta de nuevas versiones publicadas en GitHub Releases.

## El logo queda incluido en la descarga
El archivo del repositorio es `EduFirma/Assets/MarcaEduVirtual.jpg`. GitHub Actions lo copia al artefacto de Windows dentro de `Assets/` y lo puede incrustar en el EXE al compilar.

Estructura del paquete:
```
EduFirma.exe
Assets/
  MarcaEduVirtual.jpg
(otras dependencias)
```

## Descarga y versiones
- [Releases](https://github.com/PadillaC17/EduFirma/releases): versiones publicadas y descargables.
- [Actions](https://github.com/PadillaC17/EduFirma/actions): compilaciones de prueba por commit.
- El botón **Buscar actualizaciones** solo compara la versión instalada con el último Release, no instala automáticamente.

## Limitaciones de seguridad
**Versión experimental.** Antes de utilizarla en un trámite oficial se debe validar de forma independiente la integridad criptográfica, la cadena de confianza y el estado de revocación del certificado. EduVirtual Firmas aún no implementa verificación criptográfica integrada completa, TSA confiable ni una actualización automática firmada. No firmar PDF que ya contienen firmas digitales porque PDFsharp puede reescribirlas.

Los PDF y el P12 permanecen en el equipo al firmar. Nunca subir contraseñas, claves privadas, certificados P12 ni documentos personales a GitHub.
