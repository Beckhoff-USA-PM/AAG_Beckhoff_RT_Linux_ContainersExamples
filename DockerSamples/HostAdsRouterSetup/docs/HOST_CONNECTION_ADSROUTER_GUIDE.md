# Connecting to Host TwinCAT System via ADS Router

This guide shows how to connect Docker containers to a TwinCAT/ADS server running on the Docker host machine using **plain ADS routing** with AdsRouterConsole.

## Your Setup

- **Docker Host:** Beckhoff RT Linux with TwinCAT tc31-xar-um and a PLC program running on ADS port 851 with a MAIN.nCounter.
- **Host ADS NetId:** Retrieved automatically (e.g., 5.111.241.147.1.1)
- **Goal:** Run containers with AdsRouterConsole and connect to the host's ADS server
- **User Permissions:** Your user must be in the `docker` group or use `sudo` for Docker commands

## Why AdsRouterConsole?

AdsRouterConsole provides traditional ADS routing:
- **Familiar architecture:** Standard TwinCAT routing model
- **Low latency:** Direct ADS protocol without additional layers
- **Deterministic:** Predictable performance characteristics
- **No external dependencies:** No MQTT broker required

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│  Beckhoff RT Linux                                   │
│                                                      │
│  tc31-xar-um                                         │
│  NetId: 5.111.241.147.1.1                           │
│  ADS Route: 43.43.43.43.1.1 @ 192.168.21.2          │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │ br-tcads-router (192.168.21.0/24)             │ │
│  │                                                │ │
│  │  ┌──────────────────────┐                     │ │
│  │  │ AdsRouterConsole     │                     │ │
│  │  │ 192.168.21.2:48900   │                     │ │
│  │  │ NetID: 43.43.43.43.1.1│                    │ │
│  │  └──────────────────────┘                     │ │
│  │           │                                    │ │
│  │           │ (all ADS traffic routed via       │ │
│  │           │  custom loopback port)            │ │
│  │           │                                    │ │
│  │  ┌──────────────────┐                         │ │
│  │  │  PwshClient      │                         │ │
│  │  │  192.168.21.5    │                         │ │
│  │  │  Reads/Writes to │                         │ │
│  │  │  host via router │                         │ │
│  │  └──────────────────┘                         │ │
│  │                                                │ │
│  └────────────────────────────────────────────────┘ │
│                     │                                │
│                     │ (Host connects to router)      │
│                     ▼                                │
│           TwinCAT ADS Server                         │
│           uses static route                          │
└─────────────────────────────────────────────────────┘
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
- **PowerShell client** at `192.168.21.5` - For testing connections to the host
- **Docker bridge network** `br-tcads-router` with subnet `192.168.21.0/24`

**Key Configuration Requirements (Bidirectional Routing):**

```
┌─────────────────────────────────────────────────────────────┐
│ Host TwinCAT (5.111.241.147.1.1)                            │
│                                                             │
│ Route: 43.43.43.43.1.1 @ 192.168.21.2                      │
│ (configured via AdsRouteToDocker.xml)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Docker Gateway: 192.168.21.1
                       │
┌──────────────────────▼──────────────────────────────────────┐
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
1. **Host → Container:** The host TwinCAT system needs an ADS route pointing to the container router's NetID (43.43.43.43.1.1) at the router's IP address (192.168.21.2)
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
- Build the Docker images (router and PowerShell client)
- Start the AdsRouterConsole at `192.168.21.2:48900`
- Start the PowerShell client with ADS libraries pre-configured

**Monitor the containers:**
```bash
make status           # Check container status
make logs             # View all logs
make logs-router      # View router logs only
make logs-client      # View client logs only
```

### Step 5: Test the Connection

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

### Step 6: Verify Router Configuration (Optional)

To check the router's internal configuration:

```bash
# View router logs
make logs-router

# Check if router port is accessible
make test-route
```

The router should show successful connections from the PowerShell client in its logs.

## Troubleshooting

### Problem: Host cannot connect to router in Docker

**Symptoms:**
- Host TwinCAT logs show "Connection refused" or "No route to AMS NetID"
- PowerShell client cannot connect to host

**Check ADS route:**
```bash
tcadstool 127.0.0.1 routes
```

Should show:
```
43.43.43.43.1.1 -> 192.168.21.2 (DockerContainerNetwork)
```

If missing, run `make configure-host` again.

**Check connectivity:**
```bash
make test-route       # Test if port 48900 is accessible
make status           # Check if containers are running
make logs-router      # View router logs for connection attempts
```

**Verify firewall configuration:**
```bash
sudo nft list ruleset | grep 4889    # Should show rules for br-tcads-router
```

If rule is missing, run `make configure-host` again.

### Problem: Connection is very slow

**Symptoms:**
- High latency (>50ms) for ADS operations
- Timeouts on some operations

**Solution:**
Plain ADS should be faster than ADS-over-MQTT. If you're experiencing slowness:

1. **Check Docker network overhead:**
```bash
# Test latency from container to host
docker exec -it hostrouter-pwshclient-1 ping 192.168.21.1
```

2. **Verify router isn't overloaded:**
```bash
make logs-router
```

Look for error messages or warnings.

3. **Check host system load:**
```bash
top
```

High CPU usage on host may slow down ADS responses.

### Problem: Changes to configuration not reflected

**Solution:**
```bash
make clean      # Stop containers and remove volumes
make build      # Rebuild images
make up         # Start fresh
```

If static route changes aren't working:
```bash
sudo systemctl restart TcSystemServiceUm
tcadstool 127.0.0.1 routes
```

### Problem: Router container keeps restarting

**Check router logs:**
```bash
make logs-router
```

**Common issues:**
- Port 48900 already in use: Check if another router is running
- Invalid NetID configuration: Verify `config-host-adsrouter.env` settings
- Build failed: Check if NuGet packages are accessible

### Enable Verbose Logging

Edit [config/config-host-adsrouter.env](../config/config-host-adsrouter.env) and set:
```env
Logging__LogLevel__Default=Debug
Logging__LogLevel__TwinCAT=Trace
Logging__LogLevel__TwinCAT.Ads=Trace
```

Then restart:
```bash
make restart
```

## Stopping the Containers

```bash
make down       # Stop and remove containers
make clean      # Stop, remove containers, and delete volumes
```

## Advantages of ADS Router Over MQTT

1. **Lower latency** - Direct ADS protocol without MQTT overhead
2. **Simpler debugging** - Standard TwinCAT routing tools work
3. **No external dependencies** - No MQTT broker required
4. **Familiar model** - Traditional TwinCAT architecture
5. **Better performance** - Deterministic timing for real-time applications

## When to Use ADS Router vs MQTT

**Use Plain ADS Router (this guide) when:**
- All devices are on the same local network or can reach via Docker bridge
- You need lowest possible latency
- You prefer traditional TwinCAT routing model
- You have full control over routing configuration

**Use ADS-over-MQTT when:**
- Devices are across different networks or NAT boundaries
- You want simplified configuration without static routes
- You need flexibility in network topology
- You want to integrate with IoT infrastructure
- Slightly higher latency is acceptable

## Performance Comparison

Typical latency measurements (from container to host TwinCAT on same machine):

| Method | Avg Latency | Notes |
|--------|-------------|-------|
| Plain ADS Router | 1-5ms | This guide |
| ADS-over-MQTT | 10-30ms | Higher due to MQTT serialization |

Use `Test-AdsRoute` in PowerShell to measure your actual latency:
```powershell
PS> Test-AdsRoute -NetId "5.111.241.147.1.1" -Port 851 -count 100
```
