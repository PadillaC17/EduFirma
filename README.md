# EduFirma v0.2.0

Aplicación Windows de código abierto en **C# / .NET 8 / Windows Forms**.

## Incluido en esta versión
- Interfaz rediseñada en tres pestañas: **Firmar documentos**, **Verificar documento** y **Validar certificado**.
- Selección múltiple de PDF y posibilidad de arrastrarlos a la lista.
- Abrir los PDF en el lector predeterminado de Windows.
- Cargar certificados locales PKCS#12 y consultar titular, emisor, fechas y disponibilidad de clave privada.
- Campos para razón, lugar, página y tipo de sello como preparativos para la firma visible.
- Botón **Buscar actualizaciones**: consulta GitHub Releases bajo demanda; en repositorios privados abre el navegador.
- Licencia MIT. Los archivos P12, PDF y secretos están excluidos por .gitignore.

**IMPORTANTE:** Esta versión **NO firma PDF** ni valida criptográficamente firmas PDF. El botón Firmar permanece deshabilitado. La pestaña Verificar informa de esa limitación. No se hacen afirmaciones de seguridad basadas únicamente en el período de validez de un certificado.

## Compilar el EXE
En el repositorio, entra a **Actions → Compilar EduFirma Windows → Run workflow**; abre la ejecución que haya finalizado correctamente y descarga el artefacto **EduFirma-Windows-x64**. Se necesita Windows x64; el archivo se publica como ejecutable autónomo de .NET, sin instalar Python.

## Privacidad y actualizaciones
El programa no sube documentos, certificados ni contraseñas. Solo el botón de búsqueda de actualizaciones inicia una conexión HTTPS a GitHub (o abre el navegador cuando el repositorio es privado). Esta versión no descarga ni instala automáticamente actualizaciones.

## Próximas fases
1. Motor de firma PAdES compatible con certificados P12, con pruebas en lectores PDF.
2. Vista previa del PDF y posicionamiento visual del sello.
3. Verificación de firmas y cadena de confianza, con revocación en modo online opcional.
4. Distribución de actualizaciones verificadas, sin acumular ZIP.
