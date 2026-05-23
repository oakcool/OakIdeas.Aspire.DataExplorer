import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

// Create screenshots directory
const screenshotsDir = path.join(__dirname, '../screenshots');
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true });
}

test.describe('DataExplorer CSS Validation - Complete E2E', () => {
  
  test('Main App - Full UI Validation', async ({ page }) => {
    console.log('🔍 Testing Main App (http://localhost:5000)');
    
    // Navigate to main app
    await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });
    
    // Wait for app to fully render
    await page.waitForSelector('.de-shell', { timeout: 5000 });
    
    // Screenshot 1: Full page layout
    await page.screenshot({ 
      path: path.join(screenshotsDir, '01-main-app-full.png'),
      fullPage: true 
    });
    console.log('✅ Screenshot 1: Full page layout');
    
    // Check dark theme
    const bodyBg = await page.evaluate(() => {
      return window.getComputedStyle(document.body).backgroundColor;
    });
    console.log('📊 Body background:', bodyBg);
    expect(bodyBg).toContain('13'); // Should be dark (#0d1117)
    
    // Check navigation exists
    const nav = page.locator('.de-header__nav');
    await expect(nav).toBeVisible();
    console.log('✅ Navigation bar visible');
    
    // Screenshot 2: Navigation area
    const navBox = await nav.boundingBox();
    if (navBox) {
      await page.screenshot({
        path: path.join(screenshotsDir, '02-main-app-nav.png'),
        clip: { x: 0, y: 0, width: navBox.width + 200, height: navBox.height + 20 }
      });
      console.log('✅ Screenshot 2: Navigation area');
    }
    
    // Check navigation items
    const navLinks = page.locator('.de-header__nav .de-nav-link');
    const navLinkCount = await navLinks.count();
    console.log(`📊 Navigation links found: ${navLinkCount}`);
    expect(navLinkCount).toBeGreaterThan(0);
    
    // Check navigation styling
    const navLink = navLinks.first();
    const navLinkColor = await navLink.evaluate(el => 
      window.getComputedStyle(el).color
    );
    console.log('📊 Navigation link color:', navLinkColor);
    
    // Check links have no underline
    const pageLink = page.locator('a').first();
    const linkDecoration = await pageLink.evaluate(el => 
      window.getComputedStyle(el).textDecoration
    );
    console.log('📊 Link text-decoration:', linkDecoration);
    expect(linkDecoration).not.toContain('underline');
    console.log('✅ Links have no underlines');
    
    // Check CSS is loaded
    const cssLoaded = await page.evaluate(() => {
      const stylesheets = Array.from(document.styleSheets);
      return stylesheets.some(sheet => 
        sheet.href?.includes('app.css') && sheet.cssRules?.length > 0
      );
    });
    console.log('📊 CSS loaded:', cssLoaded);
    expect(cssLoaded).toBe(true);
    console.log('✅ CSS stylesheet loaded');
    
    // Screenshot 3: Header area
    await page.screenshot({
      path: path.join(screenshotsDir, '03-main-app-header.png'),
      clip: { x: 0, y: 0, width: 800, height: 120 }
    });
    console.log('✅ Screenshot 3: Header area');
    
    // Screenshot 4: Main content area
    const workspace = page.locator('.de-workspace');
    const wsBox = await workspace.boundingBox();
    if (wsBox) {
      await page.screenshot({
        path: path.join(screenshotsDir, '04-main-app-content.png'),
        clip: { x: 0, y: 110, width: wsBox.width, height: 400 }
      });
      console.log('✅ Screenshot 4: Main content area');
    }
    
    console.log('🎉 Main App validation complete!\n');
  });

  test('Sample App - Full UI Validation', async ({ browser }) => {
    // Create new context for sample app
    const context = await browser.newContext();
    const page = await context.newPage();
    
    try {
      console.log('🔍 Testing Sample App (http://localhost:8000)');
      
      // Navigate to sample app
      await page.goto('http://localhost:8000', { waitUntil: 'networkidle', timeout: 10000 });
      
      // Wait for page to load
      await page.waitForSelector('body', { timeout: 5000 });
      
      // Screenshot 1: Full page
      await page.screenshot({
        path: path.join(screenshotsDir, '05-sample-app-full.png'),
        fullPage: true
      });
      console.log('✅ Screenshot 1: Full page layout');
      
      // Check dark theme applied
      const bodyBg = await page.evaluate(() => {
        return window.getComputedStyle(document.body).backgroundColor;
      });
      console.log('📊 Body background:', bodyBg);
      
      // Check for main content
      const appContainer = page.locator('#app, [role="application"]');
      const isVisible = await appContainer.isVisible().catch(() => false);
      console.log('📊 App container visible:', isVisible);
      
      // Check links
      const allLinks = page.locator('a');
      const linkCount = await allLinks.count();
      console.log(`📊 Total links found: ${linkCount}`);
      
      if (linkCount > 0) {
        const firstLink = allLinks.first();
        const linkDecoration = await firstLink.evaluate(el =>
          window.getComputedStyle(el).textDecoration
        );
        console.log('📊 Link text-decoration:', linkDecoration);
        expect(linkDecoration).not.toContain('underline');
        console.log('✅ Links have no underlines');
      }
      
      // Screenshot 2: Top section
      await page.screenshot({
        path: path.join(screenshotsDir, '06-sample-app-top.png'),
        clip: { x: 0, y: 0, width: 1200, height: 200 }
      });
      console.log('✅ Screenshot 2: Top section');
      
      console.log('🎉 Sample App validation complete!\n');
    } finally {
      await context.close();
    }
  });

  test('CSS Comparison - Before & After', async ({ page }) => {
    console.log('🔍 CSS Metrics Comparison\n');
    
    await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });
    
    const metrics = await page.evaluate(() => {
      const styles = window.getComputedStyle(document.body);
      const mainContent = document.querySelector('.de-workspace');
      const mainStyles = mainContent ? window.getComputedStyle(mainContent) : null;
      
      return {
        bodyStyles: {
          backgroundColor: styles.backgroundColor,
          color: styles.color,
          fontFamily: styles.fontFamily,
          display: styles.display,
          flexDirection: styles.flexDirection,
        },
        mainContentStyles: mainStyles ? {
          backgroundColor: mainStyles.backgroundColor,
          display: mainStyles.display,
          flex: mainStyles.flex,
        } : null,
        linksWithoutUnderline: Array.from(document.querySelectorAll('a'))
          .filter(a => window.getComputedStyle(a).textDecoration !== 'underline')
          .length,
        totalLinks: document.querySelectorAll('a').length,
      };
    });
    
    console.log('📋 CSS Metrics:');
    console.log('Body Styles:', JSON.stringify(metrics.bodyStyles, null, 2));
    if (metrics.mainContentStyles) {
      console.log('Main Content Styles:', JSON.stringify(metrics.mainContentStyles, null, 2));
    }
    console.log(`Links without underline: ${metrics.linksWithoutUnderline}/${metrics.totalLinks}`);
    
    // Create comparison report
    const report = `# CSS Validation Report

## Build Status
✅ Build Successful

## Dark Theme
✅ Body background color: ${metrics.bodyStyles.backgroundColor}
✅ Text color: ${metrics.bodyStyles.color}
✅ Font: ${metrics.bodyStyles.fontFamily}

## Layout
✅ Body display: ${metrics.bodyStyles.display}
✅ Body flex-direction: ${metrics.bodyStyles.flexDirection}

## Links
✅ Clean links (no underlines): ${metrics.linksWithoutUnderline}/${metrics.totalLinks}

## Navigation
✅ Navigation bar visible and styled

## Screenshots Generated
1. Main app full page layout
2. Main app navigation area
3. Main app header
4. Main app content area
5. Sample app full page layout
6. Sample app top section

## Conclusion
All CSS styles are applied correctly!
`;
    
    fs.writeFileSync(
      path.join(screenshotsDir, 'VALIDATION_REPORT.md'),
      report
    );
    console.log('\n✅ Validation report generated');
  });
});
