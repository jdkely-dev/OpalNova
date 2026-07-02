# OPALNOVA Nivoda Bridge - Azure App Service Setup

This guide is for hosting the bridge at:

```text
https://api.jackthejeweller.com.au
```

The Wix site remains:

```text
https://jackthejeweller.com.au
```

## 1. Create The Azure App

In Azure Portal, create an App Service for the bridge.

Recommended starting settings:

| Setting | Value |
| --- | --- |
| Publish | Code |
| Runtime stack | .NET |
| Operating system | Windows or Linux |
| Region | Closest practical region |
| Pricing plan | Start small, then scale if needed |
| HTTPS Only | On |

Use an app name such as:

```text
opalnova-nivoda-bridge
```

Azure will first give the app a temporary URL similar to:

```text
https://opalnova-nivoda-bridge.azurewebsites.net
```

## 2. Add Environment Variables

In the Azure App Service:

```text
Settings -> Environment variables -> App settings -> Add
```

Add these values:

| Name | Value |
| --- | --- |
| `NIVODA_ENDPOINT` | Nivoda endpoint supplied by Nivoda. Use staging until production is confirmed. |
| `NIVODA_USERNAME` | Nivoda API username. |
| `NIVODA_PASSWORD` | Nivoda API password. |
| `OPALNOVA_BRIDGE_API_KEY` | Long random secret shared only with Wix backend and OPALNOVA. |
| `OPALNOVA_ALLOWED_ORIGINS` | `https://jackthejeweller.com.au,https://www.jackthejeweller.com.au` |

Apply/save the changes. Azure restarts the app when app settings change.

## 3. Deploy The Bridge

Deploy this project folder:

```text
OpalNova.NivodaBridge
```

The project is an ASP.NET API bridge. It should expose:

```text
GET /health
POST /nivoda/search
POST /nivoda/schema
```

After deployment, check the temporary Azure URL:

```text
https://opalnova-nivoda-bridge.azurewebsites.net/health
```

Expected result:

```text
status: ok
nivodaCredentialsConfigured: true
apiKeyRequired: true
productionSafe: true
```

If `productionSafe` is false, fix the missing Azure environment variable before connecting Wix.

## 4. Connect The Custom Domain

In Azure App Service, add the custom domain:

```text
api.jackthejeweller.com.au
```

Azure will ask for DNS records. For a subdomain, this is usually:

| Type | Host/name | Value |
| --- | --- | --- |
| CNAME | `api` | Azure app default hostname, for example `opalnova-nivoda-bridge.azurewebsites.net` |
| TXT | `asuid.api` | Azure custom domain verification ID |

Add those DNS records wherever `jackthejeweller.com.au` DNS is managed.

Back in Azure, validate the domain and add it.

## 5. Enable HTTPS

Add an App Service managed certificate for:

```text
api.jackthejeweller.com.au
```

Then bind it to the custom domain so the API works at:

```text
https://api.jackthejeweller.com.au/health
```

Do not give Nivoda the URL until HTTPS is working cleanly.

## 6. Set Wix Secrets

In Wix Secrets Manager, set:

| Secret name | Value |
| --- | --- |
| `OPALNOVA_NIVODA_BRIDGE_URL` | `https://api.jackthejeweller.com.au` |
| `OPALNOVA_BRIDGE_API_KEY` | Same long random secret as Azure |

Wix public page code should call the Wix backend function only. The Wix backend function then calls:

```text
POST https://api.jackthejeweller.com.au/nivoda/search
X-OPALNOVA-BRIDGE-KEY: value-from-wix-secrets
```

## 7. What To Give Nivoda

Once the Azure custom domain is live and `/health` works, give Nivoda:

```text
Production API-calling domain:
https://api.jackthejeweller.com.au
```

Also tell them:

```text
Customer website:
https://jackthejeweller.com.au

Nivoda credentials are stored server-side in Azure App Service.
Browser clients do not call Nivoda directly.
Wix backend calls the OPALNOVA bridge, and the bridge calls Nivoda.
```

