export function Footer() {
  const year = new Date().getFullYear().toString()

  return (
    <footer
      style={{
        borderTop: '1px solid var(--border-strong)',
        padding: '3rem 0',
        marginTop: '4rem',
      }}
    >
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '2rem',
          alignItems: 'start',
        }}
      >
        <div>
          <strong style={{ color: 'var(--text)' }}>OakIdeas.Aspire.DataExplorer</strong>
          <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: '0.5rem 0 0' }}>
            Development-time database exploration for .NET Aspire.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          {[
            { href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer', label: 'GitHub' },
            {
              href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/releases',
              label: 'Releases',
            },
            {
              href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/tree/main/docs',
              label: 'Docs',
            },
            {
              href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/blob/main/LICENSE',
              label: 'License',
            },
          ].map(link => (
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
        </div>

        <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: 0, fontSize: '0.875rem' }}>
          Website source lives in <code>website/</code>. Published with GitHub Pages.{' '}
          {year && <span>{year}</span>}
        </p>
      </div>
    </footer>
  )
}
