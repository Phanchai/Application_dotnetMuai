// Captures the README screenshots from a running copy of the web demo.
// Usage: node screenshots.mjs http://localhost:5080 ../docs/screenshots
import { chromium } from 'playwright';

const [base = 'http://localhost:5080', out = '../docs/screenshots'] = process.argv.slice(2);
const browser = await chromium.launch();

async function capture(prefix, viewport, mobile) {
  const context = await browser.newContext({ viewport, deviceScaleFactor: 2, isMobile: mobile, hasTouch: mobile });
  const page = await context.newPage();
  page.on('console', m => m.type() === 'error' && console.log('console error:', m.text()));
  page.on('pageerror', e => console.log('page error:', e.message));
  const shot = name => page.screenshot({ path: `${out}/${prefix}-${name}.png` });
  const go = async href => {
    await page.locator(`nav a[href="${href}"]:visible`).first().click();
    await page.waitForTimeout(600);
  };

  await page.goto(base);
  await page.waitForSelector('.demo-manager', { timeout: 60000 });
  await shot('login');
  await page.click('.demo-manager');
  await page.waitForSelector('h1');
  await page.waitForTimeout(800);
  await shot('dashboard');

  await go('jobs');
  await shot('jobs');
  await page.locator('tr.job-row a').first().click();
  await page.waitForTimeout(600);
  await shot('job-detail');
  await go('timeline');
  await shot('timeline');
  await go('teams');
  await shot('teams');
  await go('master');
  await shot('master');
  await context.close();
}

await capture('desktop', { width: 1280, height: 800 }, false);
await capture('mobile', { width: 390, height: 844 }, true);
await browser.close();
console.log('done');
