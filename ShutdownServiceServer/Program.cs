using System;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace ShutdownServiceServer
{
    class Program
    {
        private static string Key;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool ExitWindowsEx(int flg, int rea);

        public const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const int TOKEN_QUERY = 0x00000008;
        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        public const int EWX_LOGOFF = 0x00000000;
        public const int EWX_SHUTDOWN = 0x00000001;
        public const int EWX_REBOOT = 0x00000002;
        public const int EWX_FORCE = 0x00000004;
        public const int EWX_POWEROFF = 0x00000008;
        public const int EWX_FORCEIFHUNG = 0x00000010;


        public static bool DoExitWin(int flg)
        {
            TokPriv1Luid tp;
            var hproc = GetCurrentProcess();
            var htok = IntPtr.Zero;
            OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
            tp.Count = 1;
            tp.Luid = 0;
            tp.Attr = SE_PRIVILEGE_ENABLED;
            LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
            AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);

            return ExitWindowsEx(flg, 0);
        }

        static void Main(string[] args)
        {

            var options = new ServerOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                Console.WriteLine("Server port: {0}", options.Port);
                Console.WriteLine("Key: {0}", options.Key);
                Key = options.Key;
            }
            else
                return;

            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = options.Port;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                
                // TcpListener server = new TcpListener(port);
                server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests.
                server.Start();

                string data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Buffer for reading data
                    byte[] bytes = new Byte[4096];
                    byte[] result = new byte[0];

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        int oldLength = result.Length;
                        var tmp = new byte[oldLength + i];
                        Buffer.BlockCopy(result, 0, tmp, 0, oldLength);
                        Buffer.BlockCopy(bytes, 0, tmp, oldLength, i);
                        result = tmp;

                        //// Translate data bytes to a ASCII string.
                        //data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        //Console.WriteLine("Received: {0}", data);

                        //// Process the data sent by the client.
                        //data = data.ToUpper();

                        //byte[] msg = System.Text.Encoding.UTF8.GetBytes(data);

                        //// Send back a response.
                        //stream.Write(msg, 0, msg.Length);
                        //Console.WriteLine("Sent: {0}", data);
                    }

                    // Shutdown and end connection
                    client.Close();

                    // Parse
                    data = ParseBytes(result);
                    Console.WriteLine($"Message: {data}");

                    var message = JsonConvert.DeserializeObject<ShutdownMessage.ShutdownMessage>(data);

                    bool keyMatch = Key.Equals(message.Key);
                    
                    Console.WriteLine($"Key match is {keyMatch}");


                    if(message.ShutdownOption == ShutdownMessage.ShutdownOption.Shutdown)
                    {
                        try
                        {
                            DoExitWin(EWX_SHUTDOWN | EWX_FORCE);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"{ex.ToString()}");
                        }
                    }

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server?.Stop();
            }
        }

        private static string ParseBytes(byte[] bytes)
        {
            string data = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return data;
        }

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }


        private static void Shutdown()
        {
            ManagementBaseObject outParameters = null;
            ManagementClass sysOS = new ManagementClass("Win32_OperatingSystem");
            sysOS.Get();
            // enables required security privilege.
            sysOS.Scope.Options.EnablePrivileges = true;
            // get our in parameters
            ManagementBaseObject inParameters = sysOS.GetMethodParameters("Win32Shutdown");
            // pass the flag of 0 = System Shutdown
            inParameters["Flags"] = "1";
            inParameters["Reserved"] = "0";
            foreach (ManagementObject manObj in sysOS.GetInstances())
            {
                outParameters = manObj.InvokeMethod("Win32Shutdown", inParameters, null);
            }
            //ManagementBaseObject mboShutdown = null;
            //ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            //mcWin32.Get();

            //if (!TokenAdjuster.EnablePrivilege("SeShutdownPrivilege", true))
            //{
            //    Console.WriteLine("Could not enable SeShutdownPrivilege");
            //}
            //else
            //{
            //    Console.WriteLine("Enabled SeShutdownPrivilege");
            //}

            //// You can't shutdown without security privileges
            //mcWin32.Scope.Options.EnablePrivileges = true;
            //ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");

            //// Flag 1 means we want to shut down the system
            //mboShutdownParams["Flags"] = "1";
            //mboShutdownParams["Reserved"] = "0";

            //foreach (ManagementObject manObj in mcWin32.GetInstances())
            //{
            //    try
            //    {
            //        mboShutdown = manObj.InvokeMethod("Win32Shutdown",
            //                                       mboShutdownParams, null);
            //    }
            //    catch (ManagementException mex)
            //    {
            //        Console.WriteLine(mex.ToString());
            //        Console.ReadKey();
            //    }
            //}
        }

        private static void Reboot() { }
    }
}
