using dnlib.DotNet;

namespace AsmDefGenerator;

public static class Application
{
    private static string[] ScanDir(string dir)
    {
        var files = Directory.GetFiles(dir, "*.dll").ToList();

        foreach (var subDir in Directory.GetDirectories(dir))
            files.AddRange(ScanDir(subDir));

        return files.ToArray();
    }

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("You must specify a file to generate a dummy DLL from");
            return;
        }

        var files = new List<string>();

        if (Directory.Exists(args[0])) files.AddRange(ScanDir(args[0]));

        if (File.Exists(args[0])) files.Add(args[0]);

        if (files.Count < 1)
        {
            Console.WriteLine($"File \"{args[0]}\" does not exist");
            return;
        }

        foreach (var file in files)
            try
            {
                Console.WriteLine($"Processing {file}");

                // Read original assembly
                var context = ModuleDef.CreateModuleContext();
                var module = ModuleDefMD.Load(file, context);

                module.Resources.Clear();

                foreach (var type in module.Types)
                {
                    // Make class, interface or struct public
                    type.Visibility |= TypeAttributes.Public;

                    foreach (var method in type.Methods)
                    {
                        // Detect whether or not this method is present multiple times (as an override)
                        // This will only publicize the method defined by the type, and keeps the overridden method as-is
                        if (!method.HasOverrides ||
                            type.Methods.Count(m =>
                                m.Name.ToString() == method.Name.ToString().Split(".").Last() ||
                                m.Name.ToString() == method.Name.ToString()) == 1)
                        {
                            // Make method public
                            method.Access |= MethodAttributes.Public;
                            method.Access &= ~MethodAttributes.Private;
                        }

                        // Clear method instructions and exception handlers
                        method.Body?.ExceptionHandlers?.Clear();
                        method.Body?.Instructions?.Clear();
                    }

                    foreach (var field in type.Fields)
                    {
                        // Make field public
                        field.Access &= ~FieldAttributes.Private;
                        field.Access |= FieldAttributes.Public;
                    }

                    foreach (var @event in type.Events)
                    {
                        // Find the field associated with this event, and make the field private to fix ambiguity errors
                        var backingField = type.Fields.FirstOrDefault(field => field.FullName == @event.FullName);
                        if (backingField == null) continue;

                        backingField.Access &= ~FieldAttributes.Public;
                        backingField.Access |= FieldAttributes.Private;
                    }
                }

                // Emit the new generated assembly
                var folder = Path.GetDirectoryName(file)!;
                var tempFilename = Path.GetFileNameWithoutExtension(file) + ".def" + Path.GetExtension(file);

                module.Write(Path.Combine(folder, tempFilename));
                module.Dispose();

                File.Move(Path.Combine(folder, tempFilename), file, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process {file}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
    }
}