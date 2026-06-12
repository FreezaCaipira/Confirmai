# Getting Started Guide

## Prerequisites
- .NET 9.0 SDK
- PostgreSQL 15+
- Node.js 20+ (for E2E tests)

## Setup Instructions

### 1. Clone Repository
```bash
git clone <repository-url>
cd Confirmai
```

### 2. Database Setup
```bash
# Start PostgreSQL service
sudo systemctl start postgresql

# Create database and user
sudo -u postgres psql -c "CREATE USER confirmai WITH PASSWORD 'your-password';"
sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER confirmai;"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE Confirmai TO confirmai;"
```

### 3. Configure Environment
Create `appsettings.Development.json` with your settings:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=Confirmai;Username=confirmai;Password=your-password"
  }
}
```

### 4. Run Migrations
```bash
dotnet ef database update
```

### 5. Start Application
```bash
dotnet watch run
```

## Development Workflow
1. Create feature branch
2. Implement changes
3. Run tests: `dotnet test`
4. Create pull request

## Testing
- Unit tests: `Confirmai.Tests` project
- E2E tests: `e2e` folder (Playwright)
- Run all tests: `dotnet test`
