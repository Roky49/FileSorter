using System.CommandLine;
using Serilog;

namespace FileSorter;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/filesorter-.log", rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .CreateLogger();

        try
        {
            var pathOption = new Option<DirectoryInfo?>(["--path", "-p"], "Ruta del directorio a organizar");
            var dryRunOption = new Option<bool>(["--dry-run", "-d"], "Simular sin mover archivos");
            var undoOption = new Option<bool>(["--undo", "-u"], "Deshacer la última organización");
            var configOption = new Option<FileInfo?>(["--config", "-c"], "Ruta del archivo de configuración YAML/JSON con reglas personalizadas");
            var byDateOption = new Option<bool>(["--by-date"], "Organizar también por año/mes");
            var reportOption = new Option<bool>(["--report", "-r"], "Generar reporte de tipos de archivo sin organizar");

            var rootCommand = new RootCommand("Organizador de archivos por extensión");
            rootCommand.AddOption(pathOption);
            rootCommand.AddOption(dryRunOption);
            rootCommand.AddOption(undoOption);
            rootCommand.AddOption(configOption);
            rootCommand.AddOption(byDateOption);
            rootCommand.AddOption(reportOption);

            rootCommand.SetHandler((DirectoryInfo? path, bool dryRun, bool undo, FileInfo? config, bool byDate, bool report) =>
            {
                if (undo) { UndoLastOperation(); return; }

                string targetPath = path?.FullName ?? AskForPath();

                var sorter = new FileSorterEngine(targetPath, dryRun)
                {
                    ConfigPath = config?.FullName,
                    OrganizeByDate = byDate
                };

                if (report)
                {
                    sorter.GenerateReport();
                    return;
                }

                sorter.Run();
            }, pathOption, dryRunOption, undoOption, configOption, byDateOption, reportOption);

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error inesperado");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static string AskForPath()
    {
        Console.Write("Ingrese la ruta del directorio a organizar: ");
        string? input = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(input) || !Directory.Exists(input))
        {
            Console.Write("Ruta no válida. Intente de nuevo: ");
            input = Console.ReadLine();
        }
        return input;
    }

    static void UndoLastOperation()
    {
        var engine = new FileSorterEngine("");
        engine.Undo();
    }
}
