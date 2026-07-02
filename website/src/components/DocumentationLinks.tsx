import { docLinks } from '../data/navigation'

export function DocumentationLinks() {
  return (
    <section id="docs" style={{ padding: '5rem 0' }}>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
          gap: '1.25rem',
        }}
      >
        {docLinks.map(link => (
          <article
            key={link.title}
            style={{
              background: 'var(--surface)',
              border: '1px solid var(--border)',
              borderRadius: '24px',
              padding: '1.75rem',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.75rem',
            }}
          >
            <h3
              style={{
                margin: 0,
                fontSize: '1.05rem',
                fontWeight: 700,
                color: 'var(--text)',
              }}
            >
              {link.title}
            </h3>
            <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: 0, flex: 1 }}>
              {link.description}
            </p>
            <a
              href={link.href}
              style={{
                color: 'var(--accent-hover)',
                textDecoration: 'none',
                fontWeight: 600,
                fontSize: '0.9rem',
              }}
              onMouseEnter={e => (e.currentTarget.style.textDecoration = 'underline')}
              onMouseLeave={e => (e.currentTarget.style.textDecoration = 'none')}
            >
              {link.linkText}
            </a>
          </article>
        ))}
      </div>
    </section>
  )
}
