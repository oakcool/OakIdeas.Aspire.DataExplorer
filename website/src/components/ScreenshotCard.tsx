import type { Screenshot } from '../data/screenshots'

interface ScreenshotCardProps {
  screenshot: Screenshot
}

export function ScreenshotCard({ screenshot }: ScreenshotCardProps) {
  return (
    <figure
      style={{
        margin: 0,
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: '24px',
        overflow: 'hidden',
        gridColumn: screenshot.wide ? 'span 2' : undefined,
      }}
      className={screenshot.wide ? 'shot-wide' : undefined}
    >
      <img
        src={screenshot.src}
        alt={screenshot.alt}
        loading="lazy"
        style={{ width: '100%', height: 'auto' }}
      />
      <figcaption
        style={{
          padding: '1rem 1.5rem',
          color: 'var(--muted)',
          lineHeight: 1.7,
          fontSize: '0.9rem',
        }}
      >
        {screenshot.caption}
      </figcaption>
    </figure>
  )
}
