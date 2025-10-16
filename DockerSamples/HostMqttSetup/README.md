# TwinCAT ADS-over-MQTT Docker Setup

Connect to a TwinCAT system running on a Docker host machine using ADS-over-MQTT protocol.

## Quick Start

1. **Prerequisites**
   - Docker and Docker Compose installed on Debian host
   - TwinCAT running on the host with MQTT transport enabled
   - `tcadstool` available on the host

2. **Configuration**

   Edit network settings in `config/config-host-mqtt.env` if needed (defaults should work):
   ```bash
   DOCKER_SUBNET=192.168.20.0/24
   DOCKER_GATEWAY=192.168.20.1
   MQTT_BROKER_IP=192.168.20.2
   PWSH_CLIENT_IP=192.168.20.5
   ```

3. **Start the containers**
   ```bash
   make up
   ```

   The Makefile automatically retrieves your host's AMS NetID using `tcadstool`.

4. **Test the connection**
   ```bash
   make attach
   ```

## Available Commands

```bash
make help          # Show all available commands
make up            # Start containers (auto-retrieves host AMS NetID)
make down          # Stop containers
make logs          # View all logs
make logs-broker   # View MQTT broker logs only
make logs-client   # View PowerShell client logs only
make attach        # Attach to PowerShell client
make check-netid   # Check if host AMS NetID can be retrieved
```

## Directory Structure

```
HostMqttSetup/
├── README.md                              # This file
├── docker-compose.host-mqtt.yml           # Main Docker Compose configuration
├── Makefile                               # Build and run commands
├── config/                                # Configuration files
│   ├── config-host-mqtt.env              # Environment variables (edit this!)
├── containers/                            # Container definitions
│   ├── mosquitto/
│   │   └── simple-mosquitto.conf         # MQTT broker config
│   └── pwsh-client/
│       ├── Dockerfile                    # PowerShell client image
│       └── init.ps1                      # Startup script
├── docs/                                  # Documentation
│   └── HOST_CONNECTION_MQTT_GUIDE.md     # Detailed setup guide
└── diagrams/                              # Architecture diagrams
    ├── MQTTConfiguration.svg
    └── MQTTConfiguration.drawio
```

## How It Works

1. Makefile runs `tcadstool 127.0.0.1 netid` to get the host's AMS NetID
2. Docker Compose starts:
   - MQTT broker (Mosquitto) on `192.168.20.2:1883`
   - PowerShell client on `192.168.20.5`
3. PowerShell client automatically connects to the host TwinCAT system via MQTT
4. Containers restart automatically on reboot (`restart: unless-stopped`)

## Network Architecture

```
Host TwinCAT System (5.111.241.147.1.1)
         |
         | MQTT (port 1883)
         |
    [MQTT Broker Container]
    192.168.20.2
         |
         | MQTT Topic: VirtualAmsNetwork1
         |
    [PowerShell Client Container]
    192.168.20.5
```

## Documentation

See [docs/HOST_CONNECTION_MQTT_GUIDE.md](docs/HOST_CONNECTION_MQTT_GUIDE.md) for detailed setup instructions.

## Troubleshooting

- **Can't retrieve AMS NetID**: Ensure TwinCAT is running and `tcadstool` is installed
- **MQTT connection fails**: Check that TwinCAT MQTT transport is enabled and configured
- **Network conflicts**: Edit IP addresses in `config/config-host-mqtt.env`
