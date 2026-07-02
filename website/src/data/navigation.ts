export interface NavLink {
  label: string
  href: string
}

export const navLinks: NavLink[] = [
  { label: 'Features', href: '#features' },
  { label: 'Screenshots', href: '#screenshots' },
  { label: 'Getting started', href: '#getting-started' },
  { label: 'Documentation', href: '#docs' },
]

export interface DocLink {
  title: string
  description: string
  href: string
  linkText: string
}

export const docLinks: DocLink[] = [
  {
    title: 'Documentation',
    description:
      'Use the public docs as the source of truth for setup, architecture, samples, and troubleshooting.',
    href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/tree/main/docs',
    linkText: 'Open docs',
  },
  {
    title: 'Packages and releases',
    description:
      'Review package guidance, versioning expectations, and publishing workflows for preview and stable builds.',
    href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/tree/main/docs/nuget/package-readme.md',
    linkText: 'Package info',
  },
  {
    title: 'Contributing',
    description:
      'Read the contributor guide before proposing changes, especially for provider isolation and development-only boundaries.',
    href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/blob/main/CONTRIBUTING.md',
    linkText: 'Contribution guide',
  },
  {
    title: 'License',
    description: 'The project is released under the MIT License.',
    href: 'https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/blob/main/LICENSE',
    linkText: 'View license',
  },
]
