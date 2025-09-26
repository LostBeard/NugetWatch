async function getNugetPackagesByAuthor(authorName) {
    const nugetSearchUrl = "https://api-v2v3search-0.nuget.org/query"; // The NuGet Search Service URL
    const pageSize = 20; // Number of results per page, adjust as needed

    let allPackages = [];
    let skip = 0;
    let hasMoreResults = true;

    while (hasMoreResults) {
        const url = `${nugetSearchUrl}?q=&skip=${skip}&take=${pageSize}`; // Search for all packages, then filter

        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();

            if (data && data.data && data.data.length > 0) {
                const filteredPackages = data.data.filter(pkg =>
                    pkg.authors && pkg.authors.some(author =>
                        author.toLowerCase().includes(authorName.toLowerCase())
                    )
                );
                allPackages = allPackages.concat(filteredPackages);
                skip += pageSize;
            } else {
                hasMoreResults = false; // No more packages to fetch
            }
        } catch (error) {
            console.error("Error fetching NuGet packages:", error);
            hasMoreResults = false;
        }
    }

    return allPackages;
}

await getNugetPackagesByAuthor("LostBeard");

// Function to get NuGet package stats
async function getNugetPackageStats(packageName) {
    try {
        const url = `https://api.nuget.org/v3-flatcontainer/${packageName.toLowerCase()}/index.json`;
        const response = await fetch(url);

        // Throw an error for non-2xx HTTP responses
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const data = await response.json();

        // The API returns a list of versions. To get total downloads,
        // you typically need to aggregate data or use a different endpoint.
        // However, the v3-flatcontainer index gives us a list of available versions.
        // For more detailed stats like total downloads, you would need to query
        // the package metadata resource. For this example, let's show the versions.

        console.log(`Package: ${packageName}`);
        console.log(`Available versions: ${data.versions.join(', ')}`);

        // To get more comprehensive stats (including download count)
        // you would use a more complex API call.
        return data.versions;

    } catch (error) {
        console.error("Failed to fetch NuGet package stats:", error);
    }
}

// Example usage
await getNugetPackageStats('Newtonsoft.Json');

// You can also expand this function to get stats for a specific version.
async function getNugetPackageVersionStats(packageName, packageVersion) {
    try {
        const url = `https://api.nuget.org/v3-flatcontainer/${packageName.toLowerCase()}/${packageVersion.toLowerCase()}/${packageName.toLowerCase()}.nuspec`;
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const data = await response.text(); // The .nuspec is XML
        console.log(`Package Manifest (.nuspec) for ${packageName} version ${packageVersion}:`);
        console.log(data);

    } catch (error) {
        console.error("Failed to fetch NuGet package stats:", error);
    }
}

// Example usage for a specific version
getNugetPackageVersionStats('Newtonsoft.Json', '13.0.1');


//async function getNugetPackageTotalDownloads(packageName) {
//    try {
//        const url = `https://api.nuget.org/v3/registration-semver1/${packageName.toLowerCase()}/index.json`;
//        const response = await fetch(url);

//        if (!response.ok) {
//            throw new Error(`HTTP error! Status: ${response.status}`);
//        }

//        const data = await response.json();
//        const latestPage = data.items[data.items.length - 1];
//        const latestVersionItem = latestPage.items[latestPage.items.length - 1];

//        // The download count is typically available on the package metadata.
//        const downloadCount = latestVersionItem.catalogEntry.downloadCount;

//        console.log(`Total downloads for ${packageName}: ${downloadCount}`);
//        return downloadCount;

//    } catch (error) {
//        console.error("Failed to fetch NuGet total downloads:", error);
//    }
//}

//// Example usage
//await getNugetPackageTotalDownloads('Newtonsoft.Json');

// Function to get the download count for a NuGet package
async function getNugetDownloads(packageName) {
    const apiUrl = `https://azuresearch-usnc.nuget.org/query?q=${packageName}&prerelease=false`;

    try {
        const response = await fetch(apiUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json();
        // The 'totalDownloads' is a property of the first package in the search results
        if (data.data && data.data.length > 0) {
            const packageInfo = data.data[0];
            console.log(`Package: ${packageInfo.id}`);
            console.log(`Total Downloads: ${packageInfo.totalDownloads.toLocaleString()}`);
            return packageInfo.totalDownloads;
        } else {
            console.log(`Package '${packageName}' not found.`);
            return null;
        }
    } catch (error) {
        console.error("Error fetching NuGet data:", error);
        return null;
    }
}

// Call the function with a package name
await getNugetDownloads("Newtonsoft.Json");






async function searchNugetPackagesByOwnerKeyword(ownerName) {
    const nugetSearchUrl = `https://api-v2v3search-0.nuget.org/query?q=${encodeURIComponent(ownerName)}&prerelease=true&semVerLevel=2.0.0`;

    try {
        const response = await fetch(nugetSearchUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        return data.data; // The 'data' array contains the search results
    } catch (error) {
        console.error("Error fetching NuGet packages:", error);
        return [];
    }
}