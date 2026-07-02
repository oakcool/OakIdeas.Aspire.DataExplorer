import { HeroSection } from '../components/HeroSection'
import { WhatItIs } from '../components/WhatItIs'
import { FeatureGrid } from '../components/FeatureGrid'
import { ScreenshotGallery } from '../components/ScreenshotGallery'
import { GettingStarted } from '../components/GettingStarted'
import { DocumentationLinks } from '../components/DocumentationLinks'

export function HomePage() {
  return (
    <>
      <HeroSection />
      <WhatItIs />
      <FeatureGrid />
      <ScreenshotGallery />
      <GettingStarted />
      <DocumentationLinks />
    </>
  )
}
