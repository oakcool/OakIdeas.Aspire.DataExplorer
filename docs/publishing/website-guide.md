# Website guide

The public website source for OakIdeas.Aspire.DataExplorer lives in `site/`.

## Local preview

You can preview the landing page with any static file server from the repository root, for example:

```bash
python -m http.server 8080 --directory site
```

Then open `http://localhost:8080`.

## Publishing

GitHub Pages deployment is defined in `.github/workflows/pages.yml`.

- The workflow uploads `site/` as the Pages artifact.
- Deployments run automatically on pushes to `main` that touch the site or its linked docs metadata.
- You can also trigger the workflow manually with **workflow_dispatch**.
- The public URL is [https://dataexplorer.oakideas.com](https://dataexplorer.oakideas.com), served via the `site/CNAME` entry.

## Screenshots

Website screenshots live in `site/assets/screenshots/`.

When updating them:

1. Capture new images from the current UI using development-safe sample data.
2. Keep browser sizing consistent across screenshots.
3. Verify that no secrets, connection strings, or private machine details appear.
4. Replace the existing image files so the website keeps stable paths.

## Keeping content aligned with docs

The website is a polished summary, not a separate source of truth.

- Use `README.md` and the files under `docs/` for authoritative technical details.
- Update website copy whenever public-facing behavior, setup steps, or links change.
- Prefer linking to detailed documentation instead of duplicating long-form technical guidance in the landing page.
