# TwinCAT ADS Communication from Docker Containers

This directory contains examples for connecting Docker containers to TwinCAT systems running on Beckhoff RT Linux hosts.

## Two Approaches

### [HostMqttSetup](HostMqttSetup/) - ADS-over-MQTT
Uses Mosquitto MQTT broker for ADS communication. Best for cross-network scenarios and NAT traversal.


### [HostAdsRouterSetup](HostAdsRouterSetup/) - Plain ADS Router
Uses AdsRouterConsole for direct ADS routing. Best for same-network, low-latency scenarios.


## Prerequisites

Before using either setup, ensure you have:
- Beckhoff RT Linux with package [tc31-xar-um](https://infosys.beckhoff.com/content/1033/beckhoff_rt_linux/17350412299.html?id=7176322633100356666).
- [Docker and Docker Compose installed on the host](https://docs.docker.com/engine/install/debian/)
- Make utility: `sudo apt install --yes make`
- [Example TwinCAT project](../TwinCAT%20Project/TwinCAT%20Project/) running on the host Beckhoff RT Linux.
- User must be in the `docker` group or use `sudo` for Docker commands
- TwinCAT running on Beckhoff RT Linux (tc31-xar-um or similar)
- `tcadstool` available on the host for NetID auto-detection

## Overview of Both Setups

Both setups demonstrate bidirectional ADS communication between Docker containers and a host TwinCAT system:

**Common Elements:**
- **Host NetID Detection:** Automatically retrieved using `tcadstool`
- **Docker Networks:** Each setup uses a dedicated bridge network to prevent conflicts
- **Automated Configuration:** Makefile-driven setup with `make configure-host` command
- **Container Restart Policy:** All containers configured with `restart: unless-stopped` for automatic recovery

**HostAdsRouterSetup Specifics:**
- **Container Router:** AdsRouterConsole providing ADS routing services
- **Container Clients:**
  - AdsClient (.NET 8.0) - Automated monitoring (reads host state every 1 second)
  - PowerShell client - Interactive testing

**HostMqttSetup Specifics:**
- **Container Broker:** Mosquitto MQTT broker providing message routing
- **Container Client:** PowerShell client - Interactive testing

## Performance Comparison

Typical latency measurements (from container to host TwinCAT on same machine):

| Method | Avg Latency | Best For |
|--------|-------------|----------|
| **Plain ADS Router** | 1-5ms | Same-network, low-latency scenarios |
| **ADS-over-MQTT** | 40-50ms | Cross-network, NAT traversal, IoT integration |

Use `Test-AdsRoute` in PowerShell to measure your actual latency:
```powershell
PS> Test-AdsRoute -NetId "<HOST_NETID>" -Port 851 -count 100
```

## Key Differences

| Aspect | Plain ADS Router | ADS-over-MQTT |
|--------|------------------|---------------|
| **Routing** | Requires AdsRouterConsole | Uses MQTT broker |
| **Static Routes** | Required on host | Not needed |
| **Port Configuration** | Custom loopback port (48900) | Standard MQTT (1883) |
| **Discovery** | Manual route configuration | Automatic via broker |
| **Latency** | Lower (direct ADS) | Higher (MQTT overhead) |
| **Network Flexibility** | Same network only | Works across NAT |
| **Firewall Rules** | ADS ports (48898/tcp, 48899/udp) | MQTT port (1883/tcp) |
| **Cloud Integration** | Not applicable | Can use cloud MQTT brokers |

## When to Use Each Approach

### Use Plain ADS Router when:
- All devices are on the same local network or can reach via Docker bridge
- You need lowest possible latency (1-5ms)
- You have full control over routing configuration
- Network topology is simple and stable

### Use ADS-over-MQTT when:
- Devices are across different networks or NAT boundaries
- You want simplified configuration without static routes
- You need flexibility in network topology
- You want to integrate with IoT infrastructure
- You can tolerate slightly higher latency (40-50ms) for convenience
- You need to use cloud MQTT brokers (AWS IoT, Azure IoT Hub)

## Quick Start

### HostAdsRouterSetup (Plain ADS)

```bash
cd ADS/HostAdsRouterSetup

# One-time setup (installs ADS route, configures firewall)
make configure-host

# Start containers (auto-detects host AMS NetID)
make up-d

# Quick tests
make attach-read     # Read MAIN.nCounter from host
make attach-write    # Write 42 to MAIN.nCounter

# Stop
make down
```

### HostMqttSetup (ADS-over-MQTT)

```bash
cd ADS/HostMqttSetup

# One-time setup (installs MQTT config, configures firewall)
make configure-host

# Start containers (auto-detects host AMS NetID)
make up-d

# Quick tests
make attach-read     # Read MAIN.nCounter from host
make attach-write    # Write 42 to MAIN.nCounter

# Stop
make down
```

## Documentation

- **[HostAdsRouterSetup Guide](HostAdsRouterSetup/HOST_CONNECTION_ADSROUTER_GUIDE.md)** - Complete setup guide for plain ADS routing
- **[HostMqttSetup Guide](HostMqttSetup/HOST_CONNECTION_MQTT_GUIDE.md)** - Complete setup guide for ADS-over-MQTT

Each guide includes:
- Detailed architecture diagrams
- Step-by-step configuration instructions
- Troubleshooting sections


## Makefile Targets

Both setups support these common targets:

- `make help` - Show all available commands
- `make configure-host` - One-time host configuration (routes/firewall)
- `make up` - Start containers with logs
- `make up-d` - Start containers in background
- `make down` - Stop and remove containers
- `make clean` - Stop, remove containers, and delete volumes
- `make logs` - View all container logs
- `make attach` - Interactive PowerShell session
- `make attach-read` - Quick test: read MAIN.nCounter
- `make attach-write` - Quick test: write 42 to MAIN.nCounter
- `make test-latency` - Test ADS latency with 20 roundtrips
- `make check-netid` - Verify host NetID detection
- `make status` - Check container status
- `make factory-reset` - Complete cleanup and reset



## Disclaimer

All sample code provided by Beckhoff Automation LLC are for illustrative purposes only and are provided "as is" and without any warranties, express or implied. Actual implementations in applications will vary significantly. Beckhoff Automation LLC shall have no liability for, and does not waive any rights in relation to, any code samples that it provides or the use of such code samples for any purpose.
