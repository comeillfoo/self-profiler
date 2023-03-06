using System.Diagnostics;
using System.Reflection;
using System.Collections;
using Mono.Options;

namespace SelfProfiler {

    public class UsageSampler {
        private TimeSpan startSpan = TimeSpan.Zero;
        private DateTime startTime = DateTime.UtcNow;
        private TimeSpan finishSpan;
        private DateTime finishTime;

        public long MemoryUsage { get; private set; }

        public UsageSampler(Process current) {
            finishSpan = current.TotalProcessorTime;
            finishTime = DateTime.UtcNow;
            MemoryUsage = _GetRamUsage(current);
        }

        public void Stamp(Process current) {
            startSpan = finishSpan;
            startTime = finishTime;
            finishSpan = current.TotalProcessorTime;
            finishTime = DateTime.UtcNow;
            MemoryUsage = _GetRamUsage(current);
        }

        public double GetCPUUsage() {
            var cpuUsedMs = (finishSpan - startSpan).TotalMilliseconds;
            var totalMsPassed = (finishTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }

        private long _GetRamUsage(Process current) {
            return Environment.Is64BitOperatingSystem? current.PrivateMemorySize64 : current.PrivateMemorySize;
        }
    }

    public class ObjectDumper {
        private int indents = 0;
        private TextWriter dumpDestination;

        public ObjectDumper(TextWriter output) {
            dumpDestination = output;
        }

        public ObjectDumper() {
            dumpDestination = Console.Out;
        }

        private void DumpValue(object? value, Type type) {
            if (value is Hashtable) {
                dumpDestination.WriteLine();
                foreach (DictionaryEntry v in (Hashtable) value) {
                    dumpDestination.Write(v.Key);
                    dumpDestination.WriteLine($":\t{v.Value}");
                }
                return;
            }

            if (value is IEnumerable && value is not IEnumerable<Char> && value is not IEnumerable<Process>) {
                dumpDestination.WriteLine();
                foreach (var e in (value as IEnumerable)) {
                    if (e is string || e is String)
                        dumpDestination.WriteLine(e);
                    else {
                        indents++;
                        DumpGetters(e, e.GetType());
                        indents--;
                    }
                }
                return;
            }

            if (type.Name.StartsWith("FileVersionInfo")) {
                dumpDestination.WriteLine();
                indents++;
                DumpGetters(value, type);
                indents--;
                return;
            }
            dumpDestination.WriteLine(value);
        }

        public void DumpGetters(object? obj, Type type) {
            foreach (PropertyInfo propInfo in type.GetProperties()) {
                try {
                    for (int i = 0; i < indents; ++i)
                        dumpDestination.Write("\t");
                    dumpDestination.Write($"{propInfo.Name}:\t");
                    var propValue = propInfo.GetValue(obj);
                    DumpValue(propValue, propInfo.PropertyType);
                } catch (Exception e) {
                    dumpDestination.WriteLine($"{e.GetBaseException().Message}");
                }
            }

            foreach (MethodInfo methodInfo in type.GetMethods()) {
                if (methodInfo.Name.StartsWith("Get") && methodInfo.GetParameters().Length == 0) {
                    try {
                        for (int i = 0; i < indents; ++i)
                            dumpDestination.Write("\t");
                        dumpDestination.Write($"{methodInfo.Name}:\t");
                        var result = methodInfo.Invoke(obj, null);
                        DumpValue(result, methodInfo.ReturnType);
                    } catch (Exception e) {
                        dumpDestination.WriteLine($"{e.GetBaseException().Message}");
                    }
                }
            }
            dumpDestination.WriteLine();
        }
    }

    class Init {

        private static bool shouldLoaderStop = false;

        public static void BurdenUp() {
            while (!shouldLoaderStop) {
                var targetText = new byte[4000];
                using (var stream = File.Open("/dev/urandom", FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, false))
                    {
                        for (int i = 0; i < targetText.Length; ++i)
                        {
                            targetText[i] = reader.ReadByte();
                        }
                    }
                }
                var encryptedText = System.Convert.ToBase64String(targetText);
                Console.WriteLine($"Encrypted: {encryptedText}");
                var decryptedText = System.Text.Encoding.ASCII.GetString(System.Convert.FromBase64String(encryptedText));
                Console.WriteLine($"Decrypted: {decryptedText}");
            }
        }

        private static void ShowHelp(string[] args) {
            Console.WriteLine($"Usage: SelfProfiler -c|--count [times] -d|--delay [seconds] file");
            Console.WriteLine(
                "\t-c, --count        Set the number of measures, default inf\n"
                + "\t-d, --delay        Set the number of seconds between measures\n"
                + "\t-h, ?, --help      Print this help message and exit\n"
                + "\tfile               The output file of the logs");
            System.Environment.Exit(0);
        }

        public static int Main(string[] args) {
            int count = 0;
            int delay = 0;
            OptionSet getOpt = new OptionSet()
                .Add("c|count:", (int value) => { count = value; })
                .Add("d|delay:", (int value) => { delay = value; })
                .Add("h|?|help", value => ShowHelp(args));
            List<string> rest = getOpt.Parse(args);
            if (rest is null || rest.Capacity == 0)
                ShowHelp(args);

            string outputFile = rest[0];
            using (TextWriter dumpDestination = outputFile.Equals("-")? Console.Out : new StreamWriter(outputFile)) {
                ObjectDumper processDumper = new ObjectDumper(dumpDestination);
                dumpDestination.WriteLine("---   Common Info    ---");
                processDumper.DumpGetters(null, typeof(System.Environment));

                Thread loader = new Thread(new ThreadStart(Init.BurdenUp));
                loader.Start();
                for (int i = 0; i < count; ++i) {
                    var self = Process.GetCurrentProcess();
                    var sampler = new UsageSampler(self);
                    dumpDestination.WriteLine("---     Process Info      ---");
                    processDumper.DumpGetters(self, self.GetType());
                    Thread.Sleep(delay * 1000);
                    sampler.Stamp(Process.GetCurrentProcess());
                    dumpDestination.WriteLine($"CPU: {sampler.GetCPUUsage()}%; MEM: {sampler.MemoryUsage}MB");
                }
                shouldLoaderStop = true;
                loader.Join();
            }
            return 0;
        }
    }
}