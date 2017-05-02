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
            Server server = null;
            
            using (server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, "127.0.0.1", 27015, false, 1000, 1000, 1, false))
            {
                var serverInfo = server.GetInfo();
                var serverRules = server.GetRules();
                var playerInfo = server.GetPlayers();
            }

            try
            {
                server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27020), false, 1000, 10000, 1, false);
                server.GetControl("password");

                while (!Console.KeyAvailable)
                {
                    try
                    {
                        var st = Stopwatch.StartNew();
                        var result = await server.Rcon.SendCommandAsync("serverchat test");
                        st.Stop();
                        Debug.WriteLine($"{DateTime.Now:HH:mm:ss.ffff}: Command sent and returned {result ?? "[null]"} (elapsed {st.ElapsedMilliseconds:N0} ms)");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw;
                    }
                    await Task.Delay(60000);
                }
            }
            finally
            {
                server?.Dispose();
            }
        }
    }
}
