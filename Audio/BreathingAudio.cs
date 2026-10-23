using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace BreathFX.Audio
{
    internal sealed class BreathingAudio : MonoBehaviour
    {
        private const float HeartbeatStartBreath = 0.75f;
        private const float HeartbeatFadeSpeed = 2f;
        private const float BreathSoundVolume = 0.3f;

        private AudioSource _heartbeatSource;
        private AudioSource _breathSource;
        private BreathingClip _heartbeatClip;
        private List<BreathingClip> _inhaleClips = new List<BreathingClip>();
        private List<BreathingClip> _exhaleClips = new List<BreathingClip>();
        private bool _loadStarted;
        private bool _mixerApplied;

        internal void Initialize()
        {
            if (_heartbeatSource == null)
            {
                _heartbeatSource = gameObject.AddComponent<AudioSource>();
                ConfigureSource(_heartbeatSource, loop: true);
                _heartbeatSource.volume = 0f;

                _breathSource = gameObject.AddComponent<AudioSource>();
                ConfigureSource(_breathSource, loop: false);
                _breathSource.volume = 1f;
            }

            StartLoading();
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.spatialize = false;
            source.dopplerLevel = 0f;
            source.reverbZoneMix = 0f;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
            source.priority = 16;
        }

        private void Update()
        {
            ApplyMixerGroup();
            StartLoading();
        }

        private void StartLoading()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ApplyMixerGroup();

            if (_loadStarted)
            {
                return;
            }

            _loadStarted = true;
            StartCoroutine(LoadClips());
        }

        private void ApplyMixerGroup()
        {
            if (_mixerApplied || !BetterAudio.Instantiated)
            {
                return;
            }

            AudioMixerGroup group = BetterAudio.Instance.VeryStandartMixerGroup;
            if (group == null)
            {
                return;
            }

            if (_heartbeatSource != null)
            {
                _heartbeatSource.outputAudioMixerGroup = group;
            }

            if (_breathSource != null)
            {
                _breathSource.outputAudioMixerGroup = group;
            }

            _mixerApplied = true;
        }

        internal void Tick(bool holding, float breath)
        {
            AudioClip heartbeatClip = _heartbeatClip != null ? _heartbeatClip.GetClip() : null;
            if (!Settings.EnableHeartbeat.Value || heartbeatClip == null || !holding)
            {
                FadeHeartbeatOut();
                return;
            }

            float intensity = Mathf.InverseLerp(HeartbeatStartBreath, 0f, Mathf.Clamp01(breath));
            if (intensity <= 0.001f)
            {
                FadeHeartbeatOut();
                return;
            }

            if (_heartbeatSource.clip != heartbeatClip)
            {
                _heartbeatSource.clip = heartbeatClip;
            }

            _heartbeatSource.volume = Mathf.Lerp(0.1f, 1f, intensity);

            if (!_heartbeatSource.isPlaying)
            {
                _heartbeatSource.Play();
            }
        }

        internal void OnHoldStarted()
        {
            if (Settings.EnableBreathSounds.Value)
            {
                PlayRandom(_inhaleClips);
            }
        }

        internal void OnHoldEnded()
        {
            if (Settings.EnableBreathSounds.Value)
            {
                PlayRandom(_exhaleClips);
            }
        }

        private void PlayRandom(List<BreathingClip> clips)
        {
            if (_breathSource == null || clips == null || clips.Count == 0)
            {
                return;
            }

            BreathingClip data = clips[UnityEngine.Random.Range(0, clips.Count)];
            AudioClip clip = data != null ? data.GetClip() : null;
            if (clip == null)
            {
                return;
            }

            bool viaBetterAudio = BetterAudio.Instantiated;
            if (viaBetterAudio)
            {
                BetterAudio.Instance.PlayNonspatial(
                    clip,
                    BetterAudio.AudioSourceGroupType.Nonspatial,
                    0f,
                    BreathSoundVolume,
                    BetterAudio.Instance.VeryStandartMixerGroup);
            }
            else
            {
                _breathSource.PlayOneShot(clip, BreathSoundVolume);
            }
        }

        internal void StopAll()
        {
            if (_heartbeatSource != null)
            {
                _heartbeatSource.Stop();
                _heartbeatSource.volume = 0f;
            }

            if (_breathSource != null)
            {
                _breathSource.Stop();
            }
        }

        private void FadeHeartbeatOut()
        {
            if (_heartbeatSource == null)
            {
                return;
            }

            if (!_heartbeatSource.isPlaying)
            {
                _heartbeatSource.volume = 0f;
                return;
            }

            _heartbeatSource.volume = Mathf.MoveTowards(
                _heartbeatSource.volume,
                0f,
                Time.deltaTime * HeartbeatFadeSpeed);

            if (_heartbeatSource.volume <= 0.001f)
            {
                _heartbeatSource.Stop();
            }
        }

        private IEnumerator LoadClips()
        {
            yield return BreathingAudioLoader.Load("heartbeat", clip => _heartbeatClip = clip);
            yield return BreathingAudioLoader.LoadAll("inhale", clips => _inhaleClips = clips);
            yield return BreathingAudioLoader.LoadAll("exhale", clips => _exhaleClips = clips);
        }
    }

    internal sealed class BreathingClip
    {
        internal string Name;
        internal float[] Samples;
        internal int SampleCount;
        internal int Channels;
        internal int Frequency;
        internal AudioClip Clip;

        internal AudioClip GetClip()
        {
            if (Clip != null &&
                Clip.channels > 0 &&
                Clip.samples == SampleCount &&
                Clip.loadState == AudioDataLoadState.Loaded)
            {
                return Clip;
            }

            if (Samples == null || SampleCount <= 0 || Channels <= 0 || Frequency <= 0)
            {
                return null;
            }

            AudioClip rebuilt = AudioClip.Create(Name, SampleCount, Channels, Frequency, false);
            if (rebuilt == null)
            {
                return null;
            }

            rebuilt.SetData(Samples, 0);
            rebuilt.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Clip = rebuilt;
            return rebuilt;
        }

        internal void Dispose()
        {
            Clip = null;
            Samples = null;
            SampleCount = 0;
        }
    }

    internal static class BreathingAudioLoader
    {
        private static readonly Dictionary<string, BreathingClip> Cache =
            new Dictionary<string, BreathingClip>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] Extensions = { ".wav", ".ogg", ".mp3" };

        internal static IEnumerator Load(string baseName, Action<BreathingClip> assign)
        {
            string path = FindSingleFile(baseName);
            if (path == null)
            {
                yield break;
            }

            BreathingClip clip = null;
            yield return LoadFile(path, baseName, loaded => clip = loaded);
            if (clip != null)
            {
                assign?.Invoke(clip);
            }
        }

        internal static IEnumerator LoadAll(string baseName, Action<List<BreathingClip>> assign)
        {
            string[] paths = FindAllFiles(baseName);
            if (paths.Length == 0)
            {
                yield break;
            }

            List<BreathingClip> clips = new List<BreathingClip>();
            foreach (string path in paths)
            {
                BreathingClip clip = null;
                string clipName = Path.GetFileNameWithoutExtension(path);
                yield return LoadFile(path, clipName, loaded => clip = loaded);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            if (clips.Count > 0)
            {
                assign?.Invoke(clips);
            }
        }

        internal static void RecoverForRaid()
        {
            foreach (BreathingClip clip in Cache.Values)
            {
                clip.GetClip();
            }
        }

        internal static void Dispose()
        {
            foreach (BreathingClip clip in Cache.Values)
            {
                clip.Dispose();
            }

            Cache.Clear();
        }

        private static IEnumerator LoadFile(string path, string clipName, Action<BreathingClip> assign)
        {
            if (Cache.TryGetValue(path, out BreathingClip cached) && cached != null)
            {
                if (cached.GetClip() != null)
                {
                    assign?.Invoke(cached);
                    yield break;
                }

                cached.Dispose();
                Cache.Remove(path);
            }

            string url = new Uri(path).AbsoluteUri;
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, GetAudioType(path));

            try
            {
                if (request.downloadHandler is DownloadHandlerAudioClip handler)
                {
                    handler.streamAudio = false;
                }

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Log.LogWarning($"Failed to load sound '{clipName}': {request.error}");
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null || clip.channels <= 0 || clip.samples <= 0)
                {
                    Plugin.Log.LogWarning($"Sound '{clipName}' loaded as an empty clip.");
                    yield break;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                    while (clip.loadState == AudioDataLoadState.Loading)
                    {
                        yield return null;
                    }
                }

                int sampleCount = clip.samples;
                int channels = clip.channels;
                int frequency = clip.frequency;
                float[] samples = new float[sampleCount * channels];
                if (!clip.GetData(samples, 0))
                {
                    Plugin.Log.LogWarning($"Could not read PCM data for '{clipName}'.");
                    yield break;
                }

                float peak = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    float value = Mathf.Abs(samples[i]);
                    if (value > peak)
                    {
                        peak = value;
                    }
                }

                float targetPeak = clipName.StartsWith("heartbeat", StringComparison.OrdinalIgnoreCase)
                    ? 0.65f
                    : 0.8f;
                float gain = peak > 0.0001f ? Mathf.Min(targetPeak / peak, 8f) : 1f;
                if (Mathf.Abs(gain - 1f) > 0.01f)
                {
                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
                    }
                }

                string name = "BreathFX_" + clipName;
                BreathingClip loaded = new BreathingClip
                {
                    Name = name,
                    Samples = samples,
                    SampleCount = sampleCount,
                    Channels = channels,
                    Frequency = frequency
                };

                // Keep decoded PCM in managed memory
                Cache[path] = loaded;
                assign?.Invoke(loaded);
            }
            finally
            {
                request.Dispose();
            }
        }

        private static string FindSingleFile(string baseName)
        {
            string resourcesFolder = GetResourcesFolder();
            if (resourcesFolder == null)
            {
                return null;
            }

            foreach (string extension in Extensions)
            {
                string path = Path.Combine(resourcesFolder, baseName + extension);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            string[] variants = FindAllFiles(baseName);
            return variants.Length > 0 ? variants[0] : null;
        }

        private static string[] FindAllFiles(string baseName)
        {
            string resourcesFolder = GetResourcesFolder();
            if (resourcesFolder == null || !Directory.Exists(resourcesFolder))
            {
                return Array.Empty<string>();
            }

            foreach (string extension in Extensions)
            {
                string exact = Path.Combine(resourcesFolder, baseName + extension);
                if (File.Exists(exact))
                {
                    return new[] { exact };
                }
            }

            List<string> paths = new List<string>();
            foreach (string extension in Extensions)
            {
                string[] candidates = Directory.GetFiles(resourcesFolder, baseName + "*" + extension);
                Array.Sort(candidates, StringComparer.OrdinalIgnoreCase);

                foreach (string path in candidates)
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    string suffix = name.Substring(baseName.Length);
                    if (suffix.Length > 0 && IsDigits(suffix))
                    {
                        paths.Add(path);
                    }
                }
            }

            return paths.ToArray();
        }

        private static string GetResourcesFolder()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return Path.Combine(assemblyFolder, "Resources");
        }

        private static bool IsDigits(string value)
        {
            foreach (char character in value)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static AudioType GetAudioType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".wav":
                    return AudioType.WAV;
                case ".mp3":
                    return AudioType.MPEG;
                default:
                    return AudioType.OGGVORBIS;
            }
        }
    }
}