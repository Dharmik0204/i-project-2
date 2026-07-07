# AERODYNE Compressors - ASP.NET Core MVC

Industrial air compressor marketing portal with contact forms, energy savings calculator, and SMTP email integration.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB for development, Azure SQL or external SQL Server for production)
- SMTP credentials (Gmail App Password or corporate SMTP)

## Local Development

1. Update `appsettings.Development.json` with your SMTP password (never commit secrets).
2. Run the application:

```bash
dotnet restore
dotnet run
```

3. Open `https://localhost:7241` or the URL shown in the console.

## Production / Render Deployment

This project includes a `Dockerfile` and `render.yaml` for [Render](https://render.com) deployment.

### Required environment variables on Render

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__ApplicationDbContextConnection` | SQL Server connection string |
| `SmtpSettings__Server` | e.g. `smtp.gmail.com` |
| `SmtpSettings__Port` | e.g. `587` |
| `SmtpSettings__SenderEmail` | SMTP sender address |
| `SmtpSettings__ReceiverEmail` | Inbox for contact/calculation leads |
| `SmtpSettings__Password` | SMTP app password |
| `SmtpSettings__EnableSsl` | `true` |

Render automatically sets `PORT`; the app binds to it when present.

### Deploy with Render Blueprint

1. Push this repo to GitHub.
2. In Render, create a **Blueprint** from `render.yaml`.
3. Set the secret environment variables in the Render dashboard.
4. Deploy.

### Deploy with Docker manually

```bash
docker build -t aerodyne-compressors .
docker run -p 8080:8080 \
  -e ConnectionStrings__ApplicationDbContextConnection="YOUR_CONNECTION_STRING" \
  -e SmtpSettings__Server="smtp.gmail.com" \
  -e SmtpSettings__SenderEmail="your@email.com" \
  -e SmtpSettings__ReceiverEmail="your@email.com" \
  -e SmtpSettings__Password="your-app-password" \
  aerodyne-compressors
```

## Security

- Anti-forgery tokens are enforced on all POST forms.
- SMTP and database credentials are loaded from environment variables in production.
- HTTPS is supported via Render TLS termination and forwarded headers.
