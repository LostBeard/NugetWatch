# NugetWatch

- Blazor WASM web app that uses Nuget service Web APIs to occasionally query package stats for specified "Owners".
- Data is saved to an IndexedDB so stats over time can be displayed to allow a better indication of interest.
- Shows package downloads-per-day and version releases on an events-calendar similar to GitHub's [contributions in the last year](https://github.com/LostBeard#js-contribution-activity-description) table.
- Add Nuget package owners to track their packages. (Defaults to mine: LostBeard)

### Note
- All data is stored in the browser. The only servers used are official `nuget.org` public APIs.
- To actually track data (like downloads) over time, the web app must be kept running, or run often, so changes can be saved in the browser.

### Browser APIs demoed
- IndexedDB for package information storage
- Blazor WASM ServiceWorker used for offline mode (Cache API), and Periodic Background Sync when installed as a PWA to keep enable package stats over time.
- Uses Blazor's auto-generated `service-worker-assets.js` to only download app files that have changed during the service worker install event.

[NugetWatch](https://lostbeard.github.io/NugetWatch/)
