using System.Runtime.CompilerServices;

namespace Comandas.Orders.IntegrationTests;

internal static class TestcontainersConfiguration
{
    /// <summary>Label put on every container these tests start, to find orphans if a run is interrupted.</summary>
    public const string ContainerLabel = "comandas.tests";

    /// <summary>
    /// Runs before any other code in this assembly. Disables Ryuk (Testcontainers' cleanup sidecar) for this
    /// project only, so no extra image is downloaded. Containers are removed by the fixture's DisposeAsync;
    /// if a run is killed, remove leftovers with:
    /// <c>docker rm -f $(docker ps -aq --filter label=comandas.tests=true)</c>
    /// </summary>
    [ModuleInitializer]
    internal static void DisableResourceReaper() =>
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
}
