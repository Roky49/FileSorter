using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Ingrese la ruta del directorio a organizar:");
        string directoryPath = Console.ReadLine();

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine("El directorio no existe.");
            return;
        }

        OrganizarArchivos(directoryPath);
        Console.WriteLine("Organización completada.");
    }

    static void OrganizarArchivos(string directoryPath)
    {
        string[] files = Directory.GetFiles(directoryPath);

        foreach (string file in files)
        {
            string extension = Path.GetExtension(file).TrimStart('.');
            if (string.IsNullOrEmpty(extension))
            {
                extension = "SinExtensión";
            }

            string newFolder = Path.Combine(directoryPath, extension);
            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
            }

            string fileName = Path.GetFileName(file);
            string newFilePath = Path.Combine(newFolder, fileName);

            try
            {
                File.Move(file, newFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al mover {fileName}: {ex.Message}");
            }
        }
    }
}
