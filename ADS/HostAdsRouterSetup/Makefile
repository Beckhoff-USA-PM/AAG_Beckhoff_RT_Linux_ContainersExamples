# Makefile for Docker ADS Router Host Connection
# Simplifies common operations from HOST_CONNECTION_ADSROUTER_GUIDE.md

# Load environment variables from config file
include config/config-host-adsrouter.env
export

# Automatically retrieve the host AMS NetID
HOST_AMS_NETID := $(shell tcadstool 127.0.0.1 netid 2>/dev/null || echo "")

.PHONY: help configure-host check-netid up up-d down build restart clean status logs logs-router logs-client logs-pwsh attach attach-read attach-write test-latency factory-reset

# ==============================================================================
# Help
# ==============================================================================

help:
	@echo "Available targets for HOST_CONNECTION_ADSROUTER_GUIDE.md:"
	@echo ""
	@echo "Setup:"
	@echo "  make configure-host  - Configure Docker DNS, TwinCAT routes, and firewall (requires sudo)"
	@echo "  make check-netid     - Check if host AMS NetID can be retrieved"
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
	@echo "  make logs-router     - Show only ADS router logs"
	@echo "  make logs-client     - Show only ADS client logs"
	@echo "  make logs-pwsh       - Show only PowerShell client logs"
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

# Configure host: Docker DNS, TwinCAT routes, and firewall
configure-host:
	@echo "=========================================="
	@echo "Host Configuration Setup"
	@echo "=========================================="
	@echo ""
	@echo "Step 1/4: Installing TwinCAT ADS route..."
	@test -f config/AdsRouteToDocker.xml || (echo "⚠️  Error: config/AdsRouteToDocker.xml not found" && exit 1)
	@if [ -f /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml ]; then \
		echo "⚠️  Warning: ADS route already exists"; \
		read -p "Stop TwinCAT and overwrite? (yes/no): " confirm; \
		[ "$$confirm" = "yes" ] || (echo "Configuration cancelled." && exit 1); \
		echo "Stopping TwinCAT..."; \
		sudo systemctl stop TcSystemServiceUm; \
	fi
	@sudo mkdir -p /etc/TwinCAT/3.1/Target/Routes
	@sudo cp config/AdsRouteToDocker.xml /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml
	@echo "✓ TwinCAT ADS route installed"
	@echo ""
	@echo "Step 2/4: Restarting TwinCAT to load routes..."
	@sudo systemctl restart TcSystemServiceUm
	@echo "✓ TwinCAT restarted"
	@echo ""
	@echo "Step 3/4: Installing firewall rules..."
	@test -f config/50-docker0-bridge.conf || (echo "⚠️  Error: config/50-docker0-bridge.conf not found" && exit 1)
	@test -f config/60-ads-docker.conf || (echo "⚠️  Error: config/60-ads-docker.conf not found" && exit 1)
	@if [ -f /etc/nftables.conf.d/50-docker0-bridge.conf ] || [ -f /etc/nftables.conf.d/60-ads-docker.conf ]; then \
		echo "⚠️  Warning: Firewall rules already exist"; \
		read -p "Overwrite? (yes/no): " confirm; \
		[ "$$confirm" = "yes" ] || (echo "Skipping firewall configuration." && exit 0); \
	fi
	@sudo cp config/50-docker0-bridge.conf /etc/nftables.conf.d/50-docker0-bridge.conf
	@sudo cp config/60-ads-docker.conf /etc/nftables.conf.d/60-ads-docker.conf
	@sudo systemctl reload nftables
	@echo "✓ Firewall rules installed"
	@echo ""
	@echo "Step 4/4: Configuring Docker DNS and restarting..."
	@test -f config/daemon.json || (echo "⚠️  Error: config/daemon.json not found" && exit 1)
	@sudo mkdir -p /etc/docker
	@sudo cp config/daemon.json /etc/docker/daemon.json
	@sudo systemctl restart docker
	@echo "✓ Docker configured and restarted"
	@echo ""
	@echo "=========================================="
	@echo "✓ Configuration complete!"
	@echo "=========================================="
	@echo "Verify: sudo nft list ruleset | grep 4889"

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

# ==============================================================================
# Container Lifecycle
# ==============================================================================

# Start containers with build
up: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml build --no-cache
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml up

# Start containers in detached mode
up-d: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml build --no-cache
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml up -d

# Stop and remove containers
down:
	docker compose -f docker-compose.host-adsrouter.yml down

# Build containers without starting
build:
	docker compose -f docker-compose.host-adsrouter.yml build --no-cache

# Restart containers
restart:
	docker compose -f docker-compose.host-adsrouter.yml restart

# Stop containers and remove volumes
clean:
	docker compose -f docker-compose.host-adsrouter.yml down -v

# Show container status
status:
	docker compose -f docker-compose.host-adsrouter.yml ps

# ==============================================================================
# Logging & Monitoring
# ==============================================================================

# Show all logs (follow mode)
logs:
	docker compose -f docker-compose.host-adsrouter.yml logs -f

# Show only router logs
logs-router:
	docker logs -f hostrouter-router

# Show only ADS client logs
logs-client:
	docker logs -f hostrouter-adsclient-1

# Show only PowerShell client logs
logs-pwsh:
	docker logs -f hostrouter-pwshclient-1

# ==============================================================================
# Testing
# ==============================================================================

# Attach to PowerShell client
attach:
	docker exec -it hostrouter-pwshclient-1 pwsh

# Test reading MAIN.nCounter from host
attach-read:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Error: Could not retrieve host AMS NetID"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		exit 1; \
	fi
	@echo "Testing connection to host at $(HOST_AMS_NETID):851..."
	@echo "Reading MAIN.nCounter..."
	docker exec -it hostrouter-pwshclient-1 pwsh -Command "(New-TcSession -NetId '$(HOST_AMS_NETID)' -Port 851 | Get-TcSymbol -Path 'MAIN.nCounter') | Read-TcValue"

# Test writing value to MAIN.nCounter on host
attach-write:
	@if [ -z "$(HOST_AMS_NETID)" ]; then \
		echo "⚠️  Error: Could not retrieve host AMS NetID"; \
		echo "   Make sure TwinCAT is running and tcadstool is available"; \
		exit 1; \
	fi
	@echo "Testing connection to host at $(HOST_AMS_NETID):851..."
	@echo "Writing value 42 to MAIN.nCounter..."
	docker exec -it hostrouter-pwshclient-1 pwsh -Command "(New-TcSession -NetId '$(HOST_AMS_NETID)' -Port 851 | Get-TcSymbol -Path 'MAIN.nCounter') | Write-TcValue -Value 42 -Force"
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
	docker exec -it hostrouter-pwshclient-1 pwsh -Command "Test-AdsRoute -NetId '$(HOST_AMS_NETID)' -Port 851 -count 20"

# ==============================================================================
# Advanced
# ==============================================================================

# Factory reset Docker - WARNING: Removes ALL Docker data
factory-reset:
	@echo "⚠️  WARNING: This will remove ALL Docker containers, images, volumes, and networks!"
	@echo "⚠️  WARNING: This will also remove ADS route configuration and firewall rules!"
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
		echo "Step 7/8: Removing ADS route configuration..."; \
		if [ -f /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml ]; then \
			sudo systemctl stop TcSystemServiceUm; \
			sudo rm /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml; \
			sudo systemctl restart TcSystemServiceUm; \
			echo "✓ ADS route removed and TwinCAT restarted"; \
		else \
			echo "No ADS route configuration found"; \
		fi; \
		echo "Step 8/8: Removing firewall rules..."; \
		removed=0; \
		if [ -f /etc/nftables.conf.d/50-docker0-bridge.conf ]; then \
			sudo rm /etc/nftables.conf.d/50-docker0-bridge.conf; \
			removed=1; \
		fi; \
		if [ -f /etc/nftables.conf.d/60-ads-docker.conf ]; then \
			sudo rm /etc/nftables.conf.d/60-ads-docker.conf; \
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
		echo "  All Docker data, ADS routes, and firewall rules have been removed."; \
	else \
		echo "Factory reset cancelled."; \
	fi
