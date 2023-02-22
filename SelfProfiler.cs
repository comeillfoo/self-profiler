using System.Diagnostics;
using System.Reflection;
using System.Collections;

namespace apm {
    class SelfProfiler {
        private static int indents = 0;

        private static void DumpValue(object? value) {
            if (value is IEnumerable && value is not IEnumerable<Char>) {
                foreach (var e in (IEnumerable) value) {
                    indents++;
                    DumpGetters(e);
                    indents--;
                }
            } else Console.WriteLine(value);
        }


        private static void DumpGetters(object p) {
            foreach (PropertyInfo propInfo in p.GetType().GetProperties()) {
                try {
                    for (int i = 0; i < indents; ++i)
                        Console.Write("\t");
                    Console.Write($"{propInfo.Name}:\t");
                    var propValue = propInfo.GetValue(p);
                    DumpValue(propValue);
                } catch (Exception e) {
                    Console.WriteLine($"{e.GetBaseException().Message}");
                }
            }

            foreach (MethodInfo methodInfo in p.GetType().GetMethods()) {
                if (methodInfo.Name.StartsWith("Get") && methodInfo.GetParameters().Length == 0) {
                    try {
                        for (int i = 0; i < indents; ++i)
                            Console.Write("\t");
                        Console.Write($"{methodInfo.Name}:\t");
                        var result = methodInfo.Invoke(p, null);
                        Console.WriteLine(result);
                    } catch (Exception e) {
                        Console.WriteLine($"{e.GetBaseException().Message}");
                    }
                }
            }
            Console.WriteLine();
        }

        public static int Main(string[] args) {
            int[] a = new int[791];
            for (int i = 0; i < a.Length; ++i) {
                a[i] = ((i + 11) * i) % a.Length;
            }

            Console.WriteLine("--- Process Info ---");
            DumpGetters(Process.GetCurrentProcess());
            Console.WriteLine("---   OS Info    ---");
            DumpGetters(System.Environment.OSVersion);

            Console.WriteLine($"MachineName:\t{System.Environment.MachineName}");
            Console.WriteLine($"CommandLine:\t{System.Environment.CommandLine}");
            int drive_nr = 0;
            foreach (var drive in System.Environment.GetLogicalDrives()) {
                Console.WriteLine($"Drive#{drive_nr++}:\t{drive}");
            }
            return 0;
        }
    }
}