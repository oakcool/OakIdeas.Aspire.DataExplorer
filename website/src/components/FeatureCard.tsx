import type { Feature } from '../data/features'

interface FeatureCardProps {
  feature: Feature
}

export function FeatureCard({ feature }: FeatureCardProps) {
  return (
    <article
      style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: '24px',
        padding: '1.75rem',
      }}
    >
      <h3
        style={{
          margin: '0 0 0.75rem',
          fontSize: '1.1rem',
          fontWeight: 700,
          color: 'var(--text)',
        }}
      >
        {feature.title}
      </h3>
      <p style={{ color: 'var(--muted)', lineHeight: 1.7, margin: 0 }}>{feature.description}</p>
    </article>
  )
}
