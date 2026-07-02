import { screenshots } from '../data/screenshots'
import { ScreenshotCard } from './ScreenshotCard'
import { SectionHeading } from './SectionHeading'

export function ScreenshotGallery() {
  return (
    <section id="screenshots" style={{ padding: '5rem 0' }}>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
        }}
      >
        <SectionHeading
          label="Screenshots"
          heading="See the workspace before you wire it into your own app."
        />
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(2, 1fr)',
            gap: '1.25rem',
          }}
          className="screenshot-grid"
        >
          {screenshots.map(shot => (
            <ScreenshotCard key={shot.src} screenshot={shot} />
          ))}
        </div>
      </div>
      <style>{`
        @media (max-width: 640px) {
          .screenshot-grid {
            grid-template-columns: 1fr !important;
          }
          .shot-wide {
            grid-column: span 1 !important;
          }
        }
      `}</style>
    </section>
  )
}
