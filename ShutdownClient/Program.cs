﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
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


        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }


    class Program
    {
        static void Main(string[] args)
        {
            var options = new ServerOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                Console.WriteLine("Server name: {0}", options.Server);
                Console.WriteLine("Server port: {0}", options.Port);

            }
            else
                return;

            var m = new ShutdownMessage.ShutdownMessage
            {
                ShutdownOption = options.ShutdownOption,
                Message = options.Message,
                Key = options.Key
            };

            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                TcpClient client = new TcpClient(options.Server, options.Port);

                string json= JsonConvert.SerializeObject(m);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(json);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }
    }
}
