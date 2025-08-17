# Valheim Server Docker - C# Port

This repository contains a complete C# port of the Valheim server Docker container, replacing the existing Shell, Python, Go, and HCL implementations with modern .NET 8 solutions using CQRS and vertical slice architecture.

## Architecture

The solution is organized into the following projects:

### VDS.Contracts
Contains domain models, DTOs, and CQRS command/query interfaces. This is the shared contract layer.

**Key Components:**
- `ServerConfiguration`: Server settings and parameters
- `BackupConfiguration`: Backup policies and settings
- `ModConfiguration`: Mod management configuration
- CQRS base interfaces and result types
- Command/Query definitions for all features

### VDS.Core
Contains the core domain logic and business rules organized by feature (vertical slices).

**Features:**
- **Server Management**: Start/stop/restart server, status monitoring
- **Backup Management**: Create, restore, cleanup backups with retention policies
- **Log Filtering**: Advanced log processing with regex patterns and event hooks
- **Configuration**: Environment variable to configuration mapping

**Key Services:**
- `ServerLifecycleService`: Manages server process lifecycle
- `BackupService`: Handles backup creation, restoration, and cleanup
- `LogFilterService`: Processes and filters server logs

### VDS.Infrastructure
Provides concrete implementations of interfaces defined in Core, handling external concerns.

**Key Components:**
- `ValheimServerProcessManager`: Process management using System.Diagnostics.Process
- `FileSystemBackupStorage`: Backup storage with ZIP compression
- `SteamQueryServerStatusService`: Server status via Steam query protocol

### VDS.Worker
.NET Worker Service that replaces supervisord functionality, managing server lifecycle and maintenance tasks.

**Responsibilities:**
- Server bootstrapping and startup
- Periodic backup scheduling
- Update checking
- Health monitoring
- Graceful shutdown

### VDS.Blazor
Blazor Web dashboard for server management and monitoring (replaces supervisord web interface).

**Features (Planned):**
- Real-time server status and player monitoring
- Backup management interface
- Log viewing and filtering
- Server control (start/stop/restart)
- Configuration management

### VDS.Tests
Comprehensive test suite for all components.

## Key Features Ported

### ✅ Server Lifecycle Management
- Complete port of `valheim-server` script functionality
- Process management with graceful start/stop/restart
- Mod support (ValheimPlus/BepInEx) configuration
- Environment variable configuration

### ✅ Backup Management
- Complete port of `valheim-backup` script functionality
- Scheduled and on-demand backups
- ZIP compression support
- Retention policies (age and count based)
- Idle server detection for safe backups

### ✅ Log Filtering and Event Hooks
- Complete port of Go `valheim-logfilter` functionality
- Regex-based log filtering
- Event hooks for Discord notifications, etc.
- UTF-8 validation and empty line filtering

### ✅ Configuration Management
- Environment variable to configuration mapping
- Default value handling
- Type-safe configuration objects

### ✅ Process Management
- Replaces supervisord with .NET Worker Service
- Periodic task scheduling
- Health monitoring
- Graceful shutdown handling

## Usage

### Environment Variables

The Worker Service supports all the same environment variables as the original implementation:

**Server Configuration:**
- `WORLD_NAME`: World name (default: "Dedicated")
- `SERVER_NAME`: Server name (default: "My Server")
- `SERVER_PORT`: Server port (default: 2456)
- `SERVER_PASS`: Server password (default: "secret")
- `SERVER_PUBLIC`: Public visibility (default: 1)
- `CROSSPLAY`: Enable crossplay (default: false)

**Backup Configuration:**
- `BACKUPS`: Enable backups (default: false)
- `BACKUPS_INTERVAL`: Backup interval in seconds (default: 3600)
- `BACKUPS_DIRECTORY`: Backup directory (default: "/opt/valheim/backups")
- `BACKUPS_MAX_AGE`: Max backup age in days (default: 3)
- `BACKUPS_IF_IDLE`: Only backup when idle (default: true)

### Running the Worker Service

```bash
dotnet run --project src/VDS.Worker
```

### Configuration File

The service can be configured via `appsettings.json`:

```json
{
  "Server": {
    "WorldName": "MyWorld",
    "ServerName": "My Valheim Server",
    "ServerPort": 2456,
    "ServerPassword": "mypassword"
  },
  "Backup": {
    "Enabled": true,
    "IntervalSeconds": 3600,
    "MaxAgeDays": 7
  }
}
```

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Running with Docker

The Worker Service can be containerized and run as a replacement for the existing supervisord-based solution.

## Migration from Original Implementation

This C# port maintains full compatibility with the existing Docker container interface while providing:

1. **Better Performance**: Native .NET runtime vs script interpretation
2. **Type Safety**: Compile-time checking vs runtime script errors
3. **Better Debugging**: Full debugging support and stack traces
4. **Maintainability**: Modern IDE support, refactoring tools, etc.
5. **Extensibility**: Plugin architecture and dependency injection

## Future Enhancements

- **Blazor Dashboard**: Web-based management interface
- **Docker Integration**: Direct Docker API management
- **Metrics and Monitoring**: Prometheus/Grafana integration
- **Plugin System**: Extensible mod and feature system
- **Configuration Hot Reload**: Runtime configuration updates

## Architecture Decisions

### CQRS (Command Query Responsibility Segregation)
- Clear separation between read and write operations
- Improved testability and maintainability
- Scalable architecture for future enhancements

### Vertical Slice Architecture
- Features organized by business capability
- Reduces coupling between features
- Easier to understand and modify

### Dependency Injection
- Loose coupling between components
- Easy testing with mocks
- Configuration management

### Result Pattern
- Explicit error handling without exceptions
- Better performance and predictability
- Clear success/failure semantics