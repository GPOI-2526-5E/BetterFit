# Contributing to BetterFit

Thank you for your interest in contributing! This guide will help you get started.

## Development Setup

1. **Fork and clone the repository**

```bash
git clone https://github.com/your-username/gymbro.git
cd gymbro
```

2. **Install dependencies**

```bash
npm install
```

3. **Create a feature branch**

```bash
git checkout -b feature/your-feature-name
```

## Development Workflow

### Start Development Server

```bash
npm run dev
```

The app will be available at http://localhost:5173/

### Run Type Checking

```bash
npm run check
```

This runs Svelte type checking and validation.

### Format Code

```bash
npm run format
```

This uses Prettier to auto-format your code.

### Run Linting

```bash
npm run lint
```

This checks for ESLint and Prettier issues.

## Making Changes

### Adding a New Feature

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes
3. Test locally: `npm run dev`
4. Format: `npm run format`
5. Check for errors: `npm run check`
6. Lint: `npm run lint`
7. Commit with a clear message: `git commit -m "feat: add my feature"`
8. Push to your fork: `git push origin feature/my-feature`
9. Open a Pull Request with a clear description

### Adding Translations

1. Add the English key in `messages/en.json`
2. Add the Italian translation in `messages/it.json`
3. Keep keys in alphabetical order within sections
4. Use the message in a Svelte component:

```svelte
<script>
  import { m } from '$lib/paraglide/messages';
</script>

<h1>{m.your_new_key()}</h1>
```

### Modifying Styles

- Use Tailwind CSS classes whenever possible
- For custom colors, update CSS variables in `src/routes/layout.css`
- Ensure colors work in both light and dark themes
- Test both themes before committing

### Adding Components

1. Create in `src/lib/components/`
2. Use `<script lang="ts">` for type safety
3. Export props and events clearly
4. Follow shadcn-svelte patterns for UI components
5. Example:

```svelte
<script lang="ts">
  interface Props {
    title: string;
    disabled?: boolean;
    onclick?: () => void;
  }

  let { title, disabled = false, onclick } = $props();
</script>

<button {onclick} {disabled}>
  {title}
</button>
```

## Commit Message Convention

Follow Conventional Commits for clear history:

- `feat: add new feature`
- `fix: resolve bug`
- `docs: update documentation`
- `style: format code (no logic change)`
- `refactor: restructure without changing behavior`
- `test: add or update tests`
- `chore: dependency updates, config changes`

Example:
```bash
git commit -m "feat: add dark mode toggle to header"
git commit -m "fix: correct form validation error message"
git commit -m "docs: update README with new API docs"
```

## Testing Your Changes

### Local Testing

1. Test in both light and dark themes
2. Test on mobile viewport (DevTools)
3. Test all languages (EN and IT)
4. Test form submission feedback

### Before Committing

```bash
npm run check    # Type checking
npm run lint     # Linting
npm run format   # Auto-format
npm run build    # Verify production build works
```

## Pull Request Process

1. **Update your branch** with latest main:

```bash
git fetch origin
git rebase origin/main
```

2. **Resolve any conflicts** and test again

3. **Keep commits clean**: Squash related commits if needed

```bash
git rebase -i origin/main
# Mark commits as 'squash' to combine them
```

4. **Push to your fork**:

```bash
git push origin feature/your-feature --force-with-lease
```

5. **Open Pull Request** on GitHub with:
   - Clear title describing the change
   - Description of what was changed and why
   - Screenshots if UI changes
   - Link to related issues (if any)

## Code Style Guide

### Svelte Components

- Use `<script lang="ts">` for type safety
- Use `onclick` (not `on:click`) in Svelte 5
- Import types: `import type { Props } from './types'`
- Use reactive declarations with `$state`, `$derived`, `$effect`

### CSS/Tailwind

- Use utility classes for responsive design
- Mobile-first approach: base mobile styles, then `md:`, `lg:`, `xl:` breakpoints
- Extract repeated patterns into components
- Use CSS variables for theme consistency

Example:
```svelte
<div class="px-4 md:px-6 lg:px-8 py-12 md:py-16">
  <!-- Content -->
</div>
```

### TypeScript

- Define interfaces for component props
- Use `type` for unions, `interface` for objects
- Keep types in a `types.ts` file for larger features
- Example:

```typescript
interface FormData {
  email: string;
  businessName?: string;
  businessType: 'gym' | 'yoga' | 'other';
}
```

## File Structure

When adding new features, follow this structure:

```
src/
├── lib/
│   ├── components/
│   │   └── MyFeature.svelte          # Component
│   └── types/
│       └── my-feature.types.ts       # Types
├── routes/
│   ├── my-feature/
│   │   ├── +page.svelte              # Page component
│   │   ├── +page.server.ts           # Server logic (if needed)
│   │   └── types.ts                  # Page-specific types
```

## Documentation

- Update `README.md` if adding major features
- Add comments for complex logic
- Document environment variables in `.env.example`
- Keep inline comments minimal (code should be self-documenting)

## Getting Help

- Check existing issues: https://github.com/project/gymbro/issues
- Read the main [README.md](./README.md)
- Review the [Tech Stack](./README.md#tech-stack)
- Ask in Pull Request comments if stuck

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

---

Happy coding! 🚀
