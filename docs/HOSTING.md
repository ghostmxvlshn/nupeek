# Hosting Nupeek Landing Page

Nupeek uses GitHub Pages to host `web/`.

## Deploy model

- Workflow: `.github/workflows/pages.yml`
- Trigger: push to `main` when `web/**` changes
- Published directory: `web/`

After first run, site URL will appear in:
- GitHub repo → Settings → Pages
- workflow job output (`deployment.page_url`)

## Custom domain (optional)

Recommended domain options:
- `nupeek.dev`
- `nupeek.tools`
- `www.nupeek.dev`

### DNS for apex/root domain (e.g., `nupeek.dev`)

Add records:

- `A` → `185.199.108.153`
- `A` → `185.199.109.153`
- `A` → `185.199.110.153`
- `A` → `185.199.111.153`
- `AAAA` → `2606:50c0:8000::153`
- `AAAA` → `2606:50c0:8001::153`
- `AAAA` → `2606:50c0:8002::153`
- `AAAA` → `2606:50c0:8003::153`

### DNS for `www` subdomain

- `CNAME` → `<your-github-username>.github.io`

For this repo owner:
- `CNAME` → `ghostmxvlshn.github.io`

## GitHub Pages settings

1. Repo Settings → Pages
2. Source: GitHub Actions
3. (Optional) Set custom domain
4. Enable "Enforce HTTPS"

## Optional CNAME file

If you decide on a domain, add `web/CNAME` containing one line, e.g.:

```text
nupeek.dev
```

Then commit/push and redeploy.
