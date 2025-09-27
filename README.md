# NugetWatch

- Uses Nuget service Web APIs to occasionally query package stats for specified "Owners".
- Data is saved to an IndexedDB so stats over time can be displayed to allow a better indication of interest.
- Includes a `downloads-per-day` view for each package similar to GitHub's [contributions in the last year](https://github.com/LostBeard#js-contribution-activity-description) table.

### Note
- All data is stored in the browser. The only servers used are official `nuget.org` public APIs.
- To actually track data over time, the web app must be kept running so changes can be saved in the browser.
- Currently only tracks my Nuget packages (will change soon.)

[NugetWatch](https://lostbeard.github.io/NugetWatch/)

