using UnityEngine;

/// <summary>
/// Gắn script này lên panel Win hoặc Lose (root của prefab Winsc_Panel / Losesc_Panel).
/// Mỗi khi panel được SetActive(true), âm thanh tương ứng sẽ tự động phát - không cần
/// gọi code thủ công ở GameManager.
/// </summary>
public class ScreenSoundOnEnable : MonoBehaviour
{
    [Tooltip("Id âm thanh sẽ phát khi panel này bật lên, phải trùng với id đã khai báo trong Sfx List của AudioManager. Ví dụ: win_sound hoặc lose_sound")]
    public string sfxId;

    [Tooltip("Có tự tắt/giảm nhạc nền khi màn hình này hiện lên không")]
    public bool duckMusic = false;
    [Range(0f, 1f)] public float duckedMusicVolume = 0.2f;

    private void OnEnable()
    {
        //LogFilter.LogObject($"### TEST: ScreenSoundOnEnable.OnEnable() CHAY tren object '{gameObject.name}', sfxId = '{sfxId}' ###");

        if (string.IsNullOrEmpty(sfxId)) return;
        AudioManager.Instance.PlaySFX(sfxId);

        if (duckMusic)
            AudioManager.Instance.SetMusicVolume(duckedMusicVolume);
    }

    private void OnDisable()
    {
        // Khi tắt panel (đóng màn hình thắng/thua), trả nhạc nền về bình thường
        if (duckMusic)
            AudioManager.Instance.SetMusicVolume(1f);
    }
}