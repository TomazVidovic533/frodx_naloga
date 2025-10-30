.PHONY: help setup start stop clean test test-docker

help:
	@echo "Available commands:"
	@echo "  make setup       - Build all images and prepare environment"
	@echo "  make start       - Start all services"
	@echo "  make stop        - Stop all services"
	@echo "  make clean       - Remove all containers, volumes, and data"
	@echo "  make test        - Run tests locally"
	@echo "  make test-docker - Run tests in isolated Docker container"

setup:
	@echo "Building all services..."
	docker-compose build
	@echo "Setup completed!"

start:
	@echo "Starting all services..."
	docker-compose up -d
	@echo "All services started!"

stop:
	@echo "Stopping all services..."
	docker-compose down
	@echo "All services stopped!"

clean:
	@echo "Cleaning up everything..."
	docker-compose down -v
	docker volume rm frodx_mssql_data 2>/dev/null || true
	rm -rf logs/*
	rm -rf downloads/*
	@echo "Cleanup completed!"

test:
	@echo "Running tests locally..."
	dotnet test tests/OrderIngestion.Tests/OrderIngestion.Tests.csproj
	@echo "Tests completed!"

test-docker:
	@echo "Running tests in Docker container..."
	docker run --rm --name orderingestion-tests -v $(PWD):/app -w /app mcr.microsoft.com/dotnet/sdk:9.0 dotnet test tests/OrderIngestion.Tests/OrderIngestion.Tests.csproj
	@echo "Docker tests completed!"