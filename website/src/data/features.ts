export interface Feature {
  title: string
  description: string
}

export const features: Feature[] = [
  {
    title: 'Multi-database switching',
    description:
      'Reference multiple Aspire database resources from one Data Explorer instance, then switch between isolated metadata, query, and diagram contexts in the UI.',
  },
  {
    title: 'Object Explorer',
    description:
      'Browse schemas, tables, views, procedures, functions, triggers, indexes, and definitions through a compact tree built for database-first debugging.',
  },
  {
    title: 'Guarded Query Window',
    description:
      'Run ad-hoc SQL with row limits, timeout controls, optional read-only mode, destructive-query confirmation, and user-visible recovery guidance.',
  },
  {
    title: 'Execution plans and diagrams',
    description:
      'Visualize execution plans when supported and inspect entity relationships through an interactive database diagram surface.',
  },
  {
    title: 'Provider-based architecture',
    description:
      'Shared layers define contracts and orchestration, while providers own SQL, capability-specific discovery, and exception mapping.',
  },
]
