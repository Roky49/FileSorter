using System.CommandLine;
using Serilog;

namespace FileSorter;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // ── Configurar Serilog ──
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/filesorter-.log", rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .CreateLogger();

        try
        {
            var pathOption = new Option<DirectoryInfo?>(
                aliases: ["--path", "-p"],
                description: "Ruta del directorio a organizar")
            { IsRequired = false };

            var dryRunOption = new Option<bool>(
                aliases: ["--dry-run", "-d"],
                description: "Simular sin mover archivos");

            var undoOption = new Option<bool>(
                aliases: ["--undo", "-u"],
                description: "Deshacer la última organización");

            var rootCommand = new RootCommand("Organizador de archivos por extensión");
            rootCommand.AddOption(pathOption);
            rootCommand.AddOption(dryRunOption);
            rootCommand.AddOption(undoOption);

            rootCommand.SetHandler((DirectoryInfo? path, bool dryRun, bool undo) =>
            {
                if (undo)
                {
                    UndoLastOperation();
                    return;
                }

                string targetPath = path?.FullName ?? AskForPath();
                var sorter = new FileSorterEngine(targetPath, dryRun);
                sorter.Run();
            }, pathOption, dryRunOption, undoOption);

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
