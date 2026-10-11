// Offline support for the published app. See https://aka.ms/blazor-offline-considerations
//
// Every deploy changes service-worker-assets.js, so the browser installs a new copy of this worker. To make sure a
// new deploy reaches people on their next normal reload rather than after every tab is closed:
// - the new worker takes over at once (skipWaiting and clients.claim) and deletes the old version's cache;
// - the page and its unversioned files (index.html, app.css, icons) come from the network first, so a reload always
//   gets the current version when online, with the cache as the offline fallback;
// - only _framework files, whose names change with their content, are served from the cache first.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/, /\.svg$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// The worker sits at the app's base path, so this works both at the site root and on a subfolder (GitHub Pages).
const baseUrl = new URL('./', self.location);
const frameworkUrl = new URL('_framework/', baseUrl).href;
const indexUrl = new URL('index.html', baseUrl).href;
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));

    // Take over from the previous version straight away instead of waiting for every tab to close.
    await self.skipWaiting();
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));

    await self.clients.claim();
}

async function onFetch(event) {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(baseUrl.href)) {
        return fetch(request);
    }

    const cache = await caches.open(cacheName);

    // Fingerprinted framework files never change under the same name, so the cached copy is always right.
    if (request.url.startsWith(frameworkUrl)) {
        return (await cache.match(request)) || fetch(request);
    }

    // Navigations (such as /toxins/chocolate) all load index.html, unless they ask for an offline resource.
    const isAppPage = request.mode === 'navigate' && !manifestUrlList.some(url => url === request.url);
    try {
        // no-cache revalidates with the server, so a reload never gets an index.html older than the last deploy.
        // A navigation request can't be copied with new options, so it is fetched by URL.
        const response = await fetch(isAppPage ? indexUrl : request.mode === 'navigate' ? request.url : request, { cache: 'no-cache' });
        if (response.ok) {
            return response;
        }
    } catch {
        // Offline: fall through to the cache.
    }

    return (await cache.match(isAppPage ? 'index.html' : request)) || fetch(request);
}
