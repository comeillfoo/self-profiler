using System.Diagnostics;
using System.Reflection;
using System.Collections;
using Mono.Options;
using NLog;

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
        private NLog.Logger Logger;

        public ObjectDumper(NLog.Logger Logger) {
            this.Logger = Logger;
        }

        private void DumpValue(object? value, Type type, Stack<String> props) {
            var keyName = String.Join(".", props.Reverse());
            if (value is Hashtable) {
                foreach (DictionaryEntry v in (Hashtable) value)
                    Logger.Info("{Parameter:l}[{Key:l}]:\t{}", keyName, v.Key, v.Value);
                return;
            }

            if (value is IEnumerable && value is not IEnumerable<Char> && value is not IEnumerable<Process>) {
                var index = 0;
                var propName = props.Peek();
                foreach (var e in (value as IEnumerable)) {
                    if (e is string || e is String)
                        Logger.Info("{Parameter:l}[{}]:\t{}", keyName, index, e);
                    else {
                        props.Pop();
                        props.Push(String.Format("{0}[{1}]", propName, index));
                        DumpGetters(e, e.GetType(), props);
                    }
                    index++;
                }
                return;
            }

            if (type.Name.StartsWith("FileVersionInfo")) {
                DumpGetters(value, type, props);
                return;
            }
            Logger.Info("{Parameter:l}:\t{}", keyName, value);
        }

        public void DumpGetters(object? obj, Type type, Stack<String> props) {
            foreach (PropertyInfo propInfo in type.GetProperties()) {
                props.Push(propInfo.Name);
                try {
                    var propValue = propInfo.GetValue(obj);
                    DumpValue(propValue, propInfo.PropertyType, props);
                } catch (Exception e) {
                    var keyName = String.Join(".", props.Reverse());
                    Logger.Info("{Parameter:l}:\t{}", keyName, e.GetBaseException().Message);
                } finally {
                    props.Pop();
                }
            }

            foreach (MethodInfo methodInfo in type.GetMethods()) {
                if (methodInfo.Name.StartsWith("Get") && methodInfo.GetParameters().Length == 0) {
                    props.Push(methodInfo.Name);
                    try {
                        var result = methodInfo.Invoke(obj, null);
                        DumpValue(result, methodInfo.ReturnType, props);
                    } catch (Exception e) {
                        var keyName = String.Join(".", props.Reverse());
                        Logger.Info("{Parameter:l}:\t{}", keyName, e.GetBaseException().Message);
                    } finally {
                        props.Pop();
                    }
                }
            }
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

            NLog.LogManager.Setup().LoadConfiguration(builder => {
                if (outputFile.Equals("-"))
                    builder.ForLogger().WriteToConsole();
                else
                    builder.ForLogger().WriteToFile(fileName: outputFile);
            });

            var Logger = NLog.LogManager.GetCurrentClassLogger();
            ObjectDumper processDumper = new ObjectDumper(Logger);
            Logger.Info("Dumping base info...");
            processDumper.DumpGetters(null, typeof(System.Environment), new Stack<String>());

            Thread loader = new Thread(new ThreadStart(Init.BurdenUp));
            loader.Start();
            for (int i = 0; i < count; ++i) {
                var self = Process.GetCurrentProcess();
                var sampler = new UsageSampler(self);
                Logger.Info("Dumping process info...");
                processDumper.DumpGetters(self, self.GetType(), new Stack<String>());
                Thread.Sleep(delay * 1000);
                sampler.Stamp(Process.GetCurrentProcess());
                Logger.Debug("CPU:\t{} %", sampler.GetCPUUsage());
                Logger.Debug("MEM:\t{} MB", sampler.MemoryUsage);
            }
            shouldLoaderStop = true;
            loader.Join();
            return 0;
        }
    }
}