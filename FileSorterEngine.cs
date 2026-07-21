using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileSorter;

public class FileSorterEngine
{
    private readonly string _targetPath;
    private readonly bool _dryRun;
    public string? ConfigPath { get; set; }
    public bool OrganizeByDate { get; set; }

    private static readonly string UndoLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileSorter", "restore.json");

    private List<ConfigRule> _rules = new();

    public FileSorterEngine(string targetPath, bool dryRun = false)
    {
        _targetPath = targetPath;
        _dryRun = dryRun;
    }

    // ── Run ──

    public void Run()
    {
        if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath))
        {
            Log.Error("El directorio '{Path}' no existe o no es válido.", _targetPath);
            return;
        }

        LoadConfig();

        Log.Information("📂 Organizando: {Path}", _targetPath);
        if (_dryRun) Log.Warning("🔍 MODO SIMULACIÓN — No se moverá ningún archivo");

        string[] files;
        try { files = Directory.GetFiles(_targetPath); }
        catch (Exception ex) { Log.Error(ex, "Error al leer el directorio"); return; }

        if (files.Length == 0) { Log.Information("No hay archivos para organizar."); return; }

        Log.Information("Archivos encontrados: {Count}", files.Length);

        var moves = new List<FileMove>();
        int moved = 0, errors = 0;

        foreach (string file in files)
        {
            var result = ProcessFile(file);
            if (result != null) { moves.Add(result); moved++; }
            else errors++;
        }

        if (!_dryRun && moves.Count > 0) SaveUndoLog(moves);

        Log.Information("── Resumen ──");
        Log.Information("✅ Archivos procesados: {Moved}", moved);
        if (errors > 0) Log.Warning("⚠️  Errores: {Errors}", errors);
        if (_rules.Count > 0) Log.Information("📋 Reglas aplicadas: {Count}", _rules.Count);
        if (_dryRun) Log.Information("💡 Ejecuta sin --dry-run para aplicar los cambios");
    }

    // ── Process File ──

    FileMove? ProcessFile(string filePath)
    {
        try
        {
            string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = "sin_extension";

            // Determine destination folder: config rules > extension
            string destDir;
            var rule = _rules.FirstOrDefault(r =>
                r.Extensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)));
            if (rule != null)
                destDir = Path.Combine(_targetPath, rule.Folder);
            else
                destDir = Path.Combine(_targetPath, extension);

            // Add date subfolder if --by-date
            if (OrganizeByDate)
            {
                var lastWrite = File.GetLastWriteTime(filePath);
                destDir = Path.Combine(destDir, lastWrite.Year.ToString(), lastWrite.Month.ToString("00"));
            }

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

            var move = new FileMove { Source = filePath, Destination = destPath, Extension = extension };

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

    // ── Config ──

    void LoadConfig()
    {
        if (string.IsNullOrEmpty(ConfigPath) || !File.Exists(ConfigPath)) return;

        try
        {
            string text = File.ReadAllText(ConfigPath);
            if (ConfigPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var cfg = JsonSerializer.Deserialize<FileSorterConfig>(text);
                if (cfg?.Rules != null) _rules = cfg.Rules;
            }
            else if (ConfigPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                     ConfigPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                // Simple YAML-like parser (for .yaml/.yml files)
                _rules = ParseSimpleYaml(text);
            }

            Log.Information("📋 Configuración cargada: {Count} reglas desde {Path}", _rules.Count, ConfigPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️  Error al cargar configuración");
        }
    }

    List<ConfigRule> ParseSimpleYaml(string yaml)
    {
        var rules = new List<ConfigRule>();
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        ConfigRule? current = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('-') && trimmed.Contains(':'))
            {
                if (current != null) rules.Add(current);
                current = new ConfigRule();
                continue;
            }
            if (current != null && trimmed.Contains(':'))
            {
                var parts = trimmed.Split(':', 2);
                var key = parts[0].Trim().ToLowerInvariant();
                var val = parts[1].Trim().Trim('"').Trim('\'');

                if (key == "folder") current.Folder = val;
                else if (key == "extensions" || key == "ext")
                {
                    current.Extensions = val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
        }
        if (current != null) rules.Add(current);
        return rules;
    }

    // ── Report ──

    public void GenerateReport()
    {
        if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath))
        {
            Log.Error("El directorio '{Path}' no existe.", _targetPath);
            return;
        }

        string[] files;
        try { files = Directory.GetFiles(_targetPath); }
        catch (Exception ex) { Log.Error(ex, "Error al leer el directorio"); return; }

        var groups = files
            .GroupBy(f => Path.GetExtension(f).TrimStart('.').ToLowerInvariant())
            .Select(g => new { Extension = string.IsNullOrEmpty(g.Key) ? "sin_extension" : g.Key, Count = g.Count(), TotalSize = g.Sum(f => new FileInfo(f).Length) })
            .OrderByDescending(g => g.Count)
            .ToList();

        long totalSize = groups.Sum(g => g.TotalSize);
        int totalFiles = groups.Sum(g => g.Count);

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  📊 REPORTE DE ARCHIVOS");
        Console.WriteLine($"  📂 {_targetPath}");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"  Total: {totalFiles} archivos ({FormatSize(totalSize)})");
        Console.WriteLine();

        foreach (var g in groups)
        {
            double pct = (double)g.Count / totalFiles * 100;
            Console.WriteLine($"  .{g.Extension,-12} {g.Count,4} archivos  {FormatSize(g.TotalSize),8}  {pct,5:F1}%");
        }
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"  💡 Usa: dotnet run -- --path \"{_targetPath}\"");
        Console.WriteLine();

        Log.Information("Reporte generado: {Count} tipos, {Files} archivos, {Size}", groups.Count, totalFiles, FormatSize(totalSize));
    }

    static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    // ── Undo ──

    public void Undo()
    {
        if (!File.Exists(UndoLogPath))
        {
            Log.Error("No hay ninguna operación para deshacer.");
            return;
        }

        try
        {
            var log = JsonSerializer.Deserialize<RestoreLog>(File.ReadAllText(UndoLogPath));
            if (log == null || log.Moves.Count == 0) { Log.Warning("El log está vacío."); return; }

            Log.Information("↩️  Deshaciendo organización de {Date}", log.Timestamp.ToLocalTime());
            int restored = 0, errors = 0;

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
                        restored++;
                    }
                    else Log.Warning("⚠️  No se encuentra: {File}", Path.GetFileName(move.Destination));
                }
                catch (Exception ex) { Log.Error(ex, "❌ Error al restaurar"); errors++; }
            }

            Log.Information("✅ Archivos restaurados: {Restored}", restored);
            if (errors == 0) { File.Delete(UndoLogPath); Log.Information("🧹 Log eliminado."); }
        }
        catch (Exception ex) { Log.Error(ex, "Error al leer el log"); }
    }

    void SaveUndoLog(List<FileMove> moves)
    {
        try
        {
            string dir = Path.GetDirectoryName(UndoLogPath)!;
            Directory.CreateDirectory(dir);
            var log = new RestoreLog { Timestamp = DateTime.UtcNow, SourcePath = _targetPath, Moves = moves };
            File.WriteAllText(UndoLogPath, JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }));
            Log.Information("📝 Log de restauración guardado");
        }
        catch (Exception ex) { Log.Warning(ex, "No se pudo guardar el log"); }
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

public class FileSorterConfig
{
    public List<ConfigRule> Rules { get; set; } = new();
}

public class ConfigRule
{
    public string Folder { get; set; } = "";
    public List<string> Extensions { get; set; } = new();
}
