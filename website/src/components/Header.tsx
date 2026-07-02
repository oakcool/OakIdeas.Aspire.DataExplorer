import { useEffect, useState } from 'react'
import { navLinks } from '../data/navigation'

export function Header() {
  const [theme, setTheme] = useState<'dark' | 'light'>(() => {
    const stored = localStorage.getItem('oakideas-site-theme')
    if (stored === 'light' || stored === 'dark') return stored
    return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'
  })

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
    localStorage.setItem('oakideas-site-theme', theme)
  }, [theme])

  const toggleTheme = () => setTheme(t => (t === 'light' ? 'dark' : 'light'))

  return (
    <header
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 20,
        backdropFilter: 'blur(18px)',
        background: 'color-mix(in srgb, var(--bg) 82%, transparent)',
        borderBottom: '1px solid color-mix(in srgb, var(--border-strong) 80%, transparent)',
      }}
    >
      <a
        href="#content"
        style={{
          position: 'absolute',
          left: '-999px',
          top: 0,
        }}
        onFocus={e => (e.currentTarget.style.left = '1rem')}
        onBlur={e => (e.currentTarget.style.left = '-999px')}
      >
        Skip to content
      </a>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '1rem',
          minHeight: '4.5rem',
        }}
      >
        <a
          href="#top"
          aria-label="OakIdeas.Aspire.DataExplorer home"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.8rem',
            textDecoration: 'none',
            fontWeight: 700,
            color: 'var(--text)',
          }}
        >
          <img src="assets/logo.svg" alt="" width={40} height={40} />
          <span>OakIdeas.Aspire.DataExplorer</span>
        </a>

        <nav aria-label="Primary" style={{ display: 'flex', gap: '1.25rem', flexWrap: 'wrap' }}>
          {navLinks.map(link => (
            <a
              key={link.href}
              href={link.href}
              style={{ textDecoration: 'none', color: 'var(--muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--accent-hover)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--muted)')}
            >
              {link.label}
            </a>
          ))}
        </nav>

        <button
          type="button"
          onClick={toggleTheme}
          aria-label="Toggle light and dark theme"
          style={{
            border: '1px solid var(--border-strong)',
            background: 'var(--surface)',
            color: 'var(--text)',
            borderRadius: '999px',
            padding: '0.65rem 0.85rem',
            cursor: 'pointer',
          }}
        >
          ◐
        </button>
      </div>
    </header>
  )
}
