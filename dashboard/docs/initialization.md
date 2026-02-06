# Betterfit Dashboard - Documento di Inizializzazione

## 1) Contesto rapido

Questo progetto contiene la dashboard staff di Betterfit.

Stack attuale:

- `SvelteKit` con `SSR disattivato` (`src/+layout.ts`) e comportamento SPA.
- `shadcn-svelte` per i componenti UI (`src/lib/components/ui`).
- `Tailwind CSS v4` con tokens globali in `src/routes/layout.css`.
- `@tanstack/svelte-query` per fetching e caching dati.
- `Paraglide JS` per i18n (`messages/*.json`, output in `src/lib/paraglide`).

## 2) Stato funzionale attuale

- Brand guideline disponibile in: `/Users/daniele/Documents/src/gymbro/docs/brand-guideline-betterfit.md`.
- Documento layout dashboard disponibile in: `/Users/daniele/Documents/src/gymbro/docs/dashboard-layout-completo.md`.
- Componenti base UI installati e presenti in `src/lib/components/ui`.
- Sidebar globale spostata in `src/routes/+layout.svelte` con switcher centro.
- Sidebar strutturata in componenti:
  - `src/lib/components/dashboard/app-sidebar.svelte`
  - `src/lib/components/dashboard/nav-main.svelte` (sottosezioni collassabili)
  - `src/lib/components/dashboard/center-switcher.svelte`
  - `src/lib/components/dashboard/nav-user.svelte`
- Home dashboard separata in componenti in `src/lib/components/dashboard/home/*`.
- Dati dashboard caricati con TanStack Query + placeholder in `src/lib/data/dashboard-home-placeholder.ts`.
- i18n attivo su sidebar/home tramite Paraglide (`m.*`) con switch lingua nel menu utente.

## 3) File chiave da leggere per partire

1. `src/routes/layout.css` (design tokens e theme).
2. `src/routes/+layout.svelte` (providers globali + AppSidebar).
3. `src/lib/components/dashboard/app-sidebar.svelte` (root sidebar).
4. `src/lib/components/dashboard/nav-main.svelte` (menu principale collassabile).
5. `src/lib/components/dashboard/center-switcher.svelte` (selettore centro in header sidebar).
6. `src/lib/components/dashboard/nav-user.svelte` (menu utente footer sidebar).
7. `src/routes/+page.svelte` (orchestratore Home).
8. `src/lib/components/dashboard/home/*` (sezioni Home modulari).
9. `src/lib/components/ui/*` (componenti base adattati a brand Betterfit).
10. `messages/en.json` e `messages/it.json` (traduzioni sorgente).
11. `src/lib/paraglide/messages.js` (output compilato, non modificare a mano).

## 4) Convenzioni operative

- Usare i token colore/layout definiti in `layout.css`, evitando valori hardcoded non necessari.
- Riutilizzare i componenti `ui/*` prima di crearne di nuovi.
- Per nuovi fetch usare `createQuery` con query key descrittive e placeholder/stub finche API non disponibili.
- Mantenere pagine orientate a task operativi (reception, manager, coach).

## 5) Dati placeholder (fase iniziale)

Finche backend/API non sono collegati:

- usare query function locali asincrone;
- simulare latenza breve;
- includere shape dati gia vicina al modello reale (KPI, task, alert, devices).

## 6) Comandi utili

```bash
pnpm dev
pnpm check
pnpm lint
pnpm format
```

## 7) MCP Svelte: come attivarlo

Nel progetto esiste gia la configurazione:

- `.vscode/mcp.json`

Contenuto:

- server `svelte` con comando `npx -y @sveltejs/mcp`

Per usarlo:

1. apri il progetto in VS Code con estensione MCP attiva;
2. verifica che i server MCP della workspace siano abilitati;
3. avvia/riavvia il server `svelte` dalla UI MCP dell'editor.

Se non parte automaticamente, prova da terminale:

```bash
npx -y @sveltejs/mcp
```

## 8) Checklist quando apri una nuova chat

1. Leggi questo file.
2. Leggi la brand guideline Betterfit.
3. Verifica i token in `layout.css` e i componenti base `ui/*`.
4. Esegui `pnpm check` prima e dopo modifiche rilevanti.
5. Se aggiungi copy utente, aggiorna anche Paraglide (`messages/en.json`, `messages/it.json`).
6. Dopo modifiche ai messaggi esegui `pnpm exec paraglide-js compile --project ./project.inlang --outdir ./src/lib/paraglide`.
