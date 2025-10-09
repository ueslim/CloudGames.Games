# CloudGames.Games - Dual JWT Authentication

This document explains the dual JWT authentication implementation in the CloudGames.Games microservice.

## Overview

CloudGames.Games supports **two authentication modes** that automatically switch based on configuration:

1. **Development Mode**: Validates JWT tokens from CloudGames.Users microservice using symmetric key (HS256)
2. **Production Mode**: Validates JWT tokens from Azure AD (Entra ID) using OIDC discovery

## Authentication Modes

### 1. Development Mode (Local Development)

**Purpose**: Allows CloudGames.Games to validate JWT tokens issued by the CloudGames.Users microservice when running locally.

**Configuration** (`appsettings.Development.json`):
```json
{
  "Jwt": {
    "Issuer": "CloudGames",
    "Audience": "CloudGamesUsers",
    "Key": "my-super-secret-key-1234567890-ABCD"
  }
}
```

**How it works**:
- Uses symmetric key validation (HS256 algorithm)
- Validates tokens with the shared secret key
- Issuer must be "CloudGames"
- Audience must be "CloudGamesUsers"
- Allows HTTP (RequireHttpsMetadata = false)
- No ClockSkew tolerance for precise expiration

**When to use**:
- Local development with both CloudGames.Users and CloudGames.Games running
- Integration testing with microservices
- Docker Compose local environments

---

### 2. Production Mode (Azure with Entra ID)

**Purpose**: Validates JWT tokens issued by Azure AD (Entra ID) in production Azure environments.

**Configuration** (`appsettings.json`):
```json
{
  "Jwt": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "Audience": "api://cloudgames-games"
  }
}
```

**How it works**:
- Uses asymmetric key validation with OIDC discovery
- Fetches public keys from Azure AD's JWKS endpoint
- Validates issuer automatically from OIDC metadata
- Requires HTTPS (RequireHttpsMetadata = true)
- No dependency on CloudGames.Users service

**When to use**:
- Production deployments on Azure
- Azure App Service, Azure Container Apps, AKS
- When using Azure AD for authentication

---

## Configuration Detection

The authentication mode is **automatically selected** based on which configuration values are present:

```csharp
if (!string.IsNullOrWhiteSpace(jwtKey))
{
    // Development Mode: Symmetric Key Validation
}
else if (!string.IsNullOrWhiteSpace(jwtAuthority))
{
    // Production Mode: Azure AD Validation
}
else
{
    // Error: No valid configuration found
}
```

**Priority**:
1. If `Jwt:Key` is present → **Development Mode**
2. Else if `Jwt:Authority` is present → **Production Mode**
3. Else → **Throws InvalidOperationException**

---

## How to Test Locally

### Step 1: Start CloudGames.Users

```bash
cd CloudGames.Users/CloudGames.Users.Web
dotnet run
```

The Users service should start on `https://localhost:5001` (or configured port).

### Step 2: Register a User and Get JWT Token

**Register a new user:**
```bash
POST https://localhost:5001/api/users
Content-Type: application/json

{
  "name": "Test User",
  "email": "test@cloudgames.com",
  "password": "Test123!",
  "role": "Player"
}
```

**Login to get JWT token:**
```bash
POST https://localhost:5001/api/users/login
Content-Type: application/json

{
  "email": "test@cloudgames.com",
  "password": "Test123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI...",
  "user": {
    "id": "...",
    "name": "Test User",
    "email": "test@cloudgames.com",
    "role": "Player"
  }
}
```

Copy the `token` value from the response.

### Step 3: Start CloudGames.Games

```bash
cd CloudGames.Games/CloudGames.Games.Api
dotnet run
```

The Games service should start on `https://localhost:7001` (or configured port).

You should see console output confirming the authentication mode:
```
[JWT Auth] Development Mode: Symmetric Key Validation
[JWT Auth] Issuer: CloudGames
[JWT Auth] Audience: CloudGamesUsers
```

### Step 4: Call Protected Endpoints

**Using cURL:**
```bash
curl -X GET "https://localhost:7001/api/games/search?query=adventure" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

**Using PowerShell:**
```powershell
$token = "YOUR_JWT_TOKEN_HERE"
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "https://localhost:7001/api/games/search?query=adventure" -Headers $headers
```

**Using Swagger UI:**
1. Navigate to `https://localhost:7001/swagger`
2. Click the "Authorize" button (🔒 icon)
3. Enter: `Bearer YOUR_JWT_TOKEN_HERE`
4. Click "Authorize"
5. Try the `/api/games/search` endpoint

**Expected Success Response:**
```json
[
  {
    "id": "...",
    "title": "...",
    "genre": "Adventure",
    ...
  }
]
```

**Expected Failure (No Token):**
```
HTTP 401 Unauthorized
```

**Expected Failure (Invalid Token):**
```
HTTP 401 Unauthorized
```

---

## JWT Token Structure

The JWT tokens from CloudGames.Users contain the following claims:

```json
{
  "sub": "user-guid",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "user-guid",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "User Name",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "user@email.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Player",
  "iss": "CloudGames",
  "aud": "CloudGamesUsers",
  "exp": 1234567890,
  "nbf": 1234567890
}
```

**Key Claims**:
- `sub` / `nameidentifier`: User ID
- `name`: User's display name
- `emailaddress`: User's email
- `role`: User's role (Player, Admin, etc.)
- `iss`: Issuer (must be "CloudGames")
- `aud`: Audience (must be "CloudGamesUsers")

---

## Troubleshooting

### Issue: "JWT configuration is missing"

**Cause**: Neither `Jwt:Key` nor `Jwt:Authority` is configured.

**Solution**: Ensure `appsettings.Development.json` has the `Jwt:Key` configuration.

### Issue: 401 Unauthorized with Valid Token

**Possible Causes**:
1. **Secret Mismatch**: The `Jwt:Key` in Games doesn't match `JwtSettings:Secret` in Users
2. **Issuer/Audience Mismatch**: Check that both services use the same values
3. **Token Expired**: Tokens are valid for 8 hours by default
4. **Wrong Environment**: Running in Production mode but using Development token

**Verification**:
```bash
# Check CloudGames.Users config
cat CloudGames.Users/CloudGames.Users.Web/appsettings.Development.json

# Check CloudGames.Games config
cat CloudGames.Games/CloudGames.Games.Api/appsettings.Development.json

# Both should have matching values:
# Secret/Key: "my-super-secret-key-1234567890-ABCD"
# Issuer: "CloudGames"
# Audience: "CloudGamesUsers"
```

### Issue: Token Works in Users but not in Games

**Cause**: Configuration mismatch between services.

**Solution**:
1. Verify both services are using the same secret key
2. Verify Issuer and Audience match exactly (case-sensitive)
3. Check console output when Games starts to see which mode is active
4. Try decoding the JWT at [jwt.io](https://jwt.io) to inspect claims

### Issue: Cannot Validate Signature

**Cause**: The signing key in CloudGames.Users differs from the validation key in CloudGames.Games.

**Solution**: Ensure both services use exactly the same value for the secret key:
- CloudGames.Users: `JwtSettings:Secret`
- CloudGames.Games: `Jwt:Key`

---

## Production Deployment

When deploying to Azure:

1. **Remove or Override Development Config**: Ensure `Jwt:Key` is not present in production
2. **Configure Azure AD**: Set `Jwt:Authority` and `Jwt:Audience` in App Service configuration
3. **Use Key Vault**: Store sensitive values in Azure Key Vault
4. **Enable HTTPS**: Production mode requires HTTPS (RequireHttpsMetadata = true)

**Example Azure App Service Configuration**:
```
Jwt__Authority=https://login.microsoftonline.com/your-tenant-id/v2.0
Jwt__Audience=api://cloudgames-games
```

---

## Controller Authorization

All endpoints in `GamesController` are protected with the `[Authorize]` attribute:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    // All endpoints require authentication
}
```

This means:
- ✅ Valid JWT token required for all endpoints
- ❌ No anonymous access allowed
- ✅ Works with both Development and Production authentication modes
- ✅ Controllers don't need to know which mode is active

To allow anonymous access to specific endpoints, add `[AllowAnonymous]`:

```csharp
[AllowAnonymous]
[HttpGet("public")]
public IActionResult PublicEndpoint()
{
    return Ok("This endpoint is public");
}
```

---

## Security Considerations

### Development Mode
- ⚠️ Uses symmetric key (shared secret)
- ⚠️ Allows HTTP connections
- ⚠️ Secret key is in configuration files
- ✅ Only for local development
- ✅ Not suitable for production

### Production Mode
- ✅ Uses asymmetric keys (public/private)
- ✅ Requires HTTPS
- ✅ Keys managed by Azure AD
- ✅ No shared secrets
- ✅ Production-ready

---

## References

- [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Azure AD Authentication](https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-protocols-oidc)
- [JWT.io - Token Inspector](https://jwt.io)

