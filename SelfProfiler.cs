using System.Diagnostics;
using System.Reflection;

namespace apm {
    class SelfProfiler {
        private static void DumpProcess(Process p) {
            foreach (PropertyInfo propInfo in p.GetType().GetProperties()) {
                if (propInfo.Name.StartsWith("Exit") || propInfo.Name.StartsWith("StartInfo"))
                    continue;
                try {
                    Console.Write($"{propInfo.Name}: ");
                    var propValue = propInfo.GetValue(p);
                    Console.WriteLine(propValue);
                } catch (Exception e) {
                    Console.Error.WriteLine($"{e.GetBaseException().Message}");
                }
            }
        }

        public static int Main(string[] args) {
            int[] a = new int[791];
            for (int i = 0; i < a.Length; ++i) {
                a[i] = ((i + 11) * i) % a.Length;
            }
            Process self = Process.GetCurrentProcess();

            // Console.WriteLine($"{self.ToString()}[{self.Id}]#{self.GetHashCode()}\tBasePriority: {self.BasePriority}\t Exited: {(self.HasExited ? 'Y' : 'N')}");
            // // always false
            // if (self.HasExited) {
            //     Console.WriteLine($"ExitCode: {self.ExitCode}\tExitTime: {self.ExitTime}");
            // }
            // Console.WriteLine($"HandleCount: {self.HandleCount}\tHandle: {self.Handle}");
            // Console.WriteLine($"MachineName: {self.MachineName}\tMainModule: {self.MainModule}");
            // Console.WriteLine($"MainWindowTitle: {self.MainWindowTitle}\tMainWindowHandle: {self.MainWindowHandle}");
            // Console.WriteLine($"MaxWorkingSet: {self.MaxWorkingSet} MinWorkingSet: {self.MinWorkingSet}\n");
            // foreach (ProcessModule module in self.Modules) {
            //     Console.WriteLine($"{module.ToString()}#{module.GetHashCode()}:");
            //     Console.WriteLine($"ModuleName: {module.ModuleName}\tFileName: {module.FileName}"); // \t{module.FileVersionInfo}"); exception due to vdso
            //     Console.WriteLine($"BaseAddress: {module.BaseAddress}\tEntryPointAddress: {module.EntryPointAddress}");
            //     Console.WriteLine($"ModuleMemorySize: {module.ModuleMemorySize}\tSite: {module.Site}\n");
            // }
            // Console.WriteLine($"Total: {self.Modules.Count} modules");
            // Console.WriteLine($"NonpagedSystemMemorySize64: {self.NonpagedSystemMemorySize64}\tPagedMemorySize64: {self.PagedMemorySize64}");
            // Console.WriteLine($"PeakPagedMemorySize64: {self.PeakPagedMemorySize64}\tPeakVirtualMemorySize64: {self.PeakVirtualMemorySize64}");
            // Console.WriteLine($"PeakWorkingSet64: {self.PeakWorkingSet64}\tPriorityBoostEnabled: {self.PriorityBoostEnabled}");

            DumpProcess(self);
            return 0;
        }
    }
}