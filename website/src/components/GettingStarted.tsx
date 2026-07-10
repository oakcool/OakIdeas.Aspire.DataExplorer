import { SectionHeading } from './SectionHeading'

export function GettingStarted() {
  return (
    <section id="getting-started" style={{ padding: '5rem 0', background: 'var(--bg-alt)' }}>
      <div
        style={{
          width: 'min(calc(100% - 2rem), 1180px)',
          margin: '0 auto',
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: '3rem',
          alignItems: 'start',
        }}
        className="getting-started-grid"
      >
        <div>
          <SectionHeading
            label="Getting started"
            heading="Install, validate, and run the solution locally."
          />
          <ol
            style={{
              paddingLeft: '1.25rem',
              margin: 0,
              display: 'flex',
              flexDirection: 'column',
              gap: '0.75rem',
            }}
          >
            {[
              'Install .NET SDK 10+, Node.js 20+, Docker Desktop (or equivalent), and the Aspire workload.',
              'Restore, build, and test the solution.',
              'Run the AppHost and open Data Explorer from the Aspire dashboard.',
              'Switch between the sample `sampledb` and `warehousedb` resources from the database picker.',
            ].map(step => (
              <li key={step} style={{ color: 'var(--muted)', lineHeight: 1.7 }}>
                {step}
              </li>
            ))}
          </ol>
        </div>

        <div
          style={{
            background: 'var(--surface)',
            border: '1px solid var(--border)',
            borderRadius: '24px',
            padding: '1.75rem',
          }}
        >
          <pre
            style={{
              margin: 0,
              overflowX: 'auto',
              color: 'var(--text)',
              fontSize: '0.875rem',
              lineHeight: 1.7,
            }}
          >
            <code>{`dotnet restore

dotnet build OakIdeas.Aspire.DataExplorer.sln

dotnet test OakIdeas.Aspire.DataExplorer.sln

dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost`}</code>
          </pre>
          <p
            style={{ color: 'var(--muted)', lineHeight: 1.7, margin: '1rem 0 0', fontSize: '0.875rem' }}
          >
            You can also run <code>samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost</code> to
            validate a consumer-style setup with multiple database resources.
          </p>
        </div>
      </div>
      <style>{`
        @media (max-width: 768px) {
          .getting-started-grid {
            grid-template-columns: 1fr !important;
          }
        }
      `}</style>
    </section>
  )
}
