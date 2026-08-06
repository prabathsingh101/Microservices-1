#!/bin/bash
set -e

echo "=== 1. Updating System Packages ==="
sudo apt-get update -y
sudo apt-get install -y ca-certificates curl gnupg lsb-release zip unzip

echo "=== 2. Installing Docker & Docker Compose ==="
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update -y
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

sudo systemctl start docker
sudo systemctl enable docker

echo "=== 3. Checking Docker Installation ==="
sudo docker --version
sudo docker compose version

echo "=== 4. Starting Microservices Stack via Docker Compose ==="
cd /home/azureuser/app
sudo docker compose down || true
sudo docker compose up -d --build

echo "=== 5. Checking Container Status ==="
sudo docker ps
