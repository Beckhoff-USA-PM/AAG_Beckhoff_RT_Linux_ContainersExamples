using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text;
using TwinCAT.Ads;
using TwinCAT.Ads.Configuration;
using TwinCAT.Ams;

namespace Client
{
    /// <summary>
    /// The ClientService instance represents a long running (hosted) service that implements an <see cref="AdsClient"/> communicating with the docker SymbolicTestServer.
    /// Implements the <see cref="BackgroundService" />
    /// </summary>
    /// <remarks>
    /// Long running Background task that runs a <see cref="AmsTcpIpRouter."/>.
    /// The service is stopped via the <see cref="CancellationToken"/> given to the <see cref="ExecuteAsync(CancellationToken)"/> method.
    /// </remarks>
    /// <seealso cref="BackgroundService" />
    public class ClientService : BackgroundService
    {
        private readonly ILoggerFactory? _loggerFactory;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<ClientService>? _logger;
        /// <summary>
        /// Configuration
        /// </summary>
        private readonly IConfiguration? _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientService"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        public ClientService(IConfiguration? configuration, ILoggerFactory? loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ClientService>();
            _configuration = configuration;
            
            // The configuration has to be told to the TwinCAT.Ads Framework.
            // Setting the configuration statically so that the local AmsNetId
            // (AmsNetId.Local) is found!
            //GlobalConfiguration.Configuration = _configuration;
            //string? value = _configuration?.GetValue("ASPNETCORE_ENVIRONMENT", "Production");

            // Dumps the configuration
            if (_configuration != null)
            {
                foreach(var keyValue in _configuration.AsEnumerable())
                {
                    _logger?.LogDebug($"{keyValue.Key} {keyValue.Value}");
                }
            }
        }

        /// <summary>
        /// Execute the Router asynchronously as <see cref="BackgroundService"/>.
        /// </summary>
        /// <param name="cancel">The cancellation token.</param>
        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            using (_logger.BeginScope("Starting"))
            {
                // // Read the Router Settings from the actual configuration (here set by Environment Variables)
                AmsRouterConfiguration? routerSettings = ConfigurationBinder.Get<TwinCAT.Ads.Configuration.AmsRouterConfiguration>(_configuration);

                if (routerSettings?.AmsRouter?.NetId != null)
                {
                    _logger.LogInformation("RouterName  : {Router}", routerSettings.AmsRouter.Name);
                    _logger.LogInformation("LocalNetID  : {NetId}", routerSettings.AmsRouter.NetId);
                    _logger.LogInformation("LoopbackIP  : {IP}", routerSettings.AmsRouter.LoopbackIP);
                    _logger.LogInformation("LoopbackPort: {Port}", routerSettings.AmsRouter.LoopbackPort);

                    //TODO: AmsConfiguration has still to be set before Accessing AmsNetId.Local
                    // if (routerSettings != null)
                    // {
                    //     IPAddress? loopback;
                    //     int loopbackPort = routerSettings.AmsRouter.LoopbackPort;
                    //     bool ok = IPAddress.TryParse(routerSettings.AmsRouter.LoopbackIP, out loopback);

                    //     if (ok)
                    //     {f
                    //         AmsConfiguration.RouterEndPoint = new IPEndPoint(loopback, loopbackPort);
                    //     }
                    // }
                }
                if (routerSettings?.AmsRouter?.Mqtt != null)
                {
                    // Use Mqtt configuration
                    _logger.LogInformation("Address : {Address}", routerSettings.AmsRouter.Mqtt[0].Address);
                    _logger.LogInformation("Port    : {Port}", routerSettings.AmsRouter.Mqtt[0].Port);
                    _logger.LogInformation("Topic   : {Topic}", routerSettings.AmsRouter.Mqtt[0].Topic);
                }
            }

            // Get the host AMS NetID from environment variable
            string? hostNetIdStr = Environment.GetEnvironmentVariable("HOST_AMS_NETID");

            if (string.IsNullOrEmpty(hostNetIdStr))
            {
                _logger.LogError("HOST_AMS_NETID environment variable is not set!");
                throw new InvalidOperationException("HOST_AMS_NETID environment variable is required");
            }

            AmsNetId hostNetId = new AmsNetId(hostNetIdStr);
            _logger.LogInformation("Host AMS NetID: {HostNetId}", hostNetId);

            // Connect to the host TwinCAT system on port 851 (default PLC runtime port)
            AmsAddress address = new AmsAddress(hostNetId, 851);
            _logger.LogInformation("Connecting to host TwinCAT at '{Address}'", address);

            // Create Session/Client connection to this Server
            using (AdsSession session = new AdsSession(address, SessionSettings.Default, _configuration, _loggerFactory, this))
            {
                var connection = (IAdsConnection)session.Connect();

                do
                {
                    ResultReadDeviceState readStateResult = await connection.ReadStateAsync(cancel);
                    string message = $"[AdsClient] State of host TwinCAT '{address}' is: {readStateResult.State.AdsState}";
                    Console.WriteLine(message); // Print to console
                    _logger?.LogInformation(message); // Log to Logger

                    await Task.Delay(TimeSpan.FromSeconds(1)); // Delay 1 Second

                } while (!cancel.IsCancellationRequested);

                session.Close();
            }
            _logger?.LogInformation("Client Service stopped!");
        }
    }
}
