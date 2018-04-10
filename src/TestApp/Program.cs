using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using SCTP;
using Newtonsoft.Json;
using TestApp.Models;

namespace TestApp
{
    class Program
    {
        private static StreamStat[] streams;

        static void Main(string[] args)
        {
            try
            {
                IPAddress address = GetFirstActiveAddress();

                SCTPSocket local = new SCTPSocket(9100);
                local.Bind(address);
                Console.WriteLine("[{0}] Local", local.GetHashCode());


                SCTPSocket remote = new SCTPSocket(9200);
                remote.Bind(address);
                remote.MessageReceived += Remote_MessageReceived;
                Console.WriteLine("[{0}] Remote", remote.GetHashCode());

                streams = new StreamStat[100];
                for (int i = 0; i < streams.Length; i++)
                {
                    streams[i] = new StreamStat() { StreamId = i + 1 };
                }

                while (true)
                {
                    string[] cmd = Console.ReadLine().Split(' ');
                    if (cmd[0] == "q")
                    {
                        break;
                    }
                    else if (cmd[0] == "c")
                    {
                        if (local.Connect(address, 9200) == true)
                        {
                            Console.WriteLine("Local Connected: {0}", local.Connected);
                            Console.WriteLine("Remote Connected: {0}", remote.Connected);
                        }
                    }
                    else if (cmd[0].StartsWith("s"))
                    {
                        string streamStr = cmd[0].Substring(1);
                        int streamId = string.IsNullOrEmpty(streamStr) == false ? Convert.ToInt32(streamStr) : 1;
                        int count = cmd.Length > 1 ? Convert.ToInt32(cmd[1]) : 1;

                        for (int i = 0; i < count; i++)
                        {
                            streams[streamId - 1].IncrementCounter();

                            var model = new TestModel
                            {
                                Data = $"Hello World-{streams[streamId - 1].Counter}"
                            };

                            model.Hash = Convert.ToBase64String(SHA1.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(model.Data)));
                            var json = JsonConvert.SerializeObject(model);

                            local.SendMessageAsync(streamId, UTF8Encoding.UTF8.GetBytes(json)).Wait();
                        }
                    }
                    else if (cmd[0].StartsWith("a"))
                    {
                        string streamStr = cmd[0].Substring(1);
                        int streamId = string.IsNullOrEmpty(streamStr) == false ? Convert.ToInt32(streamStr) : 1;
                        int count = cmd.Length > 1 ? Convert.ToInt32(cmd[1]) : 1;

                        streams[streamId-1].IncrementCounter();

                        var model = new TestModel();

                        for (int i = 0; i < count; i++)
                        {
                            model.Data += "Hello World";
                        }

                        model.Data += $"-{streams[streamId-1].Counter}";
                        model.Hash = Convert.ToBase64String(SHA1.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(model.Data)));

                        var json = JsonConvert.SerializeObject(model);
                        local.SendMessageAsync(streamId, UTF8Encoding.UTF8.GetBytes(json)).Wait();
                    }
                    else if (cmd[0] == "d")
                    {
                        local.Shutdown();

                        Console.WriteLine("Local Connected: {0}", local.Connected);
                        Console.WriteLine("Remote Connected: {0}", remote.Connected);
                    }
                    else if (cmd[0] == "ua")
                    {
                        var chunks = local.GetUnackedChunks();
                        foreach (var chunk in chunks)
                        {
                            Console.WriteLine("Unacked Chunk: {0}", chunk.TSN);
                        }
                    }
                    else if (cmd[0] == "prws")
                    {
                        Console.WriteLine("Peer Receive Window Size: {0}", local.PeerReceiveWindowSize);
                    }
                    else if (cmd[0] == "mtu")
                    {
                        Console.WriteLine("Path MTU Size: {0}", local.MTUSize);
                    }
                }

                local.Dispose();
                remote.Dispose();

                Console.WriteLine("Local Connected: {0}", local.Connected);
                Console.WriteLine("Remote Connected: {0}", remote.Connected);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}", ex.ToString());
                Console.ResetColor();
            }


        }

        static void Remote_MessageReceived(object sender, SCTPMessageReceivedEventArgs e)
        {
            byte[] data = new byte[e.Message.Stream.Length];
            e.Message.Stream.Read(data, 0, data.Length);

            var model = JsonConvert.DeserializeObject<TestModel>(UTF8Encoding.UTF8.GetString(data));
            var hash = Convert.ToBase64String(SHA1.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(model.Data)));

            Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine("Message: {0}", e.Message.StreamSequenceNo);
            //Console.WriteLine("[{0}] --------------------------------------------------------------------------------", sender.GetHashCode());
            //Console.WriteLine("[{0}] Message received:", sender.GetHashCode());
            //Console.WriteLine("[{0}] {1}", sender.GetHashCode(), model.Data);
            //Console.WriteLine("[{0}] {1}", sender.GetHashCode(), model.Hash);
            //Console.WriteLine("[{0}] Consistent: {1}", sender.GetHashCode(), hash.Equals(model.Hash));
            //Console.WriteLine("[{0}] --------------------------------------------------------------------------------", sender.GetHashCode()); 
            //Console.ResetColor();
        }

        private static void TimerExpired(object state)
        {
            Console.WriteLine("Timer Expired @ {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }

        private static IPAddress GetFirstActiveAddress()
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus == OperationalStatus.Up)
                {
                    return iface.GetIPProperties().UnicastAddresses.First((ip) => { return ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork; }).Address;
                }
            }

            return null;
        }
    }
}
