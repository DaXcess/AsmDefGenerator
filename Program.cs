using dnlib.DotNet;

namespace AsmDefGenerator
{
    public class Application
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You must specify a file to generate a dummy DLL from");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"File \"{args[0]}\" does not exist");
                return;
            }

            // Read original assembly
            var context = ModuleDef.CreateModuleContext();
            var module = ModuleDefMD.Load(args[0], context);

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
            }

            module.Write(Path.GetFileNameWithoutExtension(args[0]) + ".def" + Path.GetExtension(args[0]));
        }
    }
}