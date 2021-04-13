// src/sw.ts

// ESLint global registration
/* global serviceWorkerOption: false */

const cacheName = "kanban-cache";
const isExcluded = (f) => /hot-update|sockjs/.test(f);

const filesToCache = [
    //...serviceWorkerOption.assets.filter((file) => !isExcluded(file)),
    "/",
];

// Cache known assets up-front
const preCache = () =>
    caches.open(cacheName).then((cache) => {
        cache.addAll(filesToCache);
    });

// Handle the 'install' event
self.addEventListener("install", (event) => {
    event.waitUntil(preCache());
});

// Handle the 'install' event
self.addEventListener("fetch", (event) => {
    console.log(event, "Fetched!");
});
