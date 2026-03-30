using Noffz.Processes.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noffz.Processes
{
    public static class ProcessRunner
    {
        public static ProcessRunResult RunExe(string exePath, string arguments = "", string workingDirectory = null, int? timeoutMilliseconds = null)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false, //required for redirection
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true //prevent console popup
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exited = timeoutMilliseconds.HasValue
                ? process.WaitForExit(timeoutMilliseconds.Value)
                : process.WaitForExit(int.MaxValue);

                ProcessRunResult res = new ProcessRunResult();
                if (!exited) {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    res.ExitCode = -1;
                    res.TimedOut = true;
                    return res;
                } else {
                    res.ExitCode = process.ExitCode;
                }

                res.StandardOutput = output.ToString();
                res.StandardError = error.ToString();

                return res;
            }
        }
    }
}
