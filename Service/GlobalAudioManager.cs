using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-9996)]
public class GlobalAudioManager : MonoBehaviour
{
    [Header("默认参数")]
    [Range(0f, 1f)] public float defaultSEVolume = 1f;
    [Range(0f, 1f)] public float defaultBGMVolume = 0.6f;
    [Range(0f, 1f)] public float defaultSpatialBlend = 0f;

    [Header("音源池")]
    [Min(2)] public int oneShotSourceCount = 6;
    [Min(1)] public int initialManagedSourceCount = 2;

    private ResourceCenter _resourceCenter;
    private AudioSource _bgmSource;

    private readonly List<AudioSource> _oneShotSources = new List<AudioSource>();
    private readonly List<AudioSource> _managedSources = new List<AudioSource>();
    private readonly Dictionary<int, ActivePlayback> _activePlaybacks = new Dictionary<int, ActivePlayback>();

    private int _oneShotIndex = -1;
    private int _nextPlaybackId = 1;

    public bool IsInitialized { get; private set; }

    private sealed class ActivePlayback
    {
        public int Id;
        public AudioSource Source;
        public Coroutine Routine;
    }

    public void Initialize(ResourceCenter resourceCenter)
    {
        _resourceCenter = resourceCenter;

        if (_resourceCenter != null && !_resourceCenter.IsInitialized)
        {
            _resourceCenter.Initialize();
        }

        EnsureBGMSource();
        EnsureOneShotSources();
        EnsureManagedSources();

        IsInitialized = true;
    }

    #region 对外 API

    /// <summary>
    /// 播放一次
    /// </summary>
    public void Play(string clipName, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        AudioClip clip = ResolveClip(clipName);
        if (clip == null)
            return;

        Play(clip, position, volume, spatialBlend);
    }

    /// <summary>
    /// 直接播放一个 AudioClip 一次
    /// </summary>
    public void Play(AudioClip clip, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        if (clip == null || !EnsureReady())
            return;

        AudioSource source = GetNextOneShotSource();
        ConfigureSource(
            source,
            position,
            volume < 0f ? defaultSEVolume : Mathf.Clamp01(volume),
            spatialBlend < 0f ? defaultSpatialBlend : Mathf.Clamp01(spatialBlend),
            false);

        source.clip = null;
        source.PlayOneShot(clip, source.volume);
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(string clipName, float volume = -1f, bool restartIfSame = false)
    {
        AudioClip clip = ResolveClip(clipName);
        if (clip == null)
            return;

        PlayBGM(clip, volume, restartIfSame);
    }

    public void PlayBGM(AudioClip clip, float volume = -1f, bool restartIfSame = false)
    {
        if (clip == null || !EnsureReady())
            return;

        EnsureBGMSource();

        bool clipChanged = _bgmSource.clip != clip;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.volume = volume < 0f ? defaultBGMVolume : Mathf.Clamp01(volume);

        if (clipChanged || restartIfSame || !_bgmSource.isPlaying)
        {
            _bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
    }

    public void SetBGMVolume(float volume)
    {
        EnsureBGMSource();
        _bgmSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 无限循环播放，返回播放 id，用于外部停止
    /// </summary>
    public int PlayLoop(string clipName, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        AudioClip clip = ResolveClip(clipName);
        if (clip == null)
            return -1;

        return PlayLoop(clip, position, volume, spatialBlend);
    }

    public int PlayLoop(AudioClip clip, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        if (clip == null || !EnsureReady())
            return -1;

        AudioSource source = GetAvailableManagedSource();
        ConfigureSource(
            source,
            position,
            volume < 0f ? defaultSEVolume : Mathf.Clamp01(volume),
            spatialBlend < 0f ? defaultSpatialBlend : Mathf.Clamp01(spatialBlend),
            true);

        source.clip = clip;
        source.Play();

        int playbackId = _nextPlaybackId++;
        _activePlaybacks[playbackId] = new ActivePlayback
        {
            Id = playbackId,
            Source = source,
            Routine = null
        };

        return playbackId;
    }

    /// <summary>
    /// 固定播放很多次，例如 3 次、5 次
    /// interval <= 0 时，默认按音频长度连续播
    /// 返回 id，可中途 Stop(id)
    /// </summary>
    public int PlayRepeat(string clipName, int times, float interval = -1f, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        AudioClip clip = ResolveClip(clipName);
        if (clip == null)
            return -1;

        return PlayRepeat(clip, times, interval, position, volume, spatialBlend);
    }

    public int PlayRepeat(AudioClip clip, int times, float interval = -1f, Vector3 position = default, float volume = -1f, float spatialBlend = -1f)
    {
        if (clip == null || !EnsureReady() || times <= 0)
            return -1;

        AudioSource source = GetAvailableManagedSource();

        int playbackId = _nextPlaybackId++;
        ActivePlayback playback = new ActivePlayback
        {
            Id = playbackId,
            Source = source
        };

        _activePlaybacks[playbackId] = playback;
        playback.Routine = StartCoroutine(PlayRepeatRoutine(
            playbackId,
            clip,
            times,
            interval,
            position,
            volume < 0f ? defaultSEVolume : Mathf.Clamp01(volume),
            spatialBlend < 0f ? defaultSpatialBlend : Mathf.Clamp01(spatialBlend)));

        return playbackId;
    }

    /// <summary>
    /// 停止某个循环/重复播放
    /// </summary>
    public bool Stop(int playbackId)
    {
        return FinishPlayback(playbackId, true, true);
    }

    /// <summary>
    /// 停止所有循环/重复播放
    /// </summary>
    public void StopAllManaged()
    {
        if (_activePlaybacks.Count == 0)
            return;

        List<int> ids = new List<int>(_activePlaybacks.Keys);
        for (int i = 0; i < ids.Count; i++)
        {
            FinishPlayback(ids[i], true, true);
        }
    }

    #endregion

    #region 内部实现

    private IEnumerator PlayRepeatRoutine(int playbackId, AudioClip clip, int times, float interval, Vector3 position, float volume, float spatialBlend)
    {
        if (!_activePlaybacks.TryGetValue(playbackId, out ActivePlayback playback) || playback.Source == null)
            yield break;

        AudioSource source = playback.Source;
        ConfigureSource(source, position, volume, spatialBlend, false);
        source.clip = null;

        float clipLength = clip.length > 0f ? clip.length : 0.01f;
        float waitBetween = interval > 0f ? interval : clipLength;

        for (int i = 0; i < times; i++)
        {
            source.transform.position = position;
            source.PlayOneShot(clip, volume);

            if (i < times - 1)
            {
                yield return new WaitForSeconds(waitBetween);
            }
            else
            {
                yield return new WaitForSeconds(clipLength);
            }
        }

        FinishPlayback(playbackId, true, false);
    }

    private bool EnsureReady()
    {
        if (IsInitialized && _resourceCenter != null)
            return true;

        Root root = Root.Instance;
        if (root == null)
        {
            root = GetComponentInParent<Root>();
        }

        if (root == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[GlobalAudioManager] 找不到 Root。", this);
#endif
            return false;
        }

        if (root.resourceCenter == null)
        {
            root.resourceCenter = root.GetComponentInChildren<ResourceCenter>(true);
        }

        Initialize(root.resourceCenter);

        if (_resourceCenter == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[GlobalAudioManager] 找不到 ResourceCenter。", this);
#endif
            return false;
        }

        return true;
    }

    private AudioClip ResolveClip(string clipName)
    {
        if (!EnsureReady())
            return null;

        if (string.IsNullOrEmpty(clipName))
            return null;

        return _resourceCenter.GetAudioClip(clipName);
    }

    private void EnsureBGMSource()
    {
        if (_bgmSource != null)
            return;

        _bgmSource = GetOrCreateChildSource("BGMSource", true);
        _bgmSource.volume = defaultBGMVolume;
        _bgmSource.spatialBlend = 0f;
    }

    private void EnsureOneShotSources()
    {
        int count = Mathf.Max(2, oneShotSourceCount);

        while (_oneShotSources.Count < count)
        {
            AudioSource source = GetOrCreateChildSource($"OneShotSource_{_oneShotSources.Count}", false);
            source.volume = defaultSEVolume;
            source.spatialBlend = defaultSpatialBlend;
            _oneShotSources.Add(source);
        }
    }

    private void EnsureManagedSources()
    {
        int count = Mathf.Max(1, initialManagedSourceCount);

        while (_managedSources.Count < count)
        {
            AudioSource source = GetOrCreateChildSource($"ManagedSource_{_managedSources.Count}", false);
            source.volume = defaultSEVolume;
            source.spatialBlend = defaultSpatialBlend;
            _managedSources.Add(source);
        }
    }

    private AudioSource GetNextOneShotSource()
    {
        EnsureOneShotSources();

        _oneShotIndex++;
        if (_oneShotIndex >= _oneShotSources.Count)
        {
            _oneShotIndex = 0;
        }

        return _oneShotSources[_oneShotIndex];
    }

    private AudioSource GetAvailableManagedSource()
    {
        EnsureManagedSources();

        for (int i = 0; i < _managedSources.Count; i++)
        {
            AudioSource source = _managedSources[i];
            if (source == null)
                continue;

            if (!IsManagedSourceBusy(source) && !source.isPlaying)
            {
                return source;
            }
        }

        AudioSource newSource = GetOrCreateChildSource($"ManagedSource_{_managedSources.Count}", false);
        _managedSources.Add(newSource);
        return newSource;
    }

    private bool IsManagedSourceBusy(AudioSource source)
    {
        foreach (KeyValuePair<int, ActivePlayback> pair in _activePlaybacks)
        {
            if (pair.Value != null && pair.Value.Source == source)
                return true;
        }

        return false;
    }

    private AudioSource GetOrCreateChildSource(string childName, bool loop)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        AudioSource source = child.GetComponent<AudioSource>();
        if (source == null)
        {
            source = child.gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;

        return source;
    }

    private void ConfigureSource(AudioSource source, Vector3 position, float volume, float spatialBlend, bool loop)
    {
        source.Stop();
        source.transform.position = position;
        source.volume = Mathf.Clamp01(volume);
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.loop = loop;
    }

    private bool FinishPlayback(int playbackId, bool stopSource, bool stopRoutine)
    {
        if (!_activePlaybacks.TryGetValue(playbackId, out ActivePlayback playback))
            return false;

        if (stopRoutine && playback.Routine != null)
        {
            StopCoroutine(playback.Routine);
            playback.Routine = null;
        }

        if (playback.Source != null)
        {
            if (stopSource)
            {
                playback.Source.Stop();
            }

            playback.Source.clip = null;
            playback.Source.loop = false;
        }

        _activePlaybacks.Remove(playbackId);
        return true;
    }

    private void OnDestroy()
    {
        StopAllManaged();

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
    }

    #endregion
}