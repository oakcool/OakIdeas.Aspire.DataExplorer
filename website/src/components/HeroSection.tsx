export function HeroSection() {
  return (
    <section id="top" style={{ padding: '2rem 0 4rem' }}>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'grid',
          gridTemplateColumns: '1.1fr 0.9fr',
          gap: '2rem',
          alignItems: 'center',
          minHeight: 'calc(100vh - 4.5rem)',
        }}
        className="hero-grid"
      >
        <div>
          <p
            style={{
              display: 'inline-flex',
              padding: '0.45rem 0.8rem',
              borderRadius: '999px',
              background: 'var(--accent-soft)',
              border: '1px solid var(--border)',
              color: 'var(--accent-hover)',
              fontSize: '0.82rem',
              fontWeight: 700,
              letterSpacing: '0.06em',
              textTransform: 'uppercase',
              margin: '0 0 1rem',
            }}
          >
            Development-time only • SQL Server-first MVP
          </p>
          <h1
            style={{
              fontSize: 'clamp(2.8rem, 6vw, 4.7rem)',
              lineHeight: 1.03,
              margin: '1rem 0',
              color: 'var(--text)',
            }}
          >
            Explore Aspire-hosted databases without leaving your local workflow.
          </h1>
          <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: '0 0 1.5rem' }}>
            OakIdeas.Aspire.DataExplorer adds a focused, polished database workspace to .NET
            Aspire: discover local resources, inspect schema metadata, open object details, and run
            guarded SQL queries with diagnostics and execution-plan support.
          </p>
          <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.75rem' }}>
            <a
              href="https://github.com/oakcool/OakIdeas.Aspire.DataExplorer"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                textDecoration: 'none',
                minHeight: '3rem',
                padding: '0 1.15rem',
                borderRadius: '999px',
                fontWeight: 700,
                background: 'linear-gradient(135deg, var(--accent), var(--accent-hover))',
                color: 'white',
                border: '1px solid transparent',
                boxShadow: '0 24px 60px rgba(1, 4, 9, 0.42)',
              }}
            >
              View on GitHub
            </a>
            <a
              href="#getting-started"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                textDecoration: 'none',
                minHeight: '3rem',
                padding: '0 1.15rem',
                borderRadius: '999px',
                fontWeight: 700,
                background: 'var(--surface)',
                color: 'var(--text)',
                border: '1px solid var(--border-strong)',
              }}
            >
              Get started
            </a>
          </div>
          <ul
            style={{
              listStyle: 'none',
              padding: 0,
              margin: 0,
              display: 'flex',
              gap: '0.75rem',
              flexWrap: 'wrap',
            }}
          >
            {[
              'Aspire resource discovery for local development',
              'Object Explorer, object details, query results, diagrams, and execution plans',
              'Development-only guardrails and sanitized diagnostics',
            ].map(point => (
              <li
                key={point}
                style={{
                  padding: '0.7rem 1rem',
                  border: '1px solid var(--border)',
                  borderRadius: '999px',
                  background: 'var(--surface)',
                  color: 'var(--muted)',
                  lineHeight: 1.7,
                  fontSize: '0.9rem',
                }}
              >
                {point}
              </li>
            ))}
          </ul>
        </div>

        <div
          style={{
            background: 'var(--surface)',
            border: '1px solid var(--border)',
            borderRadius: '24px',
            overflow: 'hidden',
          }}
        >
          <img
            src="assets/screenshots/dashboard.png"
            alt="Data Explorer dashboard and object explorer shell"
            loading="eager"
            style={{ width: '100%', height: 'auto' }}
          />
        </div>
      </div>

      <style>{`
        @media (max-width: 768px) {
          .hero-grid {
            grid-template-columns: 1fr !important;
          }
        }
      `}</style>
    </section>
  )
}
