# FileSorter

Aplicación CLI en C# (.NET) que **organiza archivos automáticamente** en carpetas según su extensión. Con soporte para CLI, dry-run, deshacer, logging y tests.

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

### Modo interactivo
```bash
dotnet run
```
Te pedirá la ruta del directorio a organizar.

### CLI con argumentos
```bash
# Ruta directa
dotnet run -- --path ""

# Modo simulación (solo muestra lo que haría)
dotnet run -- --path "" --dry-run

# Deshacer última operación
dotnet run -- --undo
```

### Con Docker
```bash
docker build -t filesorter .
docker run -it -v /ruta/a/organizar:/data filesorter
```

## 🛠️ Tecnologías

- **Lenguaje:** C# 12
- **Framework:** .NET 9
- **Logging:** Serilog (consola + archivo rotativo)
- **Testing:** xUnit (6 tests)
- **Docker:** Multi-stage build

## 📊 Progreso

**✅ 8/8 funcionalidades implementadas** | **100% completo** ██████████

## 🚀 Próximas mejoras (fase 2)

- [ ] **Interfaz gráfica** — App WPF o web con Blazor para gestión visual
- [ ] **Reglas por carpeta** — Configuración YAML/JSON para organización personalizada
- [ ] **Watch mode** — Monitorear carpeta y organizar automáticamente
- [ ] **Organización por fecha** — Año/Mes además de extensión
- [ ] **Compresión de imágenes** — Optimizar JPEG/PNG al mover
- [ ] **Estadísticas de uso** — Reporte de tipos de archivo organizados

| Feature | Descripción |
|---|---|
| ✅ **Organización por extensión** | Agrupa archivos en carpetas según su tipo |
| ✅ **CLI con argumentos** | `--path`, `--dry-run`, `--undo` |
| ✅ **Modo simulación** | Muestra qué movería sin ejecutar nada |
| ✅ **Deshacer** | Restaura archivos a su ubicación original |
| ✅ **Logging** | Logs en consola + archivo con Serilog |
| ✅ **Manejo de colisiones** | Renombra automáticamente archivos duplicados |
| ✅ **Tests unitarios** | 6 tests con xUnit |
| ✅ **Docker** | Imagen lista para ejecutar |

## 📊 Tests

```
Pruebas totales: 6
     Correcto: 6
```
