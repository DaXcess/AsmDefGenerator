using dnlib.DotNet;

namespace AsmDefGenerator
{
    public class Application
    {
        private static string[] ScanDir(string dir)
        {
            var files = Directory.GetFiles(dir, "*.dll").ToList();

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                Console.WriteLine(subDir);

                files.AddRange(ScanDir(subDir));
            }

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

            if (Directory.Exists(args[0]))
            {
                files.AddRange(ScanDir(args[0]));
            }

            if (File.Exists(args[0]))
            {
                files.Add(args[0]);
            }

            if (files.Count < 1)
            {
                Console.WriteLine($"File \"{args[0]}\" does not exist");
                return;
            }

            foreach (var file in files)
            {
                Console.WriteLine($"Processing {file}");

                // Read original assembly
                var context = ModuleDef.CreateModuleContext();
                var module = ModuleDefMD.Load(file, context);

                module.Resources.Clear();

                foreach (var type in module.Types)
                {
                    type.Visibility |= TypeAttributes.Public;

                    foreach (var method in type.Methods)
                    {
                        method.Access |= MethodAttributes.Public;
                        method.Access &= ~MethodAttributes.Private;

                        method.Body?.ExceptionHandlers?.Clear();
                        method.Body?.Instructions?.Clear();
                    }

                    foreach (var field in type.Fields)
                    {
                        field.Access &= ~FieldAttributes.Private;
                        field.Access |= FieldAttributes.Public;
                    }

                    foreach (var @event in type.Events)
                    {
                        var backingField = type.Fields.FirstOrDefault(field => field.FullName == @event.FullName);

                        if (backingField != null)
                        {
                            backingField.Access &= ~FieldAttributes.Public;
                            backingField.Access |= FieldAttributes.Private;
                        }
                    }
                }

                var folder = Path.GetDirectoryName(file)!;
                var tempFilename = Path.GetFileNameWithoutExtension(file) + ".def" + Path.GetExtension(file);

                module.Write(Path.Combine(folder, tempFilename));
                module.Dispose();

                File.Move(Path.Combine(folder, tempFilename), file, true);
            }
        }
    }
}