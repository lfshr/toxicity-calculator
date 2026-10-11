import { test, expect } from '@playwright/test';
import { createServer } from 'node:http';
import { createHash } from 'node:crypto';
import { cp, mkdtemp, readFile, rm, stat, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { extname, join, normalize } from 'node:path';

// Deploys build A, then build B, to a server that caches like GitHub Pages (max-age=600), and checks that a normal
// reload shows B. The builds differ only in a <meta name="build"> tag, with the service worker's asset list updated
// to match, as the Pages workflow does for index.html.

const published = process.env.WEB_ROOT ?? '../../publish/web/wwwroot';
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json',
  '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml', '.webmanifest': 'application/manifest+json',
  '.dat': 'application/octet-stream',
};

test.use({ serviceWorkers: 'allow' });

let work;
let root;
let server;
let url;

test.beforeEach(async () => {
  work = await mkdtemp(join(tmpdir(), 'vet-update-'));
  server = createServer(async (req, res) => {
    const path = normalize(decodeURIComponent(new URL(req.url, 'http://localhost').pathname)).replace(/^(\.\.[/\\])+/, '');
    let file = join(root, path);
    try {
      if (!(await stat(file)).isFile()) throw new Error('not a file');
    } catch {
      file = join(root, 'index.html');
    }
    const body = await readFile(file);
    res.writeHead(200, { 'Content-Type': types[extname(file)] ?? 'application/octet-stream', 'Cache-Control': 'max-age=600' });
    res.end(body);
  });
  await new Promise(resolve => server.listen(0, resolve));
  url = `http://localhost:${server.address().port}/`;
});

test.afterEach(async () => {
  await new Promise(resolve => server.close(resolve));
  await rm(work, { recursive: true, force: true });
});

/** Copies the published app as build `label`, optionally with another service worker, and makes it the live site. */
async function deploy(label, serviceWorker) {
  const dir = join(work, label);
  await cp(published, dir, { recursive: true });
  await rm(join(dir, 'index.html.br'), { force: true });
  await rm(join(dir, 'index.html.gz'), { force: true });

  const index = (await readFile(join(dir, 'index.html'), 'utf8')).replace('<head>', `<head>\n    <meta name="build" content="${label}" />`);
  await writeFile(join(dir, 'index.html'), index);
  const hash = `sha256-${createHash('sha256').update(index).digest('base64')}`;
  const assetsPath = join(dir, 'service-worker-assets.js');
  const assets = (await readFile(assetsPath, 'utf8'))
    .replace(/("hash": ")[^"]*(",\s*"url": "index\.html")/, `$1${hash}$2`)
    .replace(/"version": "[^"]*"/, `"version": "${label}"`);
  await writeFile(assetsPath, assets);
  if (serviceWorker) {
    await writeFile(join(dir, 'service-worker.js'), await readFile(serviceWorker));
  }

  root = dir;
}

const build = page => page.locator('meta[name="build"]').getAttribute('content');

async function loadAndWaitForWorker(page) {
  await expect(page.locator('.calc-card').first()).toBeVisible({ timeout: 30_000 });
  await page.evaluate(() => navigator.serviceWorker.ready);
}

test('a new deploy loads on a normal reload', async ({ page }) => {
  await deploy('A');
  await page.goto(url);
  await loadAndWaitForWorker(page);
  await expect.poll(() => page.evaluate(() => !!navigator.serviceWorker.controller)).toBe(true);
  expect(await build(page)).toBe('A');

  await deploy('B');
  await page.reload();
  await loadAndWaitForWorker(page);

  expect(await build(page)).toBe('B');
  await expect(page.locator('.calc-card').first()).toBeVisible();
});

test('people stuck on the old worker are offered a reload that gets the new version', async ({ page }) => {
  await deploy('A', 'fixtures/old-service-worker.js');
  await page.goto(url);
  await loadAndWaitForWorker(page);
  // The old worker only controls pages loaded after it installed.
  await page.reload();
  await loadAndWaitForWorker(page);
  expect(await build(page)).toBe('A');

  await deploy('B');
  await page.reload();
  await loadAndWaitForWorker(page);

  // The old worker still serves A from its cache once, but the new worker takes over and offers a reload.
  const update = page.locator('#update-ui');
  await expect(update).toBeVisible({ timeout: 30_000 });
  await Promise.all([page.waitForEvent('load'), update.getByRole('button', { name: 'Reload' }).click()]);
  await loadAndWaitForWorker(page);

  expect(await build(page)).toBe('B');
  await expect(page.locator('#update-ui')).toBeHidden();
});
