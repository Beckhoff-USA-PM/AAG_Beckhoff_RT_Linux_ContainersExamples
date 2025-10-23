# Makefile for Docker ADS-over-MQTT Host Connection
# Simplifies common operations from docs/HOST_CONNECTION_MQTT_GUIDE.md

# Load environment variables from config file
include config/config-host-mqtt.env
export

# Automatically retrieve the host AMS NetID
HOST_AMS_NETID := $(shell tcadstool 127.0.0.1 netid 2>/dev/null || echo "")

.PHONY: help configure-host check-netid up up-d down build restart clean status logs logs-broker logs-client attach attach-read attach-write test-latency test-port monitor-mqtt monitor-mqtt-all install-mqtt-tools factory-reset

# ==============================================================================
# Help
# ==============================================================================

help:
	@echo "Available targets for HOST_CONNECTION_MQTT_GUIDE.md:"
	@echo ""
	@echo "Setup:"
	@echo "  make configure-host  - Configure Docker DNS, MQTT routes, and firewall (requires sudo)"
	@echo "  make check-netid     - Check if host AMS NetID can be retrieved"
	@echo "  make install-mqtt-tools - Install mosquitto-clients for monitoring"
	@echo ""
	@echo "Container Lifecycle:"
	@echo "  make up              - Start containers (with build, auto-retrieves host AMS NetID)"
	@echo "  make up-d            - Start containers in detached mode (background)"
	@echo "  make down            - Stop and remove containers"
	@echo "  make build           - Build containers without starting"
	@echo "  make restart         - Restart all containers"
	@echo "  make clean           - Stop containers and remove volumes"
	@echo "  make status          - Show container status"
	@echo ""
	@echo "Logging & Monitoring:"
	@echo "  make logs            - Show all container logs"
	@echo "  make logs-broker     - Show only MQTT broker logs"
	@echo "  make logs-client     - Show only PowerShell client logs"
	@echo "  make test-port       - Test if MQTT port 1883 is accessible"
	@echo "  make monitor-mqtt    - Monitor MQTT traffic on VirtualAmsNetwork1 (requires mosquitto-clients)"
	@echo "  make monitor-mqtt-all - Monitor all MQTT topics (requires mosquitto-clients)"
	@echo ""
	@echo "Testing:"
	@echo "  make attach          - Attach to PowerShell client container"
	@echo "  make attach-read     - Test reading MAIN.nCounter from host"
	@echo "  make attach-write    - Test writing value 42 to MAIN.nCounter on host"
	@echo "  make test-latency    - Test ADS latency with 20 roundtrips"
	@echo ""
	@echo "Advanced:"
	@echo "  make factory-reset   - ⚠️  Factory reset Docker (removes ALL containers/images/volumes/networks)"

# ==============================================================================
# Setup & Configuration
# ==============================================================================

# Configure host: Docker DNS, ADS-over-MQTT routes, and firewall
configure-host:
	@echo "=========================================="
	@echo "Host Configuration Setup"
	@echo "=========================================="
	@echo ""
	@echo "Step 1/4: Installing TwinCAT MQTT route..."
	@test -f config/AdsOverMqtt_insecure.xml || (echo "⚠️  Error: config/AdsOverMqtt_insecure.xml not found" && exit 1)
	@if [ -f /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml ]; then \
		echo "⚠️  Warning: MQTT configuration already exists"; \
		read -p "Stop TwinCAT and overwrite? (yes/no): " confirm; \
		[ "$$confirm" = "yes" ] || (echo "Configuration cancelled." && exit 1); \
		echo "Stopping TwinCAT..."; \
		sudo systemctl stop TcSystemServiceUm; \
	fi
	@sudo mkdir -p /etc/TwinCAT/3.1/Target/Routes
	@sudo cp config/AdsOverMqtt_insecure.xml /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml
	@echo "✓ TwinCAT MQTT route installed"
	@echo ""
	@echo "Step 2/4: Restarting TwinCAT to load routes..."
	@sudo systemctl restart TcSystemServiceUm
	@echo "✓ TwinCAT restarted"
	@echo ""
	@echo "Step 3/4: Installing firewall rules..."
	@test -f config/50-docker0-bridge.conf || (echo "⚠️  Error: config/50-docker0-bridge.conf not found" && exit 1)
	@test -f config/60-mqtt-docker.conf || (echo "⚠️  Error: config/60-mqtt-docker.conf not found" && exit 1)
	@if [ -f /etc/nftables.conf.d/50-docker0-bridge.conf ] || [ -f /etc/nftables.conf.d/60-mqtt-docker.conf ]; then \
		echo "⚠️  Warning: Firewall rules already exist"; \
		read -p "Overwrite? (yes/no): " confirm; \
		[ "$$confirm" = "yes" ] || (echo "Skipping firewall configuration." && exit 0); \
	fi
	@sudo cp config/50-docker0-bridge.conf /etc/nftables.conf.d/50-docker0-bridge.conf
	@sudo cp config/60-mqtt-docker.conf /etc/nftables.conf.d/60-mqtt-docker.conf
	@sudo systemctl reload nftables
	@echo "✓ Firewall rules installed"
	@echo ""
	# @echo "Step 4/4: Configuring Docker DNS and restarting..."
	# @test -f config/daemon.json || (echo "⚠️  Error: config/daemon.json not found" && exit 1)
	# @sudo mkdir -p /etc/docker
	# @sudo cp config/daemon.json /etc/docker/daemon.json
	# @sudo systemctl restart docker
	# @echo "✓ Docker configured and restarted"
	# @echo ""
	@echo "=========================================="
	@echo "✓ Configuration complete!"
	@echo "=========================================="
	@echo "Verify: sudo nft list ruleset | grep 1883"

# Check if AMS NetID was retrieved
check-netid:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Warning: Could not retrieve AMS NetID from tcadstool"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		echo ""; \
	else \
		echo "✓ Using Host AMS NetID: $(HOST_AMS_NETID)"; \
		echo ""; \
	fi

# Install mosquitto-clients for monitoring
install-mqtt-tools:
	sudo apt-get update
	sudo apt-get install -y mosquitto-clients

# ==============================================================================
# Container Lifecycle
# ==============================================================================

# Start containers with build
up: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-mqtt.yml build --no-cache
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-mqtt.yml up

# Start containers in detached mode
up-d: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-mqtt.yml build --no-cache
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-mqtt.yml up -d

# Stop and remove containers
down:
	docker compose -f docker-compose.host-mqtt.yml down

# Build containers without starting
build:
	docker compose -f docker-compose.host-mqtt.yml build --no-cache

# Restart containers
restart:
	docker compose -f docker-compose.host-mqtt.yml restart

# Stop containers and remove volumes
clean:
	docker compose -f docker-compose.host-mqtt.yml down -v

# Show container status
status:
	docker compose -f docker-compose.host-mqtt.yml ps

# ==============================================================================
# Logging & Monitoring
# ==============================================================================

# Show all logs (follow mode)
logs:
	docker compose -f docker-compose.host-mqtt.yml logs -f

# Show only broker logs
logs-broker:
	docker logs -f hostmqtt-broker

# Show only client logs
logs-client:
	docker logs -f hostmqtt-pwshclient-1

# Test if MQTT port is accessible
test-port:
	@echo "Testing MQTT port 1883..."
	@timeout 2 bash -c 'cat < /dev/null > /dev/tcp/192.168.20.2/1883' && echo "✓ Port 1883 is open" || echo "✗ Port 1883 is closed"

# Monitor MQTT traffic with timestamps
monitor-mqtt:
	@echo "Monitoring MQTT traffic on VirtualAmsNetwork1 (Ctrl+C to stop)..."
	mosquitto_sub -h 192.168.20.2 -t 'VirtualAmsNetwork1/#' -v -F '@Y-@m-@dT@H:@M:@S@z : %t : %p'

# Monitor all MQTT topics
monitor-mqtt-all:
	@echo "Monitoring all MQTT topics (Ctrl+C to stop)..."
	mosquitto_sub -h 192.168.20.2 -t '#' -v -F '@Y-@m-@dT@H:@M:@S@z : %t : %p'

# ==============================================================================
# Testing
# ==============================================================================

# Attach to PowerShell client
attach:
	docker exec -it hostmqtt-pwshclient-1 pwsh

# Test reading MAIN.nCounter from host
attach-read:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Error: Could not retrieve host AMS NetID"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		exit 1; \
	fi
	@echo "Testing connection to host at $(HOST_AMS_NETID):851..."
	@echo "Reading MAIN.nCounter..."
	docker exec -it hostmqtt-pwshclient-1 pwsh -Command "(New-TcSession -NetId '$(HOST_AMS_NETID)' -Port 851 | Get-TcSymbol -Path 'MAIN.nCounter') | Read-TcValue"

# Test writing value to MAIN.nCounter on host
attach-write:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Error: Could not retrieve host AMS NetID"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		exit 1; \
	fi
	@echo "Testing connection to host at $(HOST_AMS_NETID):851..."
	@echo "Writing value 42 to MAIN.nCounter..."
	docker exec -it hostmqtt-pwshclient-1 pwsh -Command "(New-TcSession -NetId '$(HOST_AMS_NETID)' -Port 851 | Get-TcSymbol -Path 'MAIN.nCounter') | Write-TcValue -Value 42 -Force"
	@echo "✓ Write complete"

# Test ADS latency with 20 roundtrips
test-latency:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Error: Could not retrieve host AMS NetID"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		exit 1; \
	fi
	@echo "Testing ADS latency to host at $(HOST_AMS_NETID):851..."
	@echo "Running 20 roundtrip tests..."
	docker exec -it hostmqtt-pwshclient-1 pwsh -Command "Test-AdsRoute -NetId '$(HOST_AMS_NETID)' -Port 851 -count 20"

# ==============================================================================
# Advanced
# ==============================================================================

# Factory reset Docker - WARNING: Removes ALL Docker data
factory-reset:
	@echo "⚠️  WARNING: This will remove ALL Docker containers, images, volumes, and networks!"
	@echo "⚠️  WARNING: This will also remove MQTT configuration and firewall rules!"
	@echo "This is a complete Docker factory reset on your system."
	@echo ""
	@read -p "Are you sure you want to continue? Type 'yes' to proceed: " confirm; \
	if [ "$$confirm" = "yes" ]; then \
		echo ""; \
		echo "Step 1/8: Stopping all running containers..."; \
		docker stop $$(docker ps -aq) 2>/dev/null || echo "No containers to stop"; \
		echo "Step 2/8: Removing all containers..."; \
		docker rm $$(docker ps -aq) 2>/dev/null || echo "No containers to remove"; \
		echo "Step 3/8: Removing all images..."; \
		docker rmi -f $$(docker images -aq) 2>/dev/null || echo "No images to remove"; \
		echo "Step 4/8: Removing all volumes..."; \
		docker volume rm $$(docker volume ls -q) 2>/dev/null || echo "No volumes to remove"; \
		echo "Step 5/8: Removing all custom networks..."; \
		docker network rm $$(docker network ls | grep -v "bridge\|host\|none" | awk 'NR>1 {print $$1}') 2>/dev/null || echo "No networks to remove"; \
		echo "Step 6/8: Pruning system..."; \
		docker system prune -a --volumes -f; \
		echo "Step 7/8: Removing MQTT configuration..."; \
		if [ -f /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml ]; then \
			sudo systemctl stop TcSystemServiceUm; \
			sudo rm /etc/TwinCAT/3.1/Target/Routes/AdsOverMqtt_insecure.xml; \
			sudo systemctl restart TcSystemServiceUm; \
			echo "✓ MQTT configuration removed and TwinCAT restarted"; \
		else \
			echo "No MQTT configuration found"; \
		fi; \
		echo "Step 8/8: Removing firewall rules..."; \
		removed=0; \
		if [ -f /etc/nftables.conf.d/50-docker0-bridge.conf ]; then \
			sudo rm /etc/nftables.conf.d/50-docker0-bridge.conf; \
			removed=1; \
		fi; \
		if [ -f /etc/nftables.conf.d/60-mqtt-docker.conf ]; then \
			sudo rm /etc/nftables.conf.d/60-mqtt-docker.conf; \
			removed=1; \
		fi; \
		if [ $$removed -eq 1 ]; then \
			sudo systemctl reload nftables; \
			echo "✓ Firewall rules removed and reloaded"; \
		else \
			echo "No firewall rules found"; \
		fi; \
		echo ""; \
		echo "✓ Factory reset complete!"; \
		echo "  All Docker data, MQTT configuration, and firewall rules have been removed."; \
	else \
		echo "Factory reset cancelled."; \
	fi
