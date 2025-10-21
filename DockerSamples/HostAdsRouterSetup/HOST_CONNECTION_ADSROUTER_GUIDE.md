# Connecting to Host TwinCAT System via ADS Router

This guide shows how to connect Docker containers to a TwinCAT/ADS server running on the Docker host machine using **plain ADS routing** with AdsRouterConsole.

## Overview

This setup demonstrates bidirectional ADS communication between Docker containers and a host TwinCAT system:

- **Host System:** Beckhoff RT Linux (tc31-xar-um) running TwinCAT with a PLC program port 851
- **Container Router:** AdsRouterConsole providing ADS routing services
- **Container Clients:**
  - AdsClient (automated monitoring)
  - PowerShell client (interactive testing)
- **Host NetID Detection:** Automatically retrieved using `tcadstool`

**Prerequisites:**
- [Docker and Docker Compose installed on the host](https://docs.docker.com/engine/install/debian/)
- Make utility: `sudo apt install --yes make`
- [Example TwinCAT project](../../../TwinCAT%20Project/TwinCAT%20Project/) running on the host
- User must be in the `docker` group or use `sudo` for Docker commands

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Beckhoff RT Linux Host                                         │
│  tc31-xar-um                                                    │
│  NetId: 5.111.241.147.1.1                                       │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  TwinCAT ADS Server (Port 851)                             │ │
│  │  Static Route: 43.43.43.43.1.1 → 192.168.21.2              │ │
│  └──────────────────────┬─────────────────────────────────────┘ │
│                         │                                       │
│                         │ Docker Gateway (192.168.21.1)         │
│                         │                                       │
│  ┌──────────────────────┼─────────────────────────────────────┐ │
│  │ Docker Network       │  br-tcads-router (192.168.21.0/24)  │ │
│  │                      │                                     │ │
│  │                      |                                     │ │
│  │      ┌────────────────────────────────┐                    │ │
│  │      │    AdsRouterConsole            │                    │ │
│  │      │    192.168.21.2:48900          │                    │ │
│  │      │    NetID: 43.43.43.43.1.1      │                    │ │
│  │      └────────────┬───────────────────┘                    │ │
│  │                   │                                        │ │
│  │         ┌─────────┴──────────┐                             │ │
│  │         │                    │                             │ │
│  │         |                    |                             │ │
│  │  ┌──────────────┐     ┌──────────────┐                     │ │
│  │  │  AdsClient   │     │  PwshClient  │                     │ │
│  │  │ 192.168.21.4 │     │ 192.168.21.5 │                     │ │
│  │  │              │     │              │                     │ │
│  │  │ Automated    │     │ Interactive  │                     │ │
│  │  │ monitoring   │     │ PowerShell   │                     │ │
│  │  │ (reads state │     │ testing      │                     │ │
│  │  │  every 1s)   │     │              │                     │ │
│  │  └──────────────┘     └──────────────┘                     │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```



## Step-by-Step Setup

### Step 1: Understand the Configuration

This scenario uses pre-configured files in the repository:

**[config-host-adsrouter.env](../config/config-host-adsrouter.env)** - Configures the ADS router to:
- Use plain ADS protocol (not MQTT)
- Listen on custom loopback port `192.168.21.2:48900`
- Accept connections from subnet `192.168.21.0/24`
- Use virtual NetID `43.43.43.43.1.1`
- **Define remote connection to Docker host** at gateway IP `192.168.21.1` with host's actual NetID

**[docker-compose.host-adsrouter.yml](../docker-compose.host-adsrouter.yml)** - Creates:
- **AdsRouterConsole** at `192.168.21.2` - Routes ADS traffic between containers and host
- **AdsClient** at `192.168.21.4` - Automated monitoring client (reads host TwinCAT PLC state every second on port 851)
- **PowerShell client** at `192.168.21.5` - Interactive testing client for manual ADS operations
- **Docker bridge network** `br-tcads-router` with subnet `192.168.21.0/24`

**Key Configuration Requirements (Bidirectional Routing):**

```
┌─────────────────────────────────────────────────────────────┐
│ Host TwinCAT (5.111.241.147.1.1)                            │
│                                                             │
│ Route: 43.43.43.43.1.1 @ 192.168.21.2                       │
│ (configured via AdsRouteToDocker.xml)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Docker Gateway: 192.168.21.1
                       │
┌──────────────────────|──────────────────────────────────────┐
│ Container Router (43.43.43.43.1.1 @ 192.168.21.2)           │
│                                                             │
│ RemoteConnection[0]:                                        │
│   Name: DockerHostTwinCAT                                   │
│   Address: 192.168.21.1                                     │
│   NetID: 5.111.241.147.1.1                                  │
│ (configured via config-host-adsrouter.env)                  │
└─────────────────────────────────────────────────────────────┘
```

The key insights:
1. **Host → Container:** The host TwinCAT system needs an ADS route pointing to the container router's NetID (43.43.43.43.1.1) at the virtual ADS router's IP address (192.168.21.2)
2. **Container → Host:** The container router needs a remote connection configured to reach the host at the Docker gateway IP (192.168.21.1) with the host's actual NetID

### Step 2: Clone Repository on Host Machine

On your **Beckhoff RT Linux tc31-xar-um** host, clone this repository:

```bash
ssh Administrator@BTN-000s6dhd
cd ~
git clone <repository-url>
cd AAG_Beckhoff_RT_Linux_ContainersExamples/DockerSamples/HostAdsRouterSetup
```

### Step 3: Configure Host Static Route

**Using Makefile (recommended):**

The Makefile provides a single command to configure everything:

```bash
make configure-host
```

This command will:
1. **Install ADS route configuration** - Copies `config/AdsRouteToDocker.xml` to `/etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml` with a route from the host to the container router (43.43.43.43.1.1 @ 192.168.21.2)
2. **Configure the firewall** - Creates `/etc/nftables.conf.d/60-ads-docker.conf` to allow unsecure ADS traffic from the Docker network bridge `br-tcads-router` on ports 48898/tcp and 48899/udp
3. **Restart TwinCAT** - Applies the route changes

The command will prompt you before overwriting any existing files.

<details>
<summary><b>Manual configuration (if you prefer not to use Makefile)</b></summary>

**Install ADS route configuration:**
```bash
sudo cp config/AdsRouteToDocker.xml /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml
```

This route file contains:
- **Name:** DockerContainerNetwork
- **Address:** 192.168.21.2
- **NetId:** 43.43.43.43.1.1
- **Type:** TCP_IP

**Configure firewall:**
```bash
echo 'table inet filter {
  chain input {
    # Accept ADS traffic from Docker network
    iifname "br-tcads-router" tcp dport 48898 accept
    iifname "br-tcads-router" udp dport 48899 accept
  }
}' | sudo tee /etc/nftables.conf.d/60-ads-docker.conf
sudo systemctl reload nftables
```

**Restart TwinCAT:**
```bash
sudo systemctl restart TcSystemServiceUm
```
</details>


### Step 4: Start Docker Containers

**Important:** Ensure your user has Docker permissions. If you get permission errors:
```bash
# Add your user to the docker group (one-time setup)
sudo usermod -aG docker $USER
# Then logout and login again

# Or use sudo with make commands
sudo make up
```

The Makefile automatically retrieves your host's AMS NetID using `tcadstool` and starts the containers:

```bash
# See all available commands
make help

# Start containers in detached mode (recommended)
make up-d

# Or start with logs in foreground
make up
```

The `make up` command will:
- Retrieve the host AMS NetID automatically
- Build the Docker images (router, AdsClient, and PowerShell client)
- Start the AdsRouterConsole at `192.168.21.2:48900`
- Start the AdsClient at `192.168.21.4` (begins automated monitoring)
- Start the PowerShell client at `192.168.21.5` with ADS libraries pre-configured

**Monitor the containers:**
```bash
make status           # Check container status
make logs             # View all logs
make logs-router      # View router logs only
make logs-client      # View AdsClient logs only (automated monitoring)
make logs-pwsh        # View PowerShell client logs only
```

### Step 5: Monitor the Automated AdsClient

The AdsClient container automatically starts monitoring your host TwinCAT system upon startup. It connects to port 851 (default PLC runtime port) and reads the ADS state every second.

**View automated monitoring:**
```bash
make logs-client      # See real-time state updates
```

You should see output like:
```
[AdsClient] State of host TwinCAT '5.111.241.147.1.1:851' is: Run
[AdsClient] State of host TwinCAT '5.111.241.147.1.1:851' is: Run
[AdsClient] State of host TwinCAT '5.111.241.147.1.1:851' is: Run
```

This demonstrates continuous ADS communication from the container to the host via the router.

### Step 6: Test Interactive Commands

**Quick test commands:**

```bash
# Test reading MAIN.nCounter from host
make attach-read

# Test writing value 42 to MAIN.nCounter
make attach-write
```

These commands automatically use the detected host AMS NetID and execute read/write operations via the ADS router.

**Interactive PowerShell session:**

For more advanced testing, attach to the PowerShell client:

```bash
make attach
```

Then run PowerShell commands manually:

```powershell
# Check ADS state (verify connection via router)
PS> New-TcSession -NetId "5.111.241.147.1.1" -Port 851 | Get-AdsState

# Test latencies/roundtrip times
PS> Test-AdsRoute -NetId "5.111.241.147.1.1" -Port 851 -count 20

# Read a value
PS> (New-TcSession -NetId '5.111.241.147.1.1' -Port 851 | Get-TcSymbol -Path "MAIN.nCounter") | Read-TcValue

# Write a value
PS> (New-TcSession -NetId '5.111.241.147.1.1' -Port 851 | Get-TcSymbol -Path "MAIN.nCounter") | Write-TcValue -Value 42 -Force

# Exit interactive session
PS> exit
```


## Understanding the Two Client Containers

This setup includes two different client containers with distinct purposes:

### AdsClient (192.168.21.4) - Automated Monitoring

**Purpose:** Continuous automated verification of ADS connectivity

**Technology:**
- .NET 8.0 console application
- Uses TwinCAT.Ads SDK directly
- Implements BackgroundService for long-running operation

**Behavior:**
- Connects to host TwinCAT on port 851 at startup
- Reads ADS device state every 1 second
- Logs each state read to console and logger
- Runs continuously until container is stopped

**Use cases:**
- Verify bidirectional routing is working
- Monitor connection stability over time
- Demonstrate programmatic ADS client implementation
- Serve as reference code for .NET ADS applications

**View logs:**
```bash
make logs-client
```

### PwshClient (192.168.21.5) - Interactive Testing

**Purpose:** Manual, interactive ADS operations for testing and debugging

**Technology:**
- PowerShell 7.x with TcXaeMgmt module
- Interactive shell environment
- Rich cmdlet library for TwinCAT operations

**Behavior:**
- Starts PowerShell prompt and waits for user input
- Provides full access to TcXaeMgmt cmdlets
- Allows reading/writing PLC variables, browsing symbols, etc.
- Ideal for ad-hoc testing and exploration

**Use cases:**
- Manual read/write operations
- Symbol browsing and exploration
- Latency testing with `Test-AdsRoute`
- Quick validation during development

**Access shell:**
```bash
make attach
```

**Quick commands:**
```bash
make attach-read    # Read MAIN.nCounter
make attach-write   # Write value 42
```

Both clients route through the same AdsRouterConsole at 192.168.21.2, demonstrating different approaches to ADS communication from containers.


## Stopping the Containers

```bash
make down       # Stop and remove containers
make clean      # Stop, remove containers, and delete volumes
```

## Key Differences from ADS-over-MQTT

| Aspect | Plain ADS (This Guide) | ADS-over-MQTT |
|--------|------------------------|---------------|
| Routing | Requires AdsRouterConsole | Uses MQTT broker |
| Static Routes | Required on host | Not needed |
| Port Configuration | Custom loopback port (48900) | Standard MQTT (1883) |
| Discovery | Manual route configuration | Automatic via broker |
| Latency | Lower (direct ADS) | Higher (MQTT overhead) |
| Network Flexibility | Same network only | Works across NAT |


## When to Use ADS Router vs MQTT

**Use Plain ADS Router (this guide) when:**
- All devices are on the same local network or can reach via Docker bridge
- You need lowest possible latency
- You have full control over routing configuration

**Use ADS-over-MQTT when:**
- Devices are across different networks or NAT boundaries
- You want simplified configuration without static routes
- You need flexibility in network topology

## Performance Comparison

Typical latency measurements (from container to host TwinCAT on same machine):

| Method | Avg Latency | Notes |
|--------|-------------|-------|
| Plain ADS Router | 1-5ms | This guide |
| ADS-over-MQTT | 40-50ms | Higher due to MQTT serialization |

Use `Test-AdsRoute` in PowerShell to measure your actual latency:
```powershell
PS> Test-AdsRoute -NetId "5.111.241.147.1.1" -Port 851 -count 100
```


## Disclaimer 

All sample code provided by Beckhoff Automation LLC are for illustrative purposes only and are provided “as is” and without any warranties, express or implied. Actual implementations in applications will vary significantly. Beckhoff Automation LLC shall have no liability for, and does not waive any rights in relation to, any code samples that it provides or the use of such code samples for any purpose.