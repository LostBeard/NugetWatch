using SpawnDev.BlazorJS.JSObjects;

namespace NugetWatch.Services
{
    public class AudioService
    {
        public AudioService()
        {

        }

        public Dictionary<string, string> Sounds { get; } = new Dictionary<string, string>
        {
            { "door_open", "audio/door_open.mp3"},
            { "door_close", "audio/door_close.mp3"},
            { "scream", "audio/scream.mp3"},
        };

        Dictionary<string, Audio> _soundAudio = new Dictionary<string, Audio>();

        public void PlaySound(string name)
        {
            Audio? audio = null;
            if (!_soundAudio.TryGetValue(name, out audio))
            {
                if (!Sounds.TryGetValue(name, out var soundUrl)) return;
                audio = new Audio(soundUrl);
                _soundAudio[name] = audio;
            }
            audio.Play();
        }
        public async Task PlaySoundUrl(string url)
        {
            url = url ?? "https://www2.cs.uic.edu/~i101/SoundFiles/CantinaBand3.wav";
            using var audio = new Audio(url);
            var tcs = new TaskCompletionSource();
            var onError = new Action(() => tcs.SetException(new Exception("Play failed")));
            var OnEnded = new Action(() => tcs.SetResult());
            audio.OnError += onError;
            audio.OnEnded += OnEnded;
            try
            {
                await audio.Play();
                await tcs.Task;
            }
            finally
            {
                audio.OnError -= onError;
                audio.OnEnded -= OnEnded;
            }
        }
    }
}
