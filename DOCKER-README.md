# Email Service - Docker Deployment Guide

This guide explains how to deploy the Email Service microservices using Docker Compose.

## Architecture Overview

The solution consists of the following services:

- **EmailService.API** (Port 7100) - Main API for sending emails
- **EmailService.Admin** (Port 7101) - Admin dashboard for managing tenants and monitoring
- **EmailService.Gateway** (Port 7102) - API Gateway using Ocelot for routing and load balancing
- **EmailService.Worker** (Port 7103) - Background worker for processing email queue
- **SQL Server** (Port 1433) - Database for storing tenant, email queue, and log data
- **RabbitMQ** (Ports 5672, 15672) - Message broker for email queue management

## Prerequisites

- Docker Desktop installed and running
- At least 4GB of available RAM
- Ports 7100-7103, 1433, 5672, and 15672 available

## Quick Start

### 1. Build and Start All Services

```bash
docker-compose up -d --build
```

### 2. Check Service Status

```bash
docker-compose ps
```

### 3. View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f admin
docker-compose logs -f gateway
docker-compose logs -f worker
```

## Service URLs

- **API**: http://localhost:7100
- **Admin Dashboard**: http://localhost:7101
- **API Gateway**: http://localhost:7102
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **SQL Server**: localhost,1433 (sa/YourStrong@Passw0rd)

## Database Initialization

The database will be created automatically on first run. To run migrations manually:

```bash
# Access API container
docker exec -it emailservice-api bash

# Run migrations
dotnet ef database update
```

## Configuration

### Environment Variables

All services are configured through environment variables in `docker-compose.yml`:

- **Database Connection**: `ConnectionStrings__DefaultConnection`
- **RabbitMQ Settings**: `RabbitMQ__HostName`, `RabbitMQ__UserName`, `RabbitMQ__Password`
- **SMTP Settings**: `EmailSettings__SmtpHost`, `EmailSettings__SmtpPort`, etc.

### Customizing Settings

Edit the `docker-compose.yml` file to modify:
- Database password
- RabbitMQ credentials
- SMTP server configuration
- Port mappings

## Data Persistence

### Volumes

The following data is persisted:

- **SQL Server Data**: `sqlserver-data` volume
- **RabbitMQ Data**: `rabbitmq-data` volume
- **Application Logs**: `./docker-volumes/` directory on host

### Log Files Location

Logs are stored in the following directories on your host machine:

```
./docker-volumes/
├── api-logs/
├── admin-logs/
├── gateway-logs/
├── worker-logs/
├── sqlserver-logs/
└── rabbitmq-logs/
```

## Common Operations

### Stop All Services

```bash
docker-compose down
```

### Stop and Remove Volumes (Clean Start)

```bash
docker-compose down -v
```

### Restart a Specific Service

```bash
docker-compose restart api
docker-compose restart worker
```

### Rebuild a Specific Service

```bash
docker-compose up -d --build api
```

### Scale Worker Instances

```bash
docker-compose up -d --scale worker=3
```

## Health Checks

### Check API Health

```bash
curl http://localhost:7100/health
```

### Check via Gateway

```bash
curl http://localhost:7102/health/api
```

### RabbitMQ Status

```bash
docker exec emailservice-rabbitmq rabbitmqctl status
```

### SQL Server Status

```bash
docker exec emailservice-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT @@VERSION"
```

## Troubleshooting

### Services Won't Start

1. Check if ports are already in use:
   ```bash
   netstat -ano | findstr "7100 7101 7102 7103 1433 5672"
   ```

2. Check Docker logs:
   ```bash
   docker-compose logs
   ```

### Database Connection Issues

1. Ensure SQL Server is healthy:
   ```bash
   docker-compose ps sqlserver
   ```

2. Wait for SQL Server to fully start (check health status)

3. Check connection string in docker-compose.yml

### RabbitMQ Connection Issues

1. Check RabbitMQ is running:
   ```bash
   docker-compose ps rabbitmq
   ```

2. Access management UI: http://localhost:15672

3. Verify credentials match in docker-compose.yml

### Worker Not Processing Emails

1. Check worker logs:
   ```bash
   docker-compose logs -f worker
   ```

2. Verify RabbitMQ has messages:
   ```bash
   docker exec emailservice-rabbitmq rabbitmqctl list_queues
   ```

## Security Notes

### Production Deployment

Before deploying to production:

1. **Change default passwords** in docker-compose.yml:
   - SQL Server SA password
   - RabbitMQ credentials

2. **Use environment files** instead of hardcoded values:
   ```bash
   docker-compose --env-file .env.production up -d
   ```

3. **Enable HTTPS** by configuring certificates

4. **Update SMTP credentials** to use secure password storage

5. **Configure proper firewall rules**

## Network Architecture

All services communicate through the `emailservice-network` bridge network:

- Services reference each other by container name
- API is accessible as `api` from gateway and worker
- Database is accessible as `sqlserver` from all services
- RabbitMQ is accessible as `rabbitmq` from API and worker

## Backup and Restore

### Backup SQL Server Database

```bash
docker exec emailservice-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "BACKUP DATABASE EmailDb TO DISK = '/var/opt/mssql/backup/EmailDb.bak'"
docker cp emailservice-sqlserver:/var/opt/mssql/backup/EmailDb.bak ./backup/
```

### Restore SQL Server Database

```bash
docker cp ./backup/EmailDb.bak emailservice-sqlserver:/var/opt/mssql/backup/
docker exec emailservice-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "RESTORE DATABASE EmailDb FROM DISK = '/var/opt/mssql/backup/EmailDb.bak' WITH REPLACE"
```

## Monitoring

### View Real-time Resource Usage

```bash
docker stats
```

### View Container Details

```bash
docker inspect emailservice-api
```

## Stopping and Cleanup

### Stop Services (Keep Data)

```bash
docker-compose down
```

### Complete Cleanup (Remove Everything)

```bash
docker-compose down -v --rmi all
rm -rf ./docker-volumes
```

## Support

For issues or questions:
- Check logs in `./docker-volumes/`
- Review Docker Compose output
- Verify all services are healthy: `docker-compose ps`
