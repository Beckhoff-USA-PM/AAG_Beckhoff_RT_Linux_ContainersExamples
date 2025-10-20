# Makefile for Docker ADS Router Host Connection
# Simplifies common operations from docs/HOST_CONNECTION_ADSROUTER_GUIDE.md

# Load environment variables from config file
include config/config-host-adsrouter.env
export

# Automatically retrieve the host AMS NetID
HOST_AMS_NETID := $(shell tcadstool 127.0.0.1 netid 2>/dev/null || echo "")

.PHONY: help up up-d down build logs logs-router logs-client attach attach-read attach-write restart clean test-route status check-netid configure-host factory-reset

# Default target - show help
help:
	@echo "Available targets for HOST_CONNECTION_ADSROUTER_GUIDE.md:"
	@echo ""
	@echo "  make up              - Start containers (with build, auto-retrieves host AMS NetID)"
	@echo "  make up-d            - Start containers in detached mode (background)"
	@echo "  make down            - Stop and remove containers"
	@echo "  make build           - Build containers without starting"
	@echo "  make logs            - Show all container logs"
	@echo "  make logs-router     - Show only ADS router logs"
	@echo "  make logs-client     - Show only PowerShell client logs"
	@echo "  make attach          - Attach to PowerShell client container"
	@echo "  make attach-read     - Test reading MAIN.nCounter from host"
	@echo "  make attach-write    - Test writing value 42 to MAIN.nCounter on host"
	@echo "  make restart         - Restart all containers"
	@echo "  make clean           - Stop containers and remove volumes"
	@echo "  make status          - Show container status"
	@echo "  make check-netid     - Check if host AMS NetID can be retrieved"
	@echo "  make test-route      - Test if ADS router port 48900 is accessible"
	@echo "  make configure-host  - Configure host: Add static route to TwinCAT (requires sudo)"
	@echo ""
	@echo "  make factory-reset   - ⚠️  Factory reset Docker (removes ALL containers/images/volumes/networks)"

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

# Start containers with build
up: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml up --build

# Start containers in detached mode
up-d: check-netid
	HOST_AMS_NETID=$(HOST_AMS_NETID) docker compose -f docker-compose.host-adsrouter.yml up -d --build

# Stop and remove containers
down:
	docker compose -f docker-compose.host-adsrouter.yml down

# Build containers without starting
build:
	docker compose -f docker-compose.host-adsrouter.yml build

# Show all logs (follow mode)
logs:
	docker compose -f docker-compose.host-adsrouter.yml logs -f

# Show only router logs
logs-router:
	docker logs -f hostrouter-router

# Show only client logs
logs-client:
	docker logs -f hostrouter-pwshclient-1

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

# Restart containers
restart:
	docker compose -f docker-compose.host-adsrouter.yml restart

# Stop containers and remove volumes
clean:
	docker compose -f docker-compose.host-adsrouter.yml down -v

# Test if ADS router port is accessible
test-route:
	@echo "Testing ADS router port 48900..."
	@timeout 2 bash -c 'cat < /dev/null > /dev/tcp/192.168.21.2/48900' && echo "✓ Port 48900 is open" || echo "✗ Port 48900 is closed"

# Configure host: Add static route to TwinCAT
configure-host:
	@echo "=========================================="
	@echo "Configuring host for ADS routing"
	@echo "=========================================="
	@echo ""
	@echo "Step 1/3: Installing ADS route configuration..."
	@if [ ! -f config/AdsRouteToDocker.xml ]; then \
		echo "⚠️  Error: config/AdsRouteToDocker.xml not found"; \
		exit 1; \
	fi
	@echo "Creating route from host to container router (43.43.43.43.1.1 @ 192.168.21.2)"
	@if [ -f /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml ]; then \
		echo "⚠️  Warning: /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml already exists"; \
		echo ""; \
		read -p "Do you want to stop TwinCAT and overwrite it? (yes/no): " confirm; \
		if [ "$$confirm" != "yes" ]; then \
			echo "Configuration cancelled."; \
			exit 1; \
		fi; \
		echo "Stopping TwinCAT..."; \
		sudo systemctl stop TcSystemServiceUm; \
		echo "✓ TwinCAT stopped"; \
		echo ""; \
	fi
	@sudo cp config/AdsRouteToDocker.xml /etc/TwinCAT/3.1/Target/Routes/AdsRouteToDocker.xml
	@echo "✓ ADS route configuration installed"
	@echo ""
	@echo "Step 2/3: Configuring firewall for unsecure ADS traffic..."
	@if [ -f /etc/nftables.conf.d/60-ads-docker.conf ]; then \
		echo "⚠️  Warning: /etc/nftables.conf.d/60-ads-docker.conf already exists"; \
		read -p "Do you want to overwrite it? (yes/no): " confirm; \
		if [ "$$confirm" != "yes" ]; then \
			echo "Skipping firewall configuration."; \
		else \
			echo "table inet filter {" | sudo tee /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "  chain input {" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "    # Accept ADS traffic from Docker network" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "    iifname \"br-tcads-router\" tcp dport 48898 accept" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "    iifname \"br-tcads-router\" udp dport 48899 accept" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "  }" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			echo "}" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
			sudo systemctl reload nftables; \
			echo "✓ Firewall configured and reloaded"; \
		fi; \
	else \
		echo "table inet filter {" | sudo tee /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		echo "  chain input {" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		echo "    # Accept ADS traffic from Docker network" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		echo "    iifname \"br-tcads-router\" tcp dport 48898 accept" | sudo tee -a /etc/nftables.conf.d/60-ads-router.conf > /dev/null; \
		echo "    iifname \"br-tcads-router\" udp dport 48899 accept" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		echo "  }" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		echo "}" | sudo tee -a /etc/nftables.conf.d/60-ads-docker.conf > /dev/null; \
		sudo systemctl reload nftables; \
		echo "✓ Firewall configured and reloaded"; \
	fi
	@echo ""
	@echo "Step 3/3: Starting TwinCAT..."
	@sudo systemctl start TcSystemServiceUm
	@echo "✓ TwinCAT restarted"
	@echo ""
	@echo "=========================================="
	@echo "✓ Host configuration complete!"
	@echo "=========================================="
	@echo ""
	@echo "Verify ADS route:"
	@echo "  tcadstool 127.0.0.1 routes"
	@echo ""
	@echo "Verify firewall rules:"
	@echo "  sudo nft list ruleset | grep 4889"

# Show container status
status:
	docker compose -f docker-compose.host-adsrouter.yml ps

# Factory reset Docker - WARNING: Removes ALL Docker data
factory-reset:
	@echo "⚠️  WARNING: This will remove ALL Docker containers, images, volumes, and networks!"
	@echo "This is a complete Docker factory reset on your system."
	@echo ""
	@read -p "Are you sure you want to continue? Type 'yes' to proceed: " confirm; \
	if [ "$$confirm" = "yes" ]; then \
		echo ""; \
		echo "Step 1/6: Stopping all running containers..."; \
		docker stop $$(docker ps -aq) 2>/dev/null || echo "No containers to stop"; \
		echo "Step 2/6: Removing all containers..."; \
		docker rm $$(docker ps -aq) 2>/dev/null || echo "No containers to remove"; \
		echo "Step 3/6: Removing all images..."; \
		docker rmi -f $$(docker images -aq) 2>/dev/null || echo "No images to remove"; \
		echo "Step 4/6: Removing all volumes..."; \
		docker volume rm $$(docker volume ls -q) 2>/dev/null || echo "No volumes to remove"; \
		echo "Step 5/6: Removing all custom networks..."; \
		docker network rm $$(docker network ls | grep -v "bridge\|host\|none" | awk 'NR>1 {print $$1}') 2>/dev/null || echo "No networks to remove"; \
		echo "Step 6/6: Pruning system..."; \
		docker system prune -a --volumes -f; \
		echo ""; \
		echo "✓ Docker factory reset complete!"; \
		echo "  All containers, images, volumes, and networks have been removed."; \
	else \
		echo "Factory reset cancelled."; \
	fi
