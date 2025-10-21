# Connecting to Host TwinCAT System via ADS-over-MQTT

This guide shows how to connect Docker containers to a TwinCAT/ADS server running on the Docker host machine using **ADS-over-MQTT** protocol.

> **Note:** For prerequisites, overview, and comparison with Plain ADS Router, see [ADS README](../README.md).

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Beckhoff RT Linux Host                                         │
│  tc31-xar-um                                                    │
│  NetId: 5.111.241.147.1.1                                       │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  TwinCAT ADS Server (Port 851)                             │ │
│  │  MQTT Transport: VirtualAmsNetwork1                        │ │
│  └──────────────────────┬─────────────────────────────────────┘ │
│                         │                                       │
│                         │ Docker Gateway (192.168.20.1)         │
│                         │                                       │
│  ┌──────────────────────┼─────────────────────────────────────┐ │
│  │ Docker Network       │  br-tcads-mqtt (192.168.20.0/24)    │ │
│  │                      │                                     │ │
│  │                      |                                     │ │
│  │      ┌────────────────────────────────┐                    │ │
│  │      │    Mosquitto MQTT Broker       │                    │ │
│  │      │    192.168.20.2:1883           │                    │ │
│  │      │    Topic: VirtualAmsNetwork1   │                    │ │
│  │      └────────────┬───────────────────┘                    │ │
│  │                   │                                        │ │
│  │                   │                                        │ │
│  │                   │                                        │ │
│  │            ┌──────────────┐                                │ │
│  │            │  PwshClient  │                                │ │
│  │            │ 192.168.20.5 │                                │ │
│  │            │              │                                │ │
│  │            │ Interactive  │                                │ │
│  │            │ PowerShell   │                                │ │
│  │            │ testing      │                                │ │
│  │            └──────────────┘                                │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```



## Step-by-Step Setup

### Step 1: Understand the Configuration

This scenario uses pre-configured files in the repository:

**[config/config-host-mqtt.env](config/config-host-mqtt.env)** - Configures the MQTT connection:
- Use ADS-over-MQTT protocol
- Connect to MQTT broker at `192.168.20.2:1883`
- Use virtual NetID `42.42.42.42.1.1`
- Subscribe to topic `VirtualAmsNetwork1`

**[docker-compose.host-mqtt.yml](docker-compose.host-mqtt.yml)** - Creates:
- **Mosquitto MQTT broker** at `192.168.20.2` - Routes all ADS traffic via MQTT messages
- **PowerShell client** at `192.168.20.5` - Interactive testing client for manual ADS operations
- **Docker bridge network** `br-tcads-mqtt` with subnet `192.168.20.0/24`

**Key Configuration Requirements (MQTT-based Routing):**

```
┌─────────────────────────────────────────────────────────────┐
│ Host TwinCAT (5.111.241.147.1.1)                            │
│                                                             │
│ MQTT Config: 192.168.20.2:1883                              │
│ Topic: VirtualAmsNetwork1                                   │
│ (configured via AdsOverMqtt_insecure.xml)                   │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Docker Gateway: 192.168.20.1
                       │
┌──────────────────────|──────────────────────────────────────┐
│ Container Broker (192.168.20.2:1883)                        │
│                                                             │
│ Topic: VirtualAmsNetwork1                                   │
│ Publishes/Subscribes all ADS traffic                        │
│ (configured via mosquitto.conf)                             │
└─────────────────────────────────────────────────────────────┘
```

The key insights:
1. **Host → Container:** The host TwinCAT system publishes ADS messages to the MQTT broker on the VirtualAmsNetwork1 topic
2. **Container → Host:** Containers subscribe to the same topic and publish responses back through the broker
3. **No Static Routes:** The MQTT broker handles all routing automatically - no ADS route configuration needed

### Step 2: Clone Repository on Host Machine

On your **Beckhoff RT Linux tc31-xar-um** host, clone this repository:

```bash
ssh Administrator@BTN-000s6dhd
cd ~
git clone <repository-url>
cd AAG_Beckhoff_RT_Linux_ContainersExamples/ADS/HostMqttSetup
```

### Step 3: Configure Host for ADS-over-MQTT

**Using Makefile (recommended):**

The Makefile provides a single command to configure everything:

```bash
make configure-host
```

This command will:
1. **Install MQTT configuration** - Copies `config/AdsOverMqtt_insecure.xml` to `/etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml` to configure TwinCAT to connect to the MQTT broker at `192.168.20.2:1883` using topic `VirtualAmsNetwork1`
2. **Configure the firewall** - Creates `/etc/nftables.conf.d/60-mqtt-docker.conf` to allow MQTT traffic from the Docker network bridge `br-tcads-mqtt` on port 1883
3. **Restart TwinCAT** - Applies the MQTT configuration changes

The command will prompt you before overwriting any existing files.

<details>
<summary><b>Manual configuration (if you prefer not to use Makefile)</b></summary>

**Install MQTT configuration:**
```bash
sudo cp config/AdsOverMqtt_insecure.xml /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml
```

This configuration file contains:
- **Broker Address:** 192.168.20.2
- **Broker Port:** 1883
- **Topic:** VirtualAmsNetwork1
- **Protocol:** MQTT v3.1.1

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
- Build the Docker images (broker and PowerShell client)
- Start the Mosquitto MQTT broker at `192.168.20.2:1883`
- Start the PowerShell client at `192.168.20.5` with ADS libraries pre-configured

**Monitor the containers:**
```bash
make status           # Check container status
make logs             # View all logs
make logs-broker      # View MQTT broker logs only
make logs-client      # View PowerShell client logs only
```

### Step 5: Test Interactive Commands

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


## Understanding the PowerShell Client Container

This setup includes a single interactive client container:

### PwshClient (192.168.20.5) - Interactive Testing

**Purpose:** Manual, interactive ADS operations for testing and debugging

**Technology:**
- PowerShell 7.x with TcXaeMgmt module
- Interactive shell environment
- Rich cmdlet library for TwinCAT operations
- ADS-over-MQTT transport automatically configured

**Behavior:**
- Starts PowerShell prompt and waits for user input
- Provides full access to TcXaeMgmt cmdlets
- Allows reading/writing PLC variables, browsing symbols, etc.
- All ADS traffic routes through MQTT broker
- Ideal for ad-hoc testing and exploration

**Use cases:**
- Manual read/write operations
- Symbol browsing and exploration
- Latency testing with `Test-AdsRoute`
- Quick validation during development
- Troubleshooting MQTT connectivity

**Access shell:**
```bash
make attach
```

**Quick commands:**
```bash
make attach-read    # Read MAIN.nCounter
make attach-write   # Write value 42
```

The client communicates through the Mosquitto MQTT broker, demonstrating how ADS-over-MQTT simplifies container-to-host communication without requiring static routes.

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


## Stopping the Containers

```bash
make down       # Stop and remove containers
make clean      # Stop, remove containers, and delete volumes
```

## Troubleshooting

### Cannot Retrieve Host NetID
```bash
make check-netid        # Verify tcadstool works
sudo systemctl status TcSystemServiceUm  # Check TwinCAT running
```

### HostMqttSetup - Broker Connection Fails
```bash
make logs-broker        # Check broker logs
sudo nft list ruleset | grep 1883  # Verify firewall rule
make test-port          # Test port accessibility
```

### Host Cannot Connect to MQTT Broker

**Symptoms:**
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

### Connection is Very Slow

**Symptoms:**
- High latency (>100ms) for ADS operations
- Timeouts on some operations

**Solution:**
ADS-over-MQTT has more overhead than plain ADS. To optimize:

1. **Adjust QoS settings** in `config/config-host-mqtt.env`:
```env
AmsRouter__Mqtt__0__QoS=0  # Faster, less reliable
# AmsRouter__Mqtt__0__QoS=1  # Slower, more reliable
```

2. **Disable MQTT message persistence** in `containers/mosquitto/simple-mosquitto.conf`:
```
persistence false  # Disable for better performance
max_queued_messages 0
```

3. **Tune MQTT keep-alive** in `config/config-host-mqtt.env`:
```env
AmsRouter__Mqtt__0__KeepAlive=10
```

### Changes to Configuration Not Reflected

**Solution:**
```bash
make clean      # Stop containers and remove volumes
make up         # Start fresh
```

### Broker Disconnects Frequently

**Check broker logs:**
```bash
make logs-broker
```

**Common issues:**
- Too many connections: Edit `containers/mosquitto/simple-mosquitto.conf` to increase `max_connections`
- Memory limits: Increase Docker container memory in `docker-compose.host-mqtt.yml`
- Network instability: Check for packet loss with `ping 192.168.20.2`

### Docker Permission Denied
```bash
sudo usermod -aG docker $USER  # Then logout/login
# Or use sudo: sudo make up
```


### Enable Verbose Logging
Edit `config/config-host-mqtt.env`:
```bash
Logging__LogLevel__Default=Debug
Logging__LogLevel__TwinCAT=Trace
Logging__LogLevel__TwinCAT.Ads.AdsOverMqtt=Trace
```
Restart containers: `make down && make up`


## Disclaimer

All sample code provided by Beckhoff Automation LLC are for illustrative purposes only and are provided "as is" and without any warranties, express or implied. Actual implementations in applications will vary significantly. Beckhoff Automation LLC shall have no liability for, and does not waive any rights in relation to, any code samples that it provides or the use of such code samples for any purpose.
