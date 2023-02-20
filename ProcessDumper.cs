using System.Diagnostics;

namespace apm {
    class ProcessDumper {
        public static int Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("expected process pid");
                return 22; // EINVAL
            }
            int pid = int.Parse(args[0]);
            Process target = Process.GetProcessById(pid);

            Console.WriteLine($"Process[{target.Id}:{target.BasePriority}]: {target.ProcessName}; HasExited: {target.HasExited}");
            if (target.HasExited)
                Console.WriteLine($"ExitCode: {target.ExitCode}; ExitTime: {target.ExitTime}");
            Console.WriteLine($"MachineName: {target.MachineName}; MainModule: {target.MainModule}");
            Console.WriteLine($"MainWindowTitle: {target.MainWindowTitle}");
            Console.WriteLine($"MEM: PagedSystemMemorySize64: {target.PagedSystemMemorySize64}");

            return 0;
        }
    }
}