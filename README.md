# EduFirma

Aplicación de escritorio para Windows desarrollada desde cero en **C# / .NET 8 / WinForms**.

## Estado actual: v0.1.0 (prototipo)
- Selección local de PDF y certificados PKCS#12 (.p12/.pfx).
- Inspección local del titular, emisor y vigencia del certificado sin almacenar su contraseña.
- Botón **Buscar actualizaciones** que consulta GitHub Releases bajo demanda.
- **Todavía NO firma PDF.** La firma criptográfica PDF y la validación de firmas son trabajo pendiente.

## Generar EduFirma.exe
En GitHub abre **Actions → Compilar EduFirma Windows → Run workflow**. Al finalizar correctamente descarga el artefacto `EduFirma-Windows-x64`. También se puede publicar desde un equipo Windows con SDK .NET 8:

```powershell
dotnet publish EduFirma/EduFirma.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

## Actualizaciones y privacidad
El botón consulta `https://api.github.com/repos/PadillaC17/EduFirma/releases/latest` **solo cuando el usuario lo pulsa**. No sube PDFs, contraseñas ni certificados. **Mientras el repositorio sea privado, la consulta anónima no podrá leer Releases privados**: el programa explicará la limitación y abrirá el navegador para revisar las versiones con la sesión de GitHub. Cuando se publique un canal de distribución público, la consulta podrá comprobar directamente las versiones. No instala ni descarga versiones automáticamente.

## Seguridad
No subir certificados reales, contraseñas o documentos personales al repositorio. Las compilaciones de prueba no están firmadas con certificado Authenticode y Windows SmartScreen podría mostrar un aviso.

## Licencia
MIT; revisar las licencias de cualquier dependencia nueva antes de incorporarla.
