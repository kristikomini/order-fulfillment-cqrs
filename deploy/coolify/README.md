# Deploying LogiFlow to Coolify

Runs the platform live at **https://logiflow.testdemo.it** using
`docker-compose.coolify.yml`: the Blazor **web** UI is the only public service,
with the **api** and **Azure SQL Edge** database reachable only on the internal
Docker network.

## 1. DNS

Add an A / AAAA record pointing `logiflow.testdemo.it` at the Coolify server's
IP (same server as `hub.testdemo.it`). Give it a minute to propagate.

## 2. Create the resource in Coolify

1. **+ New → Resource → Docker Compose** → *from a Git repository*.
2. Repository: `https://github.com/kristikomini/logiflow-platform`, branch `main`.
3. **Base directory:** `/`
4. **Compose file:** `/docker-compose.coolify.yml`
5. Save. Coolify parses the file and shows three services (web, api, sqledge).

## 3. Domain + secret

- On the **web** service, set the domain to `https://logiflow.testdemo.it`
  (this fills the `SERVICE_FQDN_WEB_8080` binding; Traefik terminates TLS).
- Add an environment variable **`MSSQL_SA_PASSWORD`** — a strong secret
  (`openssl rand -base64 24`). Mark it as a build+runtime variable.
  See `logiflow.env.example`.

## 4. Deploy

Hit **Deploy**. First boot takes a few minutes: SQL Edge initialises, then the
API waits for its healthcheck, applies EF Core migrations and seeds the product
catalogue, then the Blazor UI comes up. Watch the logs until you see
`Application started`.

## 5. Verify

- Open `https://logiflow.testdemo.it` — the dashboard should show the seeded
  products. Go to **Orders**, place one, and watch it move Confirmed → Fulfilled.

## Notes

- **Data persists** in the `sqledge-data` volume across redeploys. To reset the
  demo, delete that volume.
- **Exposing Swagger/the API** (optional): give the `api` service its own domain
  (e.g. `api.logiflow.testdemo.it`) via a `SERVICE_FQDN_API_8080` binding. Not
  needed for the UI demo.
- **Footprint:** ~1 GB RAM. If the box is tight, SQL Edge is the heavy part.
