# System Overview

## Project Purpose
Confirmai is a sports event management platform that facilitates group organization, event scheduling, payment processing via Bitcoin and PIX, and administrative tools for managing tournaments and activities.

## Core Modules
1. **Event Management** - Futsal and poker tournament scheduling with confirmation systems
2. **Group Management** - Private groups with role-based access control
3. **Payment Processing** - Multi-gateway support for Bitcoin and PIX payments
4. **User Management** - Identity-based authentication with role-based access control
5. **Administrative Tools** - Payment reconciliation, audit logging, and system monitoring

## Technology Stack
- **Framework**: ASP.NET Core 9.0 Blazor Server
- **Database**: PostgreSQL
- **Frontend**: Blazor with CSS isolation
- **Payment Gateways**: BTCPay, AbacatePay, EfiBank, Appmax
- **Monitoring**: Serilog, OpenTelemetry

## Architecture Highlights
- Clean separation between UI (Pages/Shared) and business logic (Services)
- Event-driven architecture with webhook integration
- Comprehensive audit trail for critical operations
- Multi-tenant design with group-based isolation
