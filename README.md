# Indian Chemist - ASP.NET Core MVC

ASP.NET Core 8 MVC application with Razor views and a controller-based REST API built from the supplied requirements. No database, Node build, or database installation is required. Accounts and products persist to App_Data/data.json; signing keys persist to App_Data/jwt.key. Location reference data is defined in Repositories/JsonFileRepository.cs (India and US sample locations).

## Run

Install the .NET 8 SDK, then open PowerShell:

```powershell
cd D:\Assignment\IndianChemist
dotnet restore
dotnet run --launch-profile http
```

Open http://localhost:5080. The Development launch profile enables local email previews.

## First use

1. Create an account. Passwords need 10–128 characters.
2. With no SMTP configured, a Development email preview appears after registration. Click its verification link, then sign in.
3. Ordinary accounts receive the User role and can view products.
4. To test Admin access, register admin@indianchemist.local and verify it through the same preview. Choose your own password; no account or password is pre-created.
5. Admin accounts can add, edit, view, and delete products. All product writes are also protected in the API.

Set Admin:Email in appsettings.json to your own email **before registering** to designate a different administrator. Existing account roles do not automatically change when configuration changes. Public registration cannot request an Admin role.

## Email delivery and production

Set Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password, and Smtp:From. SMTP uses TLS. Set secrets using environment variables, for example Smtp__Password, rather than committing passwords. Set PublicOrigin to the trusted external HTTPS origin, and update AllowedHosts for the hostname. Run with ASPNETCORE_ENVIRONMENT=Production on an ASP.NET-capable host behind HTTPS. Production requires SMTP and never exposes verification links. Actual external email delivery has not been tested without SMTP credentials.

The JSON file store is intended for this assignment and a single running server process. It serializes writes and replaces the data file atomically. Protect App_Data with OS access permissions and back it up together with the signing key. It is not suitable for multiple replicas or shared network storage. JWT access tokens remain valid for up to 15 minutes after logout; logout immediately revokes refresh access. One refresh session per account is supported; signing in elsewhere invalidates the previous refresh session.

## API

JSON bodies use camelCase. All product and location endpoints require Authorization: Bearer <accessToken>. Product POST/PUT/DELETE require Admin. The frontend keeps short-lived access tokens in memory, and the rotated refresh token is held in an HttpOnly, SameSite=Strict cookie (Secure over HTTPS).

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | /api/auth/register | {name,email,password}; sends verification email |
| GET | /api/auth/verify?token=... | Verify email; link valid for 24 hours, single use |
| POST | /api/auth/login | {email,password}; verified accounts only |
| POST | /api/auth/refresh | Rotate refresh cookie and obtain a new JWT |
| POST | /api/auth/logout | Revoke refresh token and clear cookie |
| GET | /api/products | List all products |
| GET | /api/products/{id} | View a product |
| POST | /api/products | Create a product |
| PUT | /api/products/{id} | Update a product |
| DELETE | /api/products/{id} | Delete a product |
| GET | /api/countries | Country choices |
| GET | /api/countries/{id}/states | States for selected country |
| GET | /api/states/{id}/cities | Cities for selected state |

Product example:

```json
{"countryId":1,"stateId":1,"cityId":1,"name":"Sample product","email":"contact@example.com","description":"Product description","price":25.5,"quantity":4,"sex":"Other"}
```

The server assigns Id. Required fields, positive decimal price, positive integer quantity, valid email, predefined Sex values, and location relationships are validated server-side. The frontend validates input and resets dependent dropdowns when the parent changes. Missing products return 404; unauthenticated access returns 401 and unauthorized writes return 403. Registration and login are rate-limited.

## Verification

`dotnet build` and `node --check wwwroot/app.js` pass. Integration and browser checks are included in tests/. Run integration.py against a Development server with the default admin email on disposable data; it creates test accounts. Browser checks use Playwright and installed Microsoft Edge; update the require path for your environment. They cover login, product creation/deletion/details, cascading reset, and mobile overflow. Test screenshots are in tests/.

No deployment was performed: Sites hosting cannot execute the required ASP.NET backend. Run locally or publish to an ASP.NET-capable host with persistent writable storage.


## MVC architecture

- Models/: product, account, country, state, city, request models, and WorkspaceViewModel.
- Views/Home/Index.cshtml: strongly typed Razor workspace view.
- Views/Home/_AccountForms.cshtml and _ProductDialogs.cshtml: reusable forms.
- Views/Shared/_Layout.cshtml: shared HTML shell and versioned CSS/JavaScript assets.
- Controllers/HomeController.cs: renders the workspace using the MVC view engine.
- Controllers/AuthController.cs: registration, login, verification, refresh, and logout API actions.
- Controllers/ProductsController.cs: product CRUD with Admin authorization attributes on writes.
- Controllers/LocationsController.cs: cascading location API actions.
- Services/: authentication, product validation and operations, and location selection.
- Repositories/JsonFileRepository.cs: JSON persistence and reference data, injected as a singleton to share the file lock.
- wwwroot/: JavaScript and CSS; there is no static index.html.
- Program.cs: dependency registration, middleware, JWT configuration, and MVC routing.

Requests to / or /Home/Index run HomeController.Index, which passes WorkspaceViewModel to the Razor view. JavaScript in the rendered view consumes /api endpoints implemented by API controllers. Controllers use services and the JSON repository. Existing API URLs, data files, and functionality remain compatible.

The build, API integration suite, and browser suite passed after this MVC conversion. For isolated tests, run a separate Development instance with DataDirectory set to a temporary absolute directory and --urls http://localhost:5081. Set TEST_BASE_URL=http://localhost:5081 when running tests to keep test accounts out of normal App_Data.
