import { test, expect } from '@playwright/test';

test('Verify CSS Changes - Navigation Visible and Links Clean', async ({ page }) => {
  // Navigate to the app
  await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });
  
  // Take screenshot of the full page
  await page.screenshot({ path: 'screenshots/01-full-page.png', fullPage: true });
  
  // Check if navigation exists and is visible
  const nav = page.locator('nav, [role="navigation"], .navbar');
  const isNavVisible = await nav.isVisible();
  console.log('Navigation visible:', isNavVisible);
  
  // Get navigation styles
  const navBox = await nav.boundingBox();
  console.log('Navigation bounding box:', navBox);
  
  // Check background color
  const navBgColor = await nav.evaluate(el => window.getComputedStyle(el).backgroundColor);
  console.log('Navigation background:', navBgColor);
  
  // Take screenshot of nav area
  if (navBox) {
    await page.screenshot({ 
      path: 'screenshots/02-nav-area.png',
      clip: { x: navBox.x, y: navBox.y, width: navBox.width, height: navBox.height + 100 }
    });
  }
  
  // Check for links and their styling
  const links = page.locator('a');
  const linkCount = await links.count();
  console.log('Number of links found:', linkCount);
  
  // Check a link's text-decoration
  if (linkCount > 0) {
    const firstLink = links.first();
    const decoration = await firstLink.evaluate(el => window.getComputedStyle(el).textDecoration);
    console.log('First link text-decoration:', decoration);
    
    await firstLink.screenshot({ path: 'screenshots/03-first-link.png' });
  }
  
  // Check main content area
  const main = page.locator('main, [role="main"], .main-content');
  const mainVisible = await main.isVisible();
  console.log('Main content visible:', mainVisible);
  
  const mainBox = await main.boundingBox();
  console.log('Main content bounding box:', mainBox);
  
  // Check for white space issues - get page height
  const pageHeight = await page.evaluate(() => document.documentElement.scrollHeight);
  const viewportHeight = await page.evaluate(() => window.innerHeight);
  console.log('Page height:', pageHeight, 'Viewport height:', viewportHeight);
  
  // Take full page screenshot showing any white space
  await page.screenshot({ path: 'screenshots/04-full-content.png', fullPage: true });
  
  // Assertions
  expect(isNavVisible).toBe(true);
  expect(linkCount).toBeGreaterThan(0);
  if (linkCount > 0) {
    expect(decoration).not.toContain('underline');
  }
});

test('Verify Dark Theme Applied', async ({ page }) => {
  await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });
  
  // Check body background color
  const bodyBg = await page.evaluate(() => window.getComputedStyle(document.body).backgroundColor);
  console.log('Body background:', bodyBg);
  
  // Check body text color
  const bodyColor = await page.evaluate(() => window.getComputedStyle(document.body).color);
  console.log('Body text color:', bodyColor);
  
  // Screenshot the theme
  await page.screenshot({ path: 'screenshots/05-theme-check.png', fullPage: true });
});
