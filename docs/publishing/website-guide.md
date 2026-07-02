# Website guide

The public website for OakIdeas.Aspire.DataExplorer is a React single page application located in `website/`.

The site is published to [https://dataexplorer.oakideas.com](https://dataexplorer.oakideas.com) via GitHub Pages.

## Technology stack

- **React 19** — UI framework
- **TypeScript** — Type safety
- **Vite** — Build tool and dev server
- **Tailwind CSS v4** — Styling via `@tailwindcss/vite` plugin
- **ESLint** — Linting

## Local development

```bash
cd website
npm install
npm run dev
```

Then open [http://localhost:5173](http://localhost:5173).

## Local preview (production build)

```bash
cd website
npm run build
npm run preview
```

## Validation

Run lint and build together:

```bash
cd website
npm run validate
```

## Publishing

GitHub Pages deployment is defined in `.github/workflows/pages.yml`.

The workflow:
1. Checks out the repository.
2. Sets up Node.js 20.
3. Installs dependencies with `npm ci`.
4. Runs `npm run validate` (lint + TypeScript check + Vite build).
5. Uploads `website/dist` as the Pages artifact.
6. Deploys to GitHub Pages.

Deployments trigger automatically on pushes to `main` that touch `website/**` or `.github/workflows/pages.yml`. You can also trigger manually with **workflow_dispatch**.

The custom domain `dataexplorer.oakideas.com` is configured via `website/public/CNAME`, which Vite copies into `dist/` automatically during build.

## Screenshots

Website screenshots live in `website/public/assets/screenshots/`.

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
- Content data lives in `website/src/data/` — update `features.ts`, `screenshots.ts`, and `navigation.ts` as needed.
