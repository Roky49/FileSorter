# FileSorter — Organizador de Archivos por Extensión

Aplicación CLI en C# (.NET) que **organiza archivos automáticamente** en carpetas según su extensión.

## 🚀 ¿Qué hace?

Dado un directorio, mueve cada archivo a una subcarpeta con el nombre de su extensión:

```
📂 CarpetaDesordenada
   ├── foto.jpg
   ├── doc.pdf
   ├── cancion.mp3
   └── video.mp4

     ↓ Ejecutas FileSorter ↓

📂 CarpetaDesordenada
   ├── 📁 jpg/
   │   └── foto.jpg
   ├── 📁 pdf/
   │   └── doc.pdf
   ├── 📁 mp3/
   │   └── cancion.mp3
   └── 📁 mp4/
       └── video.mp4
```

## ⚙️ Uso

```bash
dotnet run
```

Te pedirá la ruta del directorio a organizar.

O con Docker:

```bash
docker build -t filesorter .
docker run -it -v /ruta/a/organizar:/data filesorter
```

## 🛠️ Tecnologías

- **Lenguaje:** C# 10+
- **Framework:** .NET 6+
- **Docker:** Multi-stage build (imagen ~150MB)

## ✅ Estado

Completado y funcional. Organiza archivos por extensión con manejo de errores y soporte Docker.

## 🚧 Mejoras futuras

- [ ] CLI con argumentos (`--path`, `--dry-run`)
- [ ] Modo simulación (mostrar qué movería sin mover nada)
- [ ] Tests unitarios
- [ ] Logging con Serilog
- [ ] Deshacer operación
