interface SectionHeadingProps {
  label: string
  heading: string
}

export function SectionHeading({ label, heading }: SectionHeadingProps) {
  return (
    <>
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
        {label}
      </p>
      <h2
        style={{
          fontSize: 'clamp(1.8rem, 4vw, 2.8rem)',
          lineHeight: 1.1,
          margin: '0 0 2rem',
          color: 'var(--text)',
        }}
      >
        {heading}
      </h2>
    </>
  )
}
