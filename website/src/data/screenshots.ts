export interface Screenshot {
  src: string
  alt: string
  caption: string
  wide?: boolean
}

export const screenshots: Screenshot[] = [
  {
    src: 'assets/screenshots/object-explorer.png',
    alt: 'Object Explorer showing databases, tables, views, and programmability folders',
    caption: 'Object Explorer keeps live schema navigation close to the rest of your Aspire workflow.',
    wide: true,
  },
  {
    src: 'assets/screenshots/query-results.png',
    alt: 'Query editor and results grid showing a sample SQL query',
    caption: 'Query Window supports guarded ad-hoc SQL with a focused results grid.',
  },
  {
    src: 'assets/screenshots/execution-plan.png',
    alt: 'Execution plan view rendered from a sample SQL Server plan',
    caption: 'Execution plan output helps explain query behavior without leaving the tool.',
  },
  {
    src: 'assets/screenshots/database-diagram.png',
    alt: 'Database diagram showing related tables and foreign key relationships',
    caption: 'Database Diagram highlights entity relationships and key columns.',
    wide: true,
  },
]
