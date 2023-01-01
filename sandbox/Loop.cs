using System;
using System.Threading;


public class Loop {
    static void Main(string[] args) {
        while (true) {
            Console.WriteLine("It's time to sleep...ZzzZz");
            Thread.Sleep(1000);
        }
    }
}