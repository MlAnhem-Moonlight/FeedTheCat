using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý toàn bộ âm thanh của game: nhạc nền (Theme) và hiệu ứng (SFX/UI FX).
/// Đặt script này vào 1 GameObject rỗng tên "AudioManager", để trong Scene đầu tiên (MainMenu),
/// nó sẽ tự DontDestroyOnLoad và tồn tại xuyên suốt các Scene khác.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string id;           // ID để gọi phát âm thanh, vd: "click", "theme_main", "cat_meow"
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
    }

    [Header("Nhạc nền (Theme)")]
    public List<Sound> musicList = new List<Sound>();

    [Header("Hiệu ứng âm thanh (SFX / UI FX)")]
    public List<Sound> sfxList = new List<Sound>();

    [Header("Cấu hình")]
    [SerializeField] private int sfxPoolSize = 8;   // số AudioSource dùng để phát nhiều SFX cùng lúc
    [SerializeField] private float musicFadeDuration = 0.6f;

    private Dictionary<string, Sound> _musicDict;
    private Dictionary<string, Sound> _sfxDict;

    private AudioSource _musicSource;
    private List<AudioSource> _sfxPool;

    private float _musicVolumeScale = 1f;
    private float _sfxVolumeScale = 1f;

    private void Awake()
    {
        // ---- Singleton + DontDestroyOnLoad ----
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionaries();
        SetupAudioSources();
    }

    private void BuildDictionaries()
    {
        _musicDict = new Dictionary<string, Sound>();
        foreach (var s in musicList)
        {
            if (!_musicDict.ContainsKey(s.id)) _musicDict.Add(s.id, s);
        }

        _sfxDict = new Dictionary<string, Sound>();
        foreach (var s in sfxList)
        {
            if (!_sfxDict.ContainsKey(s.id)) _sfxDict.Add(s.id, s);
        }
    }

    private void SetupAudioSources()
    {
        // Nguồn phát nhạc nền (chỉ 1, có loop)
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;

        // Pool nguồn phát SFX để phát được nhiều tiếng cùng lúc (vd: nhiều mèo kêu, nhiều click)
        _sfxPool = new List<AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            _sfxPool.Add(src);
        }
    }

    // ================== PHÁT NHẠC NỀN (THEME) ==================

    /// <summary>Phát nhạc nền theo id, có fade nhẹ. Gọi vd: AudioManager.Instance.PlayMusic("theme_main");</summary>
    public void PlayMusic(string id, bool fade = true)
    {
        if (!_musicDict.TryGetValue(id, out Sound s))
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy nhạc nền id: {id}");
            return;
        }

        if (_musicSource.clip == s.clip && _musicSource.isPlaying) return; // đang phát đúng bài rồi thì thôi

        StopAllCoroutines();
        if (fade) StartCoroutine(FadeToNewMusic(s));
        else
        {
            _musicSource.clip = s.clip;
            _musicSource.volume = s.volume * _musicVolumeScale;
            _musicSource.pitch = s.pitch;
            _musicSource.Play();
        }
    }

    private IEnumerator FadeToNewMusic(Sound s)
    {
        float t = 0f;
        float startVol = _musicSource.volume;

        // fade out bài cũ
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, t / musicFadeDuration);
            yield return null;
        }

        _musicSource.clip = s.clip;
        _musicSource.pitch = s.pitch;
        _musicSource.Play();

        // fade in bài mới
        t = 0f;
        float targetVol = s.volume * _musicVolumeScale;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(0f, targetVol, t / musicFadeDuration);
            yield return null;
        }
        _musicSource.volume = targetVol;
    }

    public void StopMusic() => _musicSource.Stop();

    // ================== PHÁT SFX / UI FX ==================

    /// <summary>Phát 1 hiệu ứng âm thanh theo id. Gọi vd: AudioManager.Instance.PlaySFX("button_click");</summary>
    public void PlaySFX(string id)
    {
        if (!_sfxDict.TryGetValue(id, out Sound s))
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy SFX id: {id}");
            return;
        }

        AudioSource src = GetFreeSfxSource();
        src.clip = s.clip;
        src.volume = s.volume * _sfxVolumeScale;
        src.pitch = s.pitch;
        src.loop = s.loop;
        src.Play();
    }

    /// <summary>Phát SFX tại 1 vị trí trong world (vd: tiếng mèo kêu tại vị trí NPC).</summary>
    public void PlaySFXAtPoint(string id, Vector3 position)
    {
        if (!_sfxDict.TryGetValue(id, out Sound s))
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy SFX id: {id}");
            return;
        }
        AudioSource.PlayClipAtPoint(s.clip, position, s.volume * _sfxVolumeScale);
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var src in _sfxPool)
            if (!src.isPlaying) return src;

        // hết chỗ trống thì dùng tạm cái đầu tiên (ghi đè)
        return _sfxPool[0];
    }

    // ================== ÂM LƯỢNG (dùng cho Settings_Panel) ==================

    public void SetMusicVolume(float value01)
    {
        _musicVolumeScale = Mathf.Clamp01(value01);
        _musicSource.volume = _musicVolumeScale;
    }

    public void SetSFXVolume(float value01)
    {
        _sfxVolumeScale = Mathf.Clamp01(value01);
    }

    // ================== GÁN SỰ KIỆN CHO BUTTON / NPC / PLAYER ==================

    /// <summary>
    /// Gán sự kiện click cho 1 Button để tự phát SFX, dùng ở code (Awake/Start của UI Manager).
    /// Ví dụ: AudioManager.Instance.BindButtonSFX(btnPlay, "button_click");
    /// </summary>
    public void BindButtonSFX(Button button, string sfxId)
    {
        if (button == null) return;
        button.onClick.AddListener(() => PlaySFX(sfxId));
    }

    /// <summary>
    /// Gán nhanh SFX cho nhiều Button cùng lúc, dùng chung 1 âm thanh (vd: mọi nút menu đều kêu "click").
    /// </summary>
    public void BindButtonsSFX(List<Button> buttons, string sfxId)
    {
        foreach (var b in buttons) BindButtonSFX(b, sfxId);
    }
}
