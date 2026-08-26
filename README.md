# STAJ Backend

ASP.NET Core Web API backend for the STAJ project. The project uses JWT-based authentication, refresh tokens, role-based authorization and Entity Framework Core.

## Authentication

Authentication is handled under `/api/Auth`.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Auth/login` | Checks username/password and returns an access token and refresh token. |
| POST | `/api/Auth/refresh` | Validates a refresh token and rotates it, returning a new access token and refresh token. |
| POST | `/api/Auth/logout` | Revokes the supplied refresh token. |

### Login

Example request:

```json
{
  "kullaniciAdi": "admin",
  "sifre": "123456"
}
```

A successful login returns the user's role, an access token, a refresh token and the refresh token expiration time.

## Token Management

- Access Token lifetime: 15 minutes.
- Refresh Token lifetime: 7 days.
- Refresh tokens are stored in the database.
- Refresh token rotation is used: a successfully consumed refresh token is revoked and replaced with a new refresh token.
- Logout revokes the supplied refresh token.
- A revoked or expired refresh token cannot be used again.

## Authorization

Protected endpoints use JWT Bearer authentication.

Role-based authorization is used for administrative customer operations. Endpoints marked with `Admin` require the user's JWT role claim to be `Admin`.

Expected behavior:

- Missing/invalid authentication: `401 Unauthorized`
- Authenticated user without the required role: `403 Forbidden`
- Invalid request/model data: `400 Bad Request`
- Missing resource: `404 Not Found`

## Customer API

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/Musteri` | Lists customers. |
| POST | `/api/Musteri` | Adds a customer. |
| GET | `/api/Musteri/{id}` | Gets a customer by ID. |
| PUT | `/api/Musteri/{id}` | Updates a customer. |
| DELETE | `/api/Musteri/{id}` | Deletes a customer. |

Administrative customer operations are protected with role-based authorization.

## Security

- HTTPS is used for local API communication.
- JWT issuer, audience, signature and lifetime validation are configured.
- CORS is configured for the Angular frontend during local development.
- Refresh token revocation/blacklist behavior is implemented through the `RevokedAt` field.
- Refresh tokens are generated using cryptographically secure random bytes.

## API Documentation

The API can be explored and tested through Swagger/OpenAPI while the application is running:

`https://localhost:7233/swagger`

## Database

Entity Framework Core migrations are used for database schema changes. Refresh tokens are stored in the `RefreshTokens` table and are associated with users through a foreign key.

## Test Scenarios

The main authentication scenarios have been verified through Swagger:

1. Successful login returns `200 OK`.
2. Invalid login credentials return `401 Unauthorized`.
3. A protected endpoint without a valid token returns `401 Unauthorized`.
4. Refresh token rotation returns a new access token and refresh token.
5. Logout revokes the refresh token.
6. A revoked refresh token cannot be used again.
