# Events.ClientApp

`Events.ClientApp` is the Vue 3 front-end for Topic 2.

It uses:

- Vite
- Vue 3
- PrimeVue
- Auth0 Vue SDK

It is intended as a companion UI for the `Events.WebAPI` backend and demonstrates how the secured API can be consumed from a browser application.

## Scripts

Install dependencies:

```powershell
npm install
```

Start the development server:

```powershell
npm run dev
```

Build for production:

```powershell
npm run build
```

Preview the production build:

```powershell
npm run preview
```

By default, the Vite dev server runs on:

- `http://localhost:5173`

## Environment Configuration

The app reads configuration from Vite environment files.

Typical options are:

- `.env`
- `.env.local`

The project already includes:

- `.env.example`

The simplest setup is to copy `.env.example` to `.env.local` and fill in the real values.

Example:

```powershell
Copy-Item Topic2\Events.ClientApp\.env.example Topic2\Events.ClientApp\.env.local
```

## Environment Variables

### Required for Auth0 login

- `VITE_AUTH0_DOMAIN`
  Auth0 tenant domain, for example `fer-web2.eu.auth0.com`

- `VITE_AUTH0_CLIENT_ID`
  Auth0 client ID for the SPA application

### Optional Auth0 settings

- `VITE_AUTH0_AUDIENCE`
  API audience passed to Auth0 when requesting an access token

- `VITE_AUTH0_SCOPE`
  Space-separated scopes requested during login

### API configuration

- `VITE_API_BASE_URL`
  Base URL of the Web API

If `VITE_API_BASE_URL` is not set, the app falls back to:

- `https://localhost:7150`

## Example

```env
VITE_AUTH0_DOMAIN=fer-web2.eu.auth0.com
VITE_AUTH0_CLIENT_ID=whed5Hdb8l1b1fGyyAz7Qrdsb2oKcSh3
VITE_AUTH0_AUDIENCE=https://erasmus-sta-2026/events-api
VITE_AUTH0_SCOPE=openid profile email events:read events:write
VITE_API_BASE_URL=https://localhost:7150
```

## Notes

- `VITE_AUTH0_DOMAIN` and `VITE_AUTH0_CLIENT_ID` are required if you want the Auth0 login flow to work.
- `VITE_AUTH0_AUDIENCE` and `VITE_AUTH0_SCOPE` are optional in code, but usually needed if the API expects bearer tokens with a specific audience and scopes.
- `VITE_API_BASE_URL` should point to the running `Events.WebAPI` instance for local development.
- `.env.local` is for local development and should not be treated as a shared secrets file.

## What The Client Demonstrates

- login and token acquisition through Auth0
- calling the secured Topic 2 API
- local development against a separately running ASP.NET Core backend
