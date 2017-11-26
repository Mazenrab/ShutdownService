using CommandLine;
using CommandLine.Text;

namespace ShutdownServiceServer
{
    public class ServerOptions
    {
        [Option("p", Required = true, HelpText = "Server port")]
        public int Port { get; set; }

        [Option("k", Required = true, HelpText = "Key")]
        public string Key { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }
}