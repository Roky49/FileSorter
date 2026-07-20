using Serilog;
using System.Text.Json;

namespace FileSorter;

public class FileSorterEngine
{
    private readonly string _targetPath;
    private readonly bool _dryRun;
    private static readonly string UndoLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileSorter", "restore.json");

    public FileSorterEngine(string targetPath, bool dryRun = false)
    {
        _targetPath = targetPath;
        _dryRun = dryRun;
    }

    public void Run()
    {
        if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath))
        {
            Log.Error("El directorio '{Path}' no existe o no es válido.", _targetPath);
            return;
        }

        Log.Information("📂 Organizando: {Path}", _targetPath);
        if (_dryRun) Log.Warning("🔍 MODO SIMULACIÓN — No se moverá ningún archivo");

        string[] files;
        try
        {
            files = Directory.GetFiles(_targetPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al leer el directorio {Path}", _targetPath);
            return;
        }

        if (files.Length == 0)
        {
            Log.Information("No hay archivos para organizar en {Path}", _targetPath);
            return;
        }

        Log.Information("Archivos encontrados: {Count}", files.Length);

        var moves = new List<FileMove>();
        int moved = 0, errors = 0;

        foreach (string file in files)
        {
            var result = ProcessFile(file);
            if (result != null)
            {
                moves.Add(result);
                moved++;
            }
            else
            {
                errors++;
            }
        }

        // Save restore log for undo
        if (!_dryRun && moves.Count > 0)
        {
            SaveUndoLog(moves);
        }

        Log.Information("── Resumen ──");
        Log.Information("✅ Archivos procesados: {Moved}", moved);
        if (errors > 0) Log.Warning("⚠️  Errores: {Errors}", errors);
        if (_dryRun) Log.Information("💡 Ejecuta sin --dry-run para aplicar los cambios");
    }

    FileMove? ProcessFile(string filePath)
    {
        try
        {
            string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = "sin_extension";

            string destDir = Path.Combine(_targetPath, extension);
            string fileName = Path.GetFileName(filePath);
            string destPath = Path.Combine(destDir, fileName);

            // Handle name collisions
            int counter = 1;
            while (File.Exists(destPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                destPath = Path.Combine(destDir, $"{nameNoExt}_{counter}{Path.GetExtension(fileName)}");
                counter++;
            }

            var move = new FileMove
            {
                Source = filePath,
                Destination = destPath,
                Extension = extension
            };

            if (_dryRun)
            {
                Log.Information("🔍 [SIMULACIÓN] {File} → {Dest}", fileName, destDir);
            }
            else
            {
                Directory.CreateDirectory(destDir);
                File.Move(filePath, destPath);
                Log.Information("✅ {File} → {Dest}", fileName, destDir);
            }

            return move;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al procesar {File}", filePath);
            return null;
        }
    }

    void SaveUndoLog(List<FileMove> moves)
    {
        try
        {
            string dir = Path.GetDirectoryName(UndoLogPath)!;
            Directory.CreateDirectory(dir);

            var log = new RestoreLog
            {
                Timestamp = DateTime.UtcNow,
                SourcePath = _targetPath,
                Moves = moves
            };

            File.WriteAllText(UndoLogPath, JsonSerializer.Serialize(log, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            Log.Information("📝 Log de restauración guardado en {Path}", UndoLogPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo guardar el log de restauración");
        }
    }

    public void Undo()
    {
        if (!File.Exists(UndoLogPath))
        {
            Log.Error("No hay ninguna operación para deshacer. Ejecuta primero el organizador.");
            return;
        }

        try
        {
            var log = JsonSerializer.Deserialize<RestoreLog>(File.ReadAllText(UndoLogPath));
            if (log == null || log.Moves.Count == 0)
            {
                Log.Warning("El log de restauración está vacío.");
                return;
            }

            Log.Information("↩️  Deshaciendo organización de {Date}", log.Timestamp.ToLocalTime());
            Log.Information("Origen: {Path}", log.SourcePath);

            int restored = 0, errors = 0;
            // Process in reverse order to restore
            for (int i = log.Moves.Count - 1; i >= 0; i--)
            {
                var move = log.Moves[i];
                try
                {
                    if (File.Exists(move.Destination))
                    {
                        string? destDir = Path.GetDirectoryName(move.Source);
                        if (destDir != null) Directory.CreateDirectory(destDir);
                        File.Move(move.Destination, move.Source);
                        Log.Information("↩️  Restaurado: {File}", Path.GetFileName(move.Source));
                        restored++;
                    }
                    else
                    {
                        Log.Warning("⚠️  No se encuentra: {File}", Path.GetFileName(move.Destination));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "❌ Error al restaurar {File}", Path.GetFileName(move.Source));
                    errors++;
                }
            }

            Log.Information("── Resumen de restauración ──");
            Log.Information("✅ Archivos restaurados: {Restored}", restored);
            if (errors > 0) Log.Warning("⚠️  Errores: {Errors}", errors);

            // Delete undo log after successful restore
            if (errors == 0)
            {
                File.Delete(UndoLogPath);
                Log.Information("🧹 Log de restauración eliminado.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al leer el log de restauración");
        }
    }
}

// ── Data models ──

public class FileMove
{
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Extension { get; set; } = "";
}

public class RestoreLog
{
    public DateTime Timestamp { get; set; }
    public string SourcePath { get; set; } = "";
    public List<FileMove> Moves { get; set; } = new();
}
