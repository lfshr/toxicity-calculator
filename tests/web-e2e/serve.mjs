// Serves the published web app the way a static host would, sending index.html for page URLs (as GitHub Pages'
// 404.html fallback does) so deep links work. Usage: node serve.mjs <wwwroot> <port>
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';

const root = process.argv[2] ?? '../../publish/web/wwwroot';
const port = Number(process.argv[3] ?? 5180);
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css',
  '.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml',
  '.webmanifest': 'application/manifest+json', '.dat': 'application/octet-stream', '.dll': 'application/octet-stream',
  '.blat': 'application/octet-stream', '.pdb': 'application/octet-stream',
};

createServer(async (req, res) => {
  const path = normalize(decodeURIComponent(new URL(req.url, 'http://localhost').pathname)).replace(/^(\.\.[/\\])+/, '');
  let file = join(root, path);
  try {
    if (!(await stat(file)).isFile()) throw new Error('not a file');
  } catch {
    file = join(root, 'index.html');
  }
  try {
    const body = await readFile(file);
    res.writeHead(200, { 'Content-Type': types[extname(file)] ?? 'application/octet-stream' });
    res.end(body);
  } catch {
    res.writeHead(404);
    res.end();
  }
}).listen(port, () => console.log(`Serving ${root} on http://localhost:${port}`));
