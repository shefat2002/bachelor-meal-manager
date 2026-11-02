using System.Net;
using System.Net.Sockets;

namespace MealManager.IntegrationTests;

/// <summary>
/// Every WebApplicationFactory co-hosts an Orleans silo with localhost clustering; parallel test
/// classes would collide on the 11111/30000 defaults, so each factory grabs OS-assigned free ports.
/// </summary>
internal static class OrleansTestPorts
{
    public static (int SiloPort, int GatewayPort) Get()
    {
        int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        return (FreePort(), FreePort());
    }
}
