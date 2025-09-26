using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using System.Net.Http.Json;

namespace NugetWatch.Services
{
    public class GitHubService
    {
        HttpClient HttpClient;
        BlazorJSRuntime JS;
        public GitHubService(HttpClient httpClient, BlazorJSRuntime js)
        {
            HttpClient = httpClient;
            JS = js;
        }
        // https://github.com/three-types/three-ts-types/archive/refs/heads/master.zip
        // https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS

        public async Task<GitHubRepo?> GetRepo(string owner, string repo)
        {
            var apiUrl = GetRepoInfoLink(owner, repo);
            try
            {
                var r = await HttpClient.GetFromJsonAsync<GitHubRepo>(apiUrl);
                return r;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public string GetRepoInfoLink(string owner, string repo)
        {
            return $"https://api.github.com/repos/{owner}/{repo}";
        }
        public string GetRepoDownloadLink(string owner, string repo, string branch)
        {
            return $"https://github.com/{owner}/{repo}/archive/refs/heads/{branch}.zip";
        }
        public Task<string?> GetRepoDefaultBranchDownloadLink(string ownerRepo) => GetRepoDefaultBranchDownloadLink(ownerRepo.Split('/')[0], ownerRepo.Split('/')[1]);
        public async Task<string?> GetRepoDefaultBranchDownloadLink(string owner, string repo)
        {
            var repoInfo = await GetRepo(owner, repo);
            if (string.IsNullOrEmpty(repoInfo?.DefaultBranch)) return null;
            var defaultBranch = repoInfo!.DefaultBranch;
            var repoDownloadLink = GetRepoDownloadLink(owner, repo, defaultBranch);
            return repoDownloadLink;
        }
        public Task<List<GitHubRepoFile>?> GetEntries(string ownerRepoPath, int page = 1, int perPage = 100)
        {
            var parts = ownerRepoPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"{nameof(ownerRepoPath)} must contain at least 'owner/repo'");
            }
            return GetEntries(parts[0], parts[1], string.Join("/", parts.Skip(2)), null, page, perPage);
        }
        /// <summary>
        /// 
        /// NOTE: Pagination does not currently work.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="repo"></param>
        /// <param name="path"></param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public async Task<List<GitHubRepoFile>?> GetEntries(string owner, string repo, string path, string? branch = null, int page = 1, int perPage = 100)
        {
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?page={page}";
            if (!string.IsNullOrEmpty(branch)) apiUrl += $"ref={branch}";
            try
            {
                using var req = new HttpRequestMessage
                {
                    RequestUri = new Uri(apiUrl),
                    Method = HttpMethod.Get,
                };
                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                using var resp = await HttpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    if (resp.Headers.TryGetValues("link", out var values))
                    {
                        var vs = values.ToList();
                        var lin = true;
                    }
                    var ret = await resp.Content.ReadFromJsonAsync<List<GitHubRepoFile>>();
                    if (ret != null)
                    {
                        if (resp.Content.Headers.TryGetValues("link", out var values3))
                        {
                            var vs = values3.ToList();
                            var lin = true;
                        }
                        foreach (var r in ret)
                        {
                            r.Owner = owner;
                            r.Repo = repo;
                        }
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public Task<GitHubRepoFile?> GetEntry(string ownerRepoPath)
        {
            var parts = ownerRepoPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"{nameof(ownerRepoPath)} must contain at least 'owner/repo'");
            }
            return GetEntry(parts[0], parts[1], string.Join("/", parts.Skip(2)));
        }
        public async Task<GitHubRepoFile?> GetEntry(string owner, string repo, string path, string? branch = null)
        {
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
            if (!string.IsNullOrEmpty(branch)) apiUrl += $"?ref={branch}";
            try
            {
                var r = await HttpClient.GetFromJsonAsync<GitHubRepoFile>(apiUrl);
                if (r != null)
                {
                    r.Owner = owner;
                    r.Repo = repo;
                }
                return r;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public Task<ArrayBuffer?> GetArrayBuffer(GitHubRepoFile entry) => GetArrayBuffer(entry.Owner, entry.Repo, entry.Path);
        public Task<ArrayBuffer?> GetArrayBuffer(string ownerRepoPath)
        {
            var parts = ownerRepoPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"{nameof(ownerRepoPath)} must contain at least 'owner/repo'");
            }
            return GetArrayBuffer(parts[0], parts[1], string.Join("/", parts.Skip(2)));
        }
        public async Task<ArrayBuffer?> GetArrayBuffer(string owner, string repo, string path)
        {
            var apiUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{path}";
            try
            {
                using var resp = await JS.Fetch(apiUrl);
                return await resp.ArrayBuffer();
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public Task<byte[]?> GetBytes(GitHubRepoFile entry) => GetBytes(entry.Owner, entry.Repo, entry.Path);
        public Task<byte[]?> GetBytes(string ownerRepoPath)
        {
            var parts = ownerRepoPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"{nameof(ownerRepoPath)} must contain at least 'owner/repo'");
            }
            return GetBytes(parts[0], parts[1], string.Join("/", parts.Skip(2)));
        }
        public async Task<byte[]?> GetBytes(string owner, string repo, string path)
        {
            var apiUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{path}";
            try
            {
                return await HttpClient.GetByteArrayAsync(apiUrl);
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public Task<string?> GetString(GitHubRepoFile entry) => GetString(entry.Owner, entry.Repo, entry.Path);
        public Task<string?> GetString(string ownerRepoPath)
        {
            var parts = ownerRepoPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"{nameof(ownerRepoPath)} must contain at least 'owner/repo'");
            }
            return GetString(parts[0], parts[1], string.Join("/", parts.Skip(2)));
        }
        public async Task<string?> GetString(string owner, string repo, string path)
        {
            var apiUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{path}";
            try
            {
                return await HttpClient.GetStringAsync(apiUrl);
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
    }
}
