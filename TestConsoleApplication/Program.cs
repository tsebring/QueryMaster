using Nito.AsyncEx;
using QueryMaster.GameServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync());
        }

            static async Task MainAsync()
        {
            var rnd = new Random();
            Server server = null;

            using (server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, "127.0.0.1", 27015, false, 1000, 1000, 1, false))
            {
                var serverInfo = server.GetInfo();
                var serverRules = server.GetRules();
                var playerInfo = server.GetPlayers();
            }

            try
            {
                server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27020), false, 3000, 10000, 1, true);
                server.GetControl("password");

                while (!Console.KeyAvailable)
                {
                    var nextDelay = rnd.Next(60, 60 * 30) * 1000;
                    var st = Stopwatch.StartNew();
                    try
                    {
                        
                        var result = await server.Rcon.SendCommandAsync("listplayers");
                        if (result != null) Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff}: Command sent and returned {result.Trim(' ', '\n', '\r')} (elapsed {st.ElapsedMilliseconds:N0} ms) [next in {TimeSpan.FromMilliseconds(nextDelay)}]");
                        else Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff}: Command sent and failed with result \"[null]\" (elapsed {st.ElapsedMilliseconds:N0} ms) [next in {TimeSpan.FromMilliseconds(nextDelay)}]");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff}: Command sent and failed with exception {ex.ToString()} (elapsed {st.ElapsedMilliseconds:N0} ms) [next in {TimeSpan.FromMilliseconds(nextDelay)}]");
                    }
                    await Task.Delay(nextDelay);
                }
            }
            finally
            {
                server?.Dispose();
            }
        }
    }
}
