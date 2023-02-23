using System.Diagnostics;
using System.Reflection;
using System.Collections;

namespace apm {
    class SelfProfiler {
        private static int indents = 0;

        private static void DumpValue(object? value, Type type) {
            if (value is Hashtable) {
                Console.WriteLine();
                foreach (DictionaryEntry v in (Hashtable) value) {
                    Console.Write(v.Key);
                    Console.WriteLine($":\t{v.Value}");
                }
                return;
            }

            if (value is IEnumerable && value is not IEnumerable<Char> && value is not IEnumerable<Process>) {
                Console.WriteLine();
                foreach (var e in (value as IEnumerable)) {
                    indents++;
                    if (e is string || e is String)
                        Console.WriteLine(e);
                    else
                        DumpGetters(e, e.GetType());
                    indents--;
                }
                return;
            }
            Console.WriteLine(value);
        }


        private static void DumpGetters(object? obj, Type type) {
            foreach (PropertyInfo propInfo in type.GetProperties()) {
                try {
                    for (int i = 0; i < indents; ++i)
                        Console.Write("\t");
                    Console.Write($"{propInfo.Name}:\t");
                    var propValue = propInfo.GetValue(obj);
                    DumpValue(propValue, propInfo.PropertyType);
                } catch (Exception e) {
                    Console.WriteLine($"{e.GetBaseException().Message}");
                }
            }

            foreach (MethodInfo methodInfo in type.GetMethods()) {
                if (methodInfo.Name.StartsWith("Get") && methodInfo.GetParameters().Length == 0) {
                    try {
                        for (int i = 0; i < indents; ++i)
                            Console.Write("\t");
                        Console.Write($"{methodInfo.Name}:\t");
                        var result = methodInfo.Invoke(obj, null);
                        DumpValue(result, methodInfo.ReturnType);
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

            Console.WriteLine("---     Process Info      ---");
            var self = Process.GetCurrentProcess();
            DumpGetters(self, self.GetType());
            Console.WriteLine("---   Environment Info    ---");
            DumpGetters(null, typeof(System.Environment));

            // Console.WriteLine($"MachineName:\t{System.Environment.MachineName}");
            // Console.WriteLine($"CommandLine:\t{System.Environment.CommandLine}");
            // int drive_nr = 0;
            // foreach (var drive in System.Environment.GetLogicalDrives()) {
            //     Console.WriteLine($"Drive#{drive_nr++}:\t{drive}");
            // }
            return 0;
        }
    }
}