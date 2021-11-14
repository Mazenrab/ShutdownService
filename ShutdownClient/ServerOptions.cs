using CommandLine;
using CommandLine.Text;
using ShutdownMessage;

namespace ShutdownClient
{
    public class ServerOptions
    {
        [Option("s", Required = true, HelpText = "Server name")]
        public string Server { get; set; }

        [Option("p", Required = true, HelpText = "Server port")]
        public int Port { get; set; }

        [Option("k", Required = true, HelpText = "Key")]
        public string Key { get; set; }

        [Option("m", DefaultValue = "", HelpText = "Message")]
        public string Message { get; set; }

        [Option("a", DefaultValue = ShutdownOption.Shutdown, HelpText = "Message")]
        public ShutdownOption ShutdownOption { get; set; }

        [Option("p", DefaultValue = false, HelpText = "Pause")]
        public bool Pause { get; set; }
        
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }
}