﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;

namespace UITests
{
    [TestClass]
    public class ConsoleStart
    {
        Process Start(string Arguments)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "captura",
                    Arguments = Arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            void Write(string Data, string Prefix)
            {
                if (string.IsNullOrWhiteSpace(Data))
                    return;

                Trace.WriteLine($"{Prefix}: {Data}");
            }

            process.ErrorDataReceived += (s, e) => Write(e.Data, "Err");
            process.OutputDataReceived += (s, e) => Write(e.Data, "Out");

            return process;
        }
        
        [TestMethod]
        public void StartGif()
        {
            var process = Start("start --encoder gif");

            Thread.Sleep(1000);

            process.StandardInput.WriteLine('q');

            process.WaitForExit();

            Assert.IsTrue(process.ExitCode == 0);
        }        
    }
}
