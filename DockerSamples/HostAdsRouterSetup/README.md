# TwinCAT ADS Router Docker Setup

Connect to a TwinCAT system running on a Docker host machine using plain ADS routing with AdsRouterConsole.

## Quick Start

1. **Prerequisites**
   - Docker and Docker Compose installed on Debian host
   - TwinCAT running on the host
   - `tcadstool` available on the host
   - User must be in the `docker` group or use `sudo` for Docker commands

2. **Configuration**

   The configuration in `config/config-host-adsrouter.env` includes:
   - Network settings (subnet, gateway, IPs)
   - Router configuration (NetID, loopback port)
   - **Remote connection to Docker host** (auto-configured with HOST_AMS_NETID)

   Network defaults (edit if needed):
   ```bash
   DOCKER_SUBNET=192.168.21.0/24
   DOCKER_GATEWAY=192.168.21.1
   ROUTER_IP=192.168.21.2
   PWSH_CLIENT_IP=192.168.21.5
   ```

   The router is pre-configured to connect to the Docker host at gateway IP `192.168.21.1`.

3. **Configure host ADS route**
   ```bash
   make configure-host
   ```

   This installs an ADS route from the host TwinCAT system to the container router.

4. **Start the containers**
   ```bash
   make up
   ```

   The Makefile automatically retrieves your host's AMS NetID using `tcadstool`.

5. **Test the connection**
   ```bash
   make attach
   ```

## Available Commands

```bash
make help             # Show all available commands
make configure-host   # Configure host ADS route (one-time setup)
make up               # Start containers (auto-retrieves host AMS NetID)
make down             # Stop containers
make logs             # View all logs
make logs-router      # View ADS router logs only
make logs-client      # View PowerShell client logs only
make attach           # Attach to PowerShell client
make attach-read      # Test reading MAIN.nCounter from host
make attach-write     # Test writing value 42 to MAIN.nCounter
make check-netid      # Check if host AMS NetID can be retrieved
```

## Directory Structure

```
HostAdsRouterSetup/
├── README.md                              # This file
├── docker-compose.host-adsrouter.yml      # Main Docker Compose configuration
├── Makefile                               # Build and run commands
├── config/                                # Configuration files
│   └── config-host-adsrouter.env          # Environment variables (edit this!)
├── containers/                            # Container definitions
│   ├── router/
│   │   ├── Dockerfile                     # AdsRouterConsole image
│   │   └── src/                           # Router source code
│   │       ├── AdsRouterConsole.csproj
│   │       ├── Program.cs
│   │       └── Worker.cs
│   └── pwsh-client/
│       ├── Dockerfile                     # PowerShell client image
│       └── init.ps1                       # Startup script
├── docs/                                  # Documentation
│   └── HOST_CONNECTION_ADSROUTER_GUIDE.md # Detailed setup guide
└── diagrams/                              # Architecture diagrams
    ├── RouterConsoleConfiguration.svg
    └── RouterConsoleConfiguration.drawio
```

## How It Works

1. Makefile runs `tcadstool 127.0.0.1 netid` to get the host's AMS NetID
2. Docker Compose starts:
   - ADS Router (AdsRouterConsole) on `192.168.21.2:48900`
   - PowerShell client on `192.168.21.5`
3. Host TwinCAT system has an ADS route pointing to the container router (43.43.43.43.1.1 @ 192.168.21.2)
4. PowerShell client connects to host TwinCAT system via the router
5. Containers restart automatically on reboot (`restart: unless-stopped`)

## Network Architecture

```
Host TwinCAT System (e.g., 5.111.241.147.1.1)
         |
         | ADS Route: 43.43.43.43.1.1 @ 192.168.21.2
         |
    [ADS Router Container]
    192.168.21.2:48900
    NetID: 43.43.43.43.1.1
         |
         | ADS Protocol
         |
    [PowerShell Client Container]
    192.168.21.5
```

## Documentation

See [docs/HOST_CONNECTION_ADSROUTER_GUIDE.md](docs/HOST_CONNECTION_ADSROUTER_GUIDE.md) for detailed setup instructions.

## Troubleshooting

- **Docker permission denied**: Add your user to the docker group: `sudo usermod -aG docker $USER` then logout/login, or use `sudo make up`
- **Can't retrieve AMS NetID**: Ensure TwinCAT is running and `tcadstool` is installed
- **ADS connection fails**: Check that ADS route is configured on host (`tcadstool 127.0.0.1 routes`)
- **Network conflicts**: Edit IP addresses in `config/config-host-adsrouter.env`
- **Router not accessible**: Check firewall rules allow ports 48898/tcp and 48899/udp from Docker network
- **Route file location**: Route is installed at `/etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml`
