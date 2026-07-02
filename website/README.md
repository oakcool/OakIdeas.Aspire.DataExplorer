# OakIdeas.Aspire.DataExplorer — Website

Public landing page for [OakIdeas.Aspire.DataExplorer](https://github.com/oakcool/OakIdeas.Aspire.DataExplorer), hosted at [dataexplorer.oakideas.com](https://dataexplorer.oakideas.com).

## Technology stack

| Tool | Purpose |
|------|---------|
| React 19 | UI framework |
| TypeScript | Type safety |
| Vite | Build tool and dev server |
| Tailwind CSS v4 | Styling (via `@tailwindcss/vite` plugin) |
| ESLint | Linting |
| GitHub Pages | Hosting |

## Project structure

```text
website/
  public/
    assets/
      screenshots/    # Application screenshots
      logo.svg        # Product logo
    favicon.png       # Browser favicon
    favicon.svg       # SVG favicon
    CNAME             # Custom domain (dataexplorer.oakideas.com)
  src/
    components/       # Reusable UI components
    data/             # Structured content data (features, screenshots, nav)
    layouts/          # Page shell (MainLayout)
    pages/            # Page-level views (HomePage)
    styles/           # Global CSS with Tailwind entry point
    App.tsx           # Application root
    main.tsx          # Entry point
  index.html          # HTML shell
  package.json
  tsconfig.json
  tsconfig.app.json
  tsconfig.node.json
  vite.config.ts
  eslint.config.js
  .gitignore
  README.md           # This file
```

## Prerequisites

- [Node.js](https://nodejs.org/) 20 or later
- npm 10 or later (included with Node.js 20)

## Development

Install dependencies:

```bash
cd website
npm install
```

Start the local dev server:

```bash
npm run dev
```

Then open [http://localhost:5173](http://localhost:5173).

## Build

```bash
npm run build
```

Output is written to `website/dist/`. This folder is **not committed** — it is generated during the publish workflow.

## Preview

Preview the production build locally:

```bash
npm run preview
```

## Lint

```bash
npm run lint
```

## Validate

Run lint and build together:

```bash
npm run validate
```

This is the same sequence run by CI before deployment.

## Deployment notes

The website is deployed to GitHub Pages using the workflow at `.github/workflows/pages.yml`.

The workflow:
1. Checks out the repository.
2. Sets up Node.js.
3. Installs dependencies with `npm ci`.
4. Runs `npm run validate` (lint + build).
5. Uploads `website/dist` as the Pages artifact.
6. Deploys to GitHub Pages.

The custom domain `dataexplorer.oakideas.com` is configured via `website/public/CNAME`, which Vite copies into `dist/` during build.

Deployments trigger automatically on pushes to `main` that touch `website/**` or `.github/workflows/pages.yml`. Manual deploys can be triggered with `workflow_dispatch` from the Actions tab.

## Screenshots and content updates

Application screenshots live in `website/public/assets/screenshots/`.

When updating screenshots:
1. Capture new images from the current UI using development-safe sample data.
2. Keep browser sizing consistent across screenshots.
3. Verify that no secrets, connection strings, or private machine details appear.
4. Replace the existing image files to keep stable paths.

Website copy is kept in sync with `README.md` and the files under `docs/`. Prefer linking to detailed documentation rather than duplicating long-form technical content in the landing page.

## Troubleshooting

**`npm run dev` fails to start**
Ensure Node.js 20+ is installed (`node --version`) and dependencies are installed (`npm install`).

**Build fails with TypeScript errors**
Run `npx tsc --noEmit` in the `website/` directory to see detailed type errors.

**Styles not applying**
Tailwind CSS v4 is configured via the `@tailwindcss/vite` plugin — no separate `tailwind.config.ts` or `postcss.config.js` is needed. Ensure `src/styles/index.css` contains `@import "tailwindcss";` and is imported from `src/main.tsx`.

**Screenshots not loading locally**
Screenshots are served from `website/public/assets/screenshots/`. Run `npm run dev` from the `website/` directory and confirm the files exist.
