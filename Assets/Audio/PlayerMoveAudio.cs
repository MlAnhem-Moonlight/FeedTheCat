using UnityEngine;

/// <summary>
/// Lắng nghe sự kiện tĩnh PlayerController.OnPlayerStep để tự động phát âm thanh
/// mỗi khi Player di chuyển thành công 1 ô trên lưới. Không cần sửa gì vào PlayerController.cs.
///
/// Gắn script này vào 1 GameObject đang active trong scene Gameplay
/// (ví dụ: chính object Player, hoặc GameManager, hoặc 1 GameObject "AudioHooks" riêng).
/// </summary>
public class PlayerMoveAudio : MonoBehaviour
{
    [Tooltip("Id âm thanh bước đi, phải trùng với id đã khai báo trong Sfx List của AudioManager. Ví dụ: player_step")]
    public string stepSfxId = "player_step";

    private void OnEnable()
    {
        // Đăng ký lắng nghe event mỗi khi script này được bật
        PlayerController.OnPlayerStep += HandlePlayerStep;
    }

    private void OnDisable()
    {
        // Huỷ đăng ký khi tắt để tránh lỗi hoặc gọi trùng nhiều lần
        PlayerController.OnPlayerStep -= HandlePlayerStep;
    }

    private void HandlePlayerStep()
    {
        if (string.IsNullOrEmpty(stepSfxId)) return;
        AudioManager.Instance.PlaySFX(stepSfxId);
    }
}
