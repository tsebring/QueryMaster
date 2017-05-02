using QueryMaster.GameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, "127.0.0.1", 27015, false, 1000, 1000, 1, false))
            {
                if (server == null) return;

                var serverInfo = server.GetInfo();
                var serverRules = server.GetRules();
                var playerInfo = server.GetPlayers();
            }

            using (var server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, new IPEndPoint(IPAddress.Parse("62.63.229.45"), 27020), false, 1000, 1000, 1, false))
            {
                server.GetControl("password");
                server.Rcon.SendCommand("serverchat test");
            }
        }
    }
}
