import { features } from '../data/features'
import { FeatureCard } from './FeatureCard'
import { SectionHeading } from './SectionHeading'

export function FeatureGrid() {
  return (
    <section
      id="features"
      style={{
        padding: '5rem 0',
        background: 'var(--bg-alt)',
      }}
    >
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
        }}
      >
        <SectionHeading
          label="Why it exists"
          heading="Fast local investigation without weakening your boundaries."
        />
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
            gap: '1.25rem',
          }}
        >
          {features.map(feature => (
            <FeatureCard key={feature.title} feature={feature} />
          ))}
        </div>
      </div>
    </section>
  )
}
