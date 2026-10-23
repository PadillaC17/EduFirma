# EduFirma v0.4.0 — experimental

Aplicación Windows Forms en C# / .NET 8 para firma local de archivos PDF.

## Funciones
- Seleccionar varios PDF, arrastrar archivos y consultar el certificado PKCS#12.
- **Firma CMS/SHA-256 experimental de PDF** mediante PDFsharp 6.2.4 (MIT).
- Guarda copias con sufijo `_EduFirma.pdf`. Nunca sobreescribe los originales.
- Botón para buscar nuevas versiones en GitHub Releases (sin actualización automática).
- Verificación integral de firmas, sello visible editable y previsualización incrustada: **pendientes**.

### Advertencias importantes
**No utilices todavía EduFirma para documentos legales, universitarios o trámites oficiales.** El motor de firma debe superar compilación y pruebas independientes, incluyendo Adobe Acrobat, múltiples PDFs, y certificados de prueba. Esta versión puede reescribir el PDF y afectar firmas existentes; úsala únicamente en documentos sin firmas previas. No incluye comprobación de revocación OCSP/CRL, timestamp de autoridad TSA ni validación PAdES completa. La autenticidad y aceptación legal no están garantizadas.

Nunca subas tu archivo .p12, contraseña o PDFs personales al repositorio ni a GitHub Issues. Firma offline; el botón de actualizaciones sí consulta la API de GitHub.

## Compilar
GitHub → Actions → **Compilar EduFirma Windows** → ejecución correcta → descargar artefacto `EduFirma-Windows-x64`.
La compilación produce un ejecutable independiente `EduFirma.exe` para Windows x64. El proyecto utiliza PDFsharp 6.2.4 sujeto a licencia MIT.

## Plan
1. Pruebas criptográficas de firmar y verificar con certificados de prueba.
2. Vista previa PDF y sello visible interactivo.
3. Verificador integrado completo (integridad, cadena de confianza y revocación opcional).
4. Actualizador seguro con integridad/verificación de descarga y distribución sin ZIP.
