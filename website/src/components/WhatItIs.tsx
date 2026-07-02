export function WhatItIs() {
  return (
    <section style={{ padding: '5rem 0' }}>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: '3rem',
          alignItems: 'center',
        }}
        className="what-it-is-grid"
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
              textTransform: 'uppercase' as const,
              margin: '0 0 1rem',
            }}
          >
            What it is
          </p>
          <h2
            style={{
              fontSize: 'clamp(1.8rem, 4vw, 2.8rem)',
              lineHeight: 1.1,
              margin: 0,
              color: 'var(--text)',
            }}
          >
            A database explorer tailored for Aspire-backed local environments.
          </h2>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: 0 }}>
            Data Explorer keeps schema exploration and SQL troubleshooting inside the same
            development loop as the rest of your Aspire app. Instead of reconstructing connection
            details in external tools, contributors can inspect live local databases directly from a
            purpose-built UI.
          </p>
          <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: 0 }}>
            The project is intentionally development-only. Runtime and hosting guards help keep the
            tool out of production scenarios, while provider-specific SQL and error mapping stay
            isolated in provider projects.
          </p>
        </div>
      </div>
      <style>{`
        @media (max-width: 768px) {
          .what-it-is-grid {
            grid-template-columns: 1fr !important;
          }
        }
      `}</style>
    </section>
  )
}
