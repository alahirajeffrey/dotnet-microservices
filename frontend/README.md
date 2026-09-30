# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

````js
export default defineConfig([
  # Frontend

  React 19, TypeScript, Vite, and Tailwind CSS v4 application for the microservices stack.

  ## Run locally

  ```bash
  npm install
  npm run dev
````

The Vite dev server runs at `http://localhost:5173`. The API client targets the backend gateway at `http://localhost:5000` by default. Copy `.env.example` to `.env.local` to override `VITE_API_BASE_URL`.

## Checks

```bash
npm run lint
npm run build
```

Use `src/lib/api.ts` for gateway requests. Pass route paths such as `/api/v1/Product` to `apiRequest`; it prefixes the configured gateway URL.
},
