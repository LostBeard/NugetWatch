using System.Text.Json.Serialization;

namespace NugetWatch.Services
{
    /// <summary>
    /// GitHub repository file information<br/>
    /// https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/contents/.editorconfig?ref=main
    /// </summary>
    public class GitHubRepoFile
    {
        #region Custom properties
        /// <summary>
        /// {Owner}/{Repo}/{Path}
        /// </summary>
        public string FullPath => $"{Owner}/{Repo}/{Path}";
        /// <summary>
        /// Owner
        /// </summary>
        public string Owner { get; set; }
        /// <summary>
        /// Repo
        /// </summary>
        public string Repo { get; set; }
        #endregion
        /// <summary>
        /// Entry name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Entry path in the repo
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Entry sha hash
        /// </summary>
        public string Sha { get; set; }

        /// <summary>
        /// Entry size
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Entry url that returns this info
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Entry link to the normal GitHub webpage that views this file
        /// </summary>
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("git_url")]
        public string GitUrl { get; set; }

        /// <summary>
        /// Entry download link. Used to get file contents.
        /// </summary>
        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Entry type. "file", "dir"
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Entry content (may not be populated)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Entry content encoding
        /// </summary>
        public string? Encoding { get; set; }

        public override string ToString() => FullPath ?? "";
    }
}

//[
//  {
//    "name": ".editorconfig",
//    "path": ".editorconfig",
//    "sha": "cc789785501080fe20197f4ddba6962bdb6cd59f",
//    "size": 147,
//    "url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/contents/.editorconfig?ref=main",
//    "html_url": "https://github.com/LostBeard/SpawnDev.BlazorJS/blob/main/.editorconfig",
//    "git_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/blobs/cc789785501080fe20197f4ddba6962bdb6cd59f",
//    "download_url": "https://raw.githubusercontent.com/LostBeard/SpawnDev.BlazorJS/main/.editorconfig",
//    "type": "file",
//    "_links": {
//      "self": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/contents/.editorconfig?ref=main",
//      "git": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/blobs/cc789785501080fe20197f4ddba6962bdb6cd59f",
//      "html": "https://github.com/LostBeard/SpawnDev.BlazorJS/blob/main/.editorconfig"
//    }
//  },

// git_url
//{
//  "sha": "cc789785501080fe20197f4ddba6962bdb6cd59f",
//  "node_id": "B_kwDOIkJdD9oAKGNjNzg5Nzg1NTAxMDgwZmUyMDE5N2Y0ZGRiYTY5NjJiZGI2Y2Q1OWY",
//  "size": 147,
//  "url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/blobs/cc789785501080fe20197f4ddba6962bdb6cd59f",
//  "content": "77u/WyouY3NdCgojIENBMjAxODogJ0J1ZmZlci5CbG9ja0NvcHknIGV4cGVj\ndHMgdGhlIG51bWJlciBvZiBieXRlcyB0byBiZSBjb3BpZWQgZm9yIHRoZSAn\nY291bnQnIGFyZ3VtZW50CmRvdG5ldF9kaWFnbm9zdGljLkNBMjAxOC5zZXZl\ncml0eSA9IG5vbmUK\n",
//  "encoding": "base64"
//}