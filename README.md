# BetterFit Landing

Modern landing page for BetterFit, a CRM for fitness businesses. Built with SvelteKit 5, Tailwind CSS 4, and shadcn-svelte (bits-ui).

## Prerequisites

- **Node.js**: v20.20.0 or higher (v22.12+ recommended)
- **npm**: 10.x or higher

### Check Your Node Version

```sh
node --version
npm --version
```

### Update Node.js (Windows)

#### Option 1: Using nvm-windows (Recommended)

If you don't have nvm installed:

1. Download [nvm-windows](https://github.com/coreybutler/nvm-windows/releases)
2. Install the latest `.msi` file
3. Restart your terminal and run:

```powershell
nvm install 20.20.0
nvm use 20.20.0
nvm list  # Verify active version
```

#### Option 2: Direct Download

1. Visit https://nodejs.org/en/download
2. Select v20.20.0 (or v22.12+) and download the Windows Installer
3. Run the installer and follow the setup wizard
4. Restart your terminal and verify:

```powershell
node --version  # Should show v20.20.0+
npm --version   # Should show 10.x or higher
```

## Quick Start

```sh
npm install
npm run dev
```

Open http://localhost:5173/

## Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start dev server (http://localhost:5173) |
| `npm run build` | Build for production |
| `npm run preview` | Preview production build locally |
| `npm run check` | Type-check with Svelte |
| `npm run lint` | Run ESLint + Prettier checks |
| `npm run format` | Auto-format code with Prettier |

## Tech Stack

- **Framework**: SvelteKit 2 + Svelte 5.43.8
- **Styling**: Tailwind CSS 4 + shadcn-svelte (bits-ui)
- **i18n**: Paraglide.js (English & Italian)
- **Build**: Vite 7.3.1
- **Type Safety**: TypeScript 5.9.3

## Project Structure

```
src/
├── routes/
│   ├── (betterfit)/
│   │   ├── +layout.svelte     # Layout with header
│   │   └── +page.svelte       # Main landing page (hero, features, waitlist, footer)
│   ├── +layout.svelte         # Root layout
│   └── layout.css             # Design tokens & theme variables
├── lib/
│   ├── components/
│   │   ├── Header.svelte      # Navigation header + theme toggle
│   │   ├── LanguageSwitcher.svelte  # Language selector (EN/IT)
│   │   ├── AnimatedBusinessType.svelte  # Rotating business types
│   │   └── ui/                # shadcn-svelte components
│   └── paraglide/             # Generated i18n module
├── app.html                   # HTML entrypoint
└── app.d.ts                   # TypeScript declarations
```

## Key Features

### 1. **Theme Toggle (Light/Dark)**
- Located in the header (sun/moon icons)
- Preference stored in localStorage
- Applies `dark` class to the `html` element
- All colors maintain proper contrast in both modes

### 2. **Waitlist Form**
- Email, business name, and business type fields
- Form validation and error handling
- Loading state with spinner animation
- Success message with auto-disappear (5 seconds)
- Currently uses simulated submit (1.5s delay)

**To connect a real backend:**
Update the `handleFormSubmit` function in `src/routes/(betterfit)/+page.svelte` to call your API endpoint instead of the simulated delay.

### 3. **Internationalization (i18n)**
- Two languages: English (en) and Italian (it)
- Language selector in both desktop and mobile headers
- All copy strings managed in `messages/en.json` and `messages/it.json`
- Uses Paraglide.js for type-safe translations

**Adding new translations:**
1. Add key to both `messages/en.json` and `messages/it.json`
2. Use in components: `import { m } from '$lib/paraglide/messages'; {m.your_key()}`

### 4. **Responsive Design**
- Mobile-first approach with Tailwind CSS
- Fully functional on mobile, tablet, and desktop
- Mobile menu toggle in header for navigation

### 5. **SVG Dashboard Mockups**
- 4 inline SVG dashboard previews (no external images needed)
- Member Management, Class Scheduling, Analytics, Mobile App
- All created with inline SVG for fast loading

## Styling Guide

### CSS Variables (src/routes/layout.css)

All colors are defined as CSS custom properties in light and dark modes:

```css
:root {
  --primary: oklch(0.696 0.17 162.48);   /* Vibrant green */
  --background: oklch(0.99 0.002 162);   /* Off-white */
  --foreground: oklch(0.141 0.005 285.823); /* Dark grey */
  /* ... more variables ... */
}

.dark {
  --background: oklch(0.141 0.005 285.823); /* Dark grey */
  --foreground: oklch(0.985 0 0);           /* Pure white */
  /* ... more variables ... */
}
```

To add a new color:
1. Define it in both `:root` and `.dark` blocks
2. Use it in Tailwind: `text-[var(--your-color)]` or add to `@theme`

### Tailwind Configuration

Tailwind CSS 4 with Tailwind Vite plugin. All color tokens are automatically available as Tailwind utilities.

## Database & Backend

⚠️ **This repo is frontend-only.** No database or backend is currently configured.

**To-do for production:**
- [ ] Connect waitlist form to backend API
- [ ] Set up database (PostgreSQL recommended) for storing waitlist signups
- [ ] Implement authentication for admin dashboard
- [ ] Add contact form email notifications

## Deployment

### Build for Production

```sh
npm run build
npm run preview  # Test production build locally
```

### Hosting Options

- **Vercel** (Recommended for SvelteKit): Fork repo → Connect to Vercel → Auto-deploy on push
- **Netlify**: Similar setup, supports SvelteKit adapter
- **Self-hosted**: Deploy to any Node.js server (configure adapter in `svelte.config.js`)

## Common Issues

### Issue: "Module not found" errors
**Solution:** Run `npm install` again and clear `.svelte-kit` folder

### Issue: Port 5173 already in use
**Solution:** 
```sh
npm run dev -- --port 3000  # Use different port
```

### Issue: @html not working
**Solution:** Make sure your string is properly escaped and uses the Paraglide API correctly

### Issue: Theme not persisting
**Solution:** Check that localStorage is enabled and not blocked by browser settings

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make changes and commit: `git commit -m "feat: add feature"`
3. Format code: `npm run format`
4. Run checks: `npm run lint`
5. Push and open a Pull Request

## Notes

- All SVG mockups are created inline (no image files to download)
- The form submit is mocked; connect to a real API before launch
- Keep translations in sync (en.json and it.json)
- Use `onclick` for event handlers in Svelte 5 (not `on:click`)

