# Connecting to Host TwinCAT System via ADS-over-MQTT

This guide shows how to connect Docker containers to a TwinCAT/ADS server running on the Docker host machine using **ADS-over-MQTT** instead of plain ADS routing.

## Your Setup

- **Docker Host:** Beckhoff RT Linux with TwinCAT tc31-xar-um and a PLC program running on ADS port 851 with a MAIN.nCounter.
- **Host ADS NetId:** 5.111.241.147.1.1
- **Goal:** Run containers with ADS-over-MQTT and connect to the host's ADS server

## Why ADS-over-MQTT?

ADS-over-MQTT provides several advantages over plain ADS:
- **Simplified routing:** No need for complex router configurations
- **Broker-based discovery:** Devices find each other through the MQTT broker
- **Network flexibility:** Works across different network topologies
- **Firewall friendly:** Only requires MQTT port (1883) to be open

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│  Beckhoff RT Linux                                   │
│                                                      │
│  tc31-xar-um                                         │
│  NetId: 5.111.241.147.1.1                           │
│  ADS over MQTT  connecting to: 192.168.20.2:1883      │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │ br-tcads-mqtt (192.168.20.0/24)               │ │
│  │                                                │ │
│  │  ┌──────────────────────┐                     │ │
│  │  │ Mosquitto MQTT       │                     │ │
│  │  │ Broker               │                     │ │
│  │  │ 192.168.20.2:1883    │                     │ │
│  │  │ Topic: HostNetwork   │                     │ │
│  │  └──────────────────────┘                     │ │
│  │           │                                    │ │
│  │           │ (all ADS traffic flows via MQTT)  │ │
│  │           │                                    │ │
│  │  ┌──────────────────┐                         │ │
│  │  │  PwshClient      │                         │ │
│  │  │  192.168.20.5    │                         │ │
│  │  │  Reads/Writes to │                         │ │
│  │  │  host via MQTT   │                         │ │
│  │  └──────────────────┘                         │ │
│  │                                                │ │
│  └────────────────────────────────────────────────┘ │
│                     │                                │
│                     │ (Host connects to broker)      │
│                     ▼                                │
│           TwinCAT ADS Server                         │
│           subscribes to MQTT                         │
└─────────────────────────────────────────────────────┘
```

## Key Differences from Plain ADS

| Aspect | Plain ADS | ADS-over-MQTT |
|--------|-----------|---------------|
| Routing | Requires AdsRouterConsole | Uses MQTT broker |
| Static Routes | Required on host | Not needed |
| Port Configuration | Custom loopback ports (48900) | Standard MQTT (1883) |
| Discovery | Manual route configuration | Automatic via broker |

## Step-by-Step Setup

### Step 1: Understand the Configuration

This scenario uses two pre-configured files in the repository:

**[config-host-mqtt.env](config-host-mqtt.env)** - Configures the ADS router to:
- Use ADS-over-MQTT protocol instead of plain ADS
- Connect to the MQTT broker at `192.168.20.2:1883`
- Subscribe to the topic `VirtualAmsNetwork1` where all ADS traffic flows

**[docker-compose.host-mqtt.yml](docker-compose.host-mqtt.yml)** - Creates:
- **Mosquitto MQTT broker** at `192.168.20.2` - Acts as the message hub for ADS traffic
- **PowerShell client** at `192.168.20.5` - For testing connections to the host
- **Docker bridge network** `br-tcads-mqtt` with subnet `192.168.20.0/24`

The key insight: Instead of direct ADS routing, all ADS packets are serialized and sent as MQTT messages through the broker. This simplifies network configuration and works across NAT boundaries.

### Step 2: Clone Repository on Host Machine

On your **Beckhoff RT Linux tc31-xar-um** host, clone this repository:

```bash
ssh Administrator@BTN-000s6dhd
cd ~
git clone <repository-url>
cd AAG_Beckhoff_RT_Linux_ContainersExamples/DockerSamples/HostMqttSetup
```

### Step 3: Configure Host for ADS-over-MQTT

**Using Makefile (recommended):**

The Makefile provides a single command to configure everything:

```bash
make configure-host
```

This command will:
1. **Install the MQTT configuration** - Copies [config/AdsOverMqtt_insecure.xml](../config/AdsOverMqtt_insecure.xml) to `/etc/TwinCAT/3.1/Target/Routes/` to configure TwinCAT to connect to the MQTT broker at `192.168.20.2:1883` using topic `VirtualAmsNetwork1`
2. **Configure the firewall** - Creates `/etc/nftables.conf.d/60-mqtt-docker.conf` to allow MQTT traffic from the Docker network bridge `br-tcads-mqtt` on port 1883
3. **Restart TwinCAT** - Applies the MQTT configuration changes

The command will prompt you before overwriting any existing files.

<details>
<summary><b>Manual configuration (if you prefer not to use Makefile)</b></summary>

**Install MQTT configuration:**
```bash
sudo cp config/AdsOverMqtt_insecure.xml /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml
```

**Configure firewall:**
```bash
echo 'table inet filter {
  chain input {
    # Accept MQTT traffic from Docker network
    iifname "br-tcads-mqtt" tcp dport 1883 accept
  }
}' | sudo tee /etc/nftables.conf.d/60-mqtt-docker.conf
sudo systemctl reload nftables
```

**Restart TwinCAT:**
```bash
sudo systemctl restart TcSystemServiceUm
```
</details>

### Step 4: Start Docker Containers

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
- Build the Docker images (broker and PowerShell client)
- Start the MQTT broker at `192.168.20.2:1883`
- Start the PowerShell client with ADS libraries pre-configured

**Monitor the containers:**
```bash
make status           # Check container status
make logs             # View all logs
make logs-broker      # View broker logs only
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

These commands automatically use the detected host AMS NetID and execute read/write operations via ADS-over-MQTT.

**Interactive PowerShell session:**

For more advanced testing, attach to the PowerShell client:

```bash
make attach
```

Then run PowerShell commands manually:

```powershell
# Check ADS state (verify connection via MQTT)
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

### Step 6: Monitor MQTT Traffic (Optional)

To see ADS traffic flowing through the MQTT broker:

```bash
# Install monitoring tools
make install-mqtt-tools

# Monitor traffic on VirtualAmsNetwork1 topic (with timestamps)
make monitor-mqtt

# Or monitor all MQTT topics
make monitor-mqtt-all
```

This lets you see ADS packets being exchanged between the host and containers in real-time.

## Troubleshooting

### Problem: Host cannot connect to MQTT broker in Docker

**Symptoms:**
- Host TwinCAT logs show "Connection refused" to MQTT broker
- No MQTT traffic visible

**Check connectivity:**
```bash
make test-port        # Test if port 1883 is accessible
make status           # Check if containers are running
make logs-broker      # View broker logs for connection attempts
```

**Verify firewall configuration:**
```bash
sudo nft list ruleset | grep 1883    # Should show rule for br-tcads-mqtt
```

If rule is missing, run `make configure-host` again.

### Problem: Connection is very slow

**Symptoms:**
- High latency (>100ms) for ADS operations
- Timeouts on some operations

**Solution:**
ADS-over-MQTT has more overhead than plain ADS. To optimize:

1. **Adjust QoS settings** in `config-host-mqtt.env`:
```env
AmsRouter__Mqtt__0__QoS=0  # Faster, less reliable
# AmsRouter__Mqtt__0__QoS=1  # Slower, more reliable
```

2. **Enable MQTT message persistence** in `simple-mosquitto.conf`:
```
persistence false  # Disable for better performance
max_queued_messages 0
```

3. **Tune MQTT keep-alive**:
```env
AmsRouter__Mqtt__0__KeepAlive=10
```

### Problem: Changes to configuration not reflected

**Solution:**
```bash
make clean      # Stop containers and remove volumes
make build      # Rebuild images
make up         # Start fresh
```

### Problem: Broker disconnects frequently

**Check broker logs:**
```bash
make logs-broker
```

**Common issues:**
- Too many connections: Edit `simple-mosquitto.conf` to increase `max_connections`
- Memory limits: Increase Docker container memory in `docker-compose.host-mqtt.yml`
- Network instability: Check for packet loss

### Enable Verbose Logging

Edit [config/config-host-mqtt.env](../config/config-host-mqtt.env) and set:
```env
Logging__LogLevel__Default=Debug
Logging__LogLevel__TwinCAT=Trace
Logging__LogLevel__TwinCAT.Ads.AdsOverMqtt=Trace
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

## Advantages of MQTT Over Plain ADS

1. **No static routes required** - Devices discover each other via broker
2. **NAT-friendly** - Works across network boundaries
3. **Single firewall rule** - Only MQTT port needs to be open
4. **Flexible topology** - Star topology via broker instead of point-to-point
5. **Cloud-ready** - Can use cloud MQTT brokers (AWS IoT, Azure IoT Hub)

## When to Use Plain ADS vs MQTT

**Use Plain ADS when:**
- All devices are on the same local network
- You have full control over routing configuration

**Use ADS-over-MQTT when:**
- Devices are across different networks or NAT boundaries
- You want simplified configuration
- You need flexibility in network topology
- You want to integrate with IoT infrastructure



## Disclaimer 

All sample code provided by Beckhoff Automation LLC are for illustrative purposes only and are provided “as is” and without any warranties, express or implied. Actual implementations in applications will vary significantly. Beckhoff Automation LLC shall have no liability for, and does not waive any rights in relation to, any code samples that it provides or the use of such code samples for any purpose.
