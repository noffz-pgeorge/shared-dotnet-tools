using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noffz.Processes.Types
{
    public sealed class ProcessRunResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public bool TimedOut { get; set; } = false;
    }
}
