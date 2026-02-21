# DigitalOcean Droplet Guide

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: Work package og markdown-kommandoer er synkroniseret (2026-02-21).

Formål:
Driftsguide til deployment på en Ubuntu droplet med PostgreSQL, systemd,
Nginx og HTTPS.

## 1. Opret droplet

1. Opret en Ubuntu 24.04 LTS droplet i DigitalOcean.
1. Tilføj SSH key ved oprettelse.
1. Notér offentlig IP.

## 2. Log ind og opdater droplet

Kør som `root` første gang:

```bash
ssh root@DIN_DROPLET_IP
apt update
apt -y upgrade
apt -y autoremove
reboot
```

Log ind igen efter reboot.

## 3. Opret admin-bruger og lås ned

```bash
adduser deploy
usermod -aG sudo deploy
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
chmod 600 /home/deploy/.ssh/authorized_keys
```

Skift SSH hardening i `/etc/ssh/sshd_config`:

- `PermitRootLogin no`
- `PasswordAuthentication no`

Genstart SSH:

```bash
systemctl restart ssh
```

## 4. Firewall

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
ufw status
```

## 5. Installer runtime og værktøjer

```bash
apt update
apt -y install nginx certbot python3-certbot-nginx unzip curl git
```

Installer .NET runtime 10:

```bash
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
apt update
apt -y install dotnet-runtime-10.0
```

## 6. Installer PostgreSQL

```bash
apt -y install postgresql postgresql-contrib
sudo -u postgres psql
```

I `psql`:

```sql
CREATE USER lager_user WITH PASSWORD 'SKIFT_DENNE';
CREATE DATABASE lager_db OWNER lager_user;
\q
```

## 7. Deploy app

Kør som `deploy` bruger:

```bash
mkdir -p /opt/lager
```

Upload release output til `/opt/lager/app` (scp/rsync/CI artifact).

Sæt `appsettings.Production.json`:

```json
{
  "Database": {
    "Provider": "Postgres",
    "ConnectionString": "Host=127.0.0.1;Port=5432;Database=lager_db;Username=lager_user;Password=SKIFT_DENNE"
  },
  "Auth": {
    "RequireAuthentication": true
  }
}
```

## 8. systemd service

Opret `/etc/systemd/system/lager.service`:

```ini
[Unit]
Description=LagerPalleSortering
After=network.target

[Service]
WorkingDirectory=/opt/lager/app
ExecStart=/usr/bin/dotnet /opt/lager/app/LagerPalleSortering.dll
Restart=always
RestartSec=5
User=deploy
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

Aktivér:

```bash
systemctl daemon-reload
systemctl enable lager
systemctl start lager
systemctl status lager
```

## 9. Nginx reverse proxy

Opret `/etc/nginx/sites-available/lager`:

```nginx
server {
    listen 80;
    server_name dit-domæne.dk;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Aktivér site:

```bash
ln -s /etc/nginx/sites-available/lager /etc/nginx/sites-enabled/lager
nginx -t
systemctl reload nginx
```

## 10. HTTPS med Let's Encrypt

```bash
certbot --nginx -d dit-domæne.dk
systemctl status certbot.timer
```

## 11. Verificering

```bash
curl -I https://dit-domæne.dk/app
curl https://dit-domæne.dk/health
journalctl -u lager -n 200 --no-pager
```

## 12. Opdateringsflow

1. Tag backup fra `/backup/db`.
1. Stop service: `systemctl stop lager`.
1. Upload ny release til `/opt/lager/app`.
1. Start service: `systemctl start lager`.
1. Verificér `/health` og login.

