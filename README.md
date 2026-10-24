# EduVirtual Firmas v0.6.6 — SEGURIDAD: multifirma todavía NO soportada

Esta versión mantiene el motor PDFsharp y **bloquea de forma preventiva** documentos donde detecta estructuras habituales de firmas previas. El control es heurístico y no puede garantizar detección de todos los documentos firmados. **No utilizar para añadir una segunda firma.** FirmaEC sigue siendo la opción recomendada para firmar PDFs previamente firmados.

## Por qué se requiere cambiar el motor

PDFsharp 6.2 vuelve a escribir el archivo completo. La firma adicional debe añadirse mediante **actualización incremental del PDF**, conservando sin modificación todos los bytes originales. El motor futuro también debe verificar cada firma antes y después, conservar los campos de firma, respetar permisos DocMDP y rechazar entregas cuando no se pueda demostrar que todas siguen válidas.

## Motor candidato y licencia

iText Core (.NET) permite guardar en modo `UseAppendMode()`, pero se distribuye bajo licencia **AGPLv3 o comercial**, que no equivale a la licencia MIT actual. **No se ha integrado iText en v0.6.6**. Su adopción requeriría revisar y cumplir las condiciones de licencia. Hasta completar las pruebas automáticas con PDFs firmados por FirmaEC y otros lectores, no se anunciará soporte de multifirma.

## Funciones actuales

- Firma inicial con P12 para documentos sin firmas anteriores.
- Visor PDF y posicionamiento con mouse.
- Apariencia compacta con código QR informativo, nombre y logo EduVirtual incrustado.
- Conserva el PDF original generando otro con sufijo.
- Búsqueda manual de nuevas versiones de GitHub.

## Pruebas obligatorias antes de multifirma

1. PDF sin firma: agregar firma y verificar integridad.
2. PDF con una firma válida: comprobarla, añadir la segunda incrementalmente y validar **ambas**.
3. PDF con dos firmas: agregar una tercera y validar las tres.
4. PDF con cambios prohibidos por DocMDP: **rechazar** firma.
5. PDF con firmas inválidas o ambiguas: **rechazar** operación.
6. Fallo de I/O: eliminar salida parcial, preservar PDF original.
7. Comprobar aceptación en FirmaEC y al menos otro verificador independiente.

No subas a GitHub P12, contraseñas ni PDFs personales.
