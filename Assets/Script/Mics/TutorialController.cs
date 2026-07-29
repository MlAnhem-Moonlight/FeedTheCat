using UnityEngine;

/// <summary>
/// Gắn script này vào GameObject tutorial (hướng dẫn chơi).
/// - Ở Start(): tự tìm GameManager + LevelManager, lấy level hiện tại đang áp dụng.
///   Nếu là level đầu tiên (index == 0) thì giữ nguyên (không làm gì, tutorial vẫn hiện).
///   Nếu không phải level đầu (index != 0) thì tự tắt GameObject này ngay lập tức.
/// - Trong lúc đang hiện (level 0), nếu người chơi chạm/click vào màn hình thì
///   tự tắt GameObject tutorial (Update() theo dõi input chuột + cảm ứng).
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Tooltip("Nếu true, chỉ tắt theo chạm màn hình chứ không tự động ẩn theo level (dùng để debug)")]
    public bool onlyDisableOnTouch = false;

    private void Start()
    {
        if (onlyDisableOnTouch)
            return;

        // Tự tìm GameManager và LevelManager trong scene
        GameManager gameManager = GameManager.Instance != null
            ? GameManager.Instance
            : FindAnyObjectByType<GameManager>();

        LevelManager levelManager = FindAnyObjectByType<LevelManager>();

        if (gameManager == null)
        {
            Debug.LogWarning("TutorialController: Không tìm thấy GameManager trong scene.");
            return;
        }

        // Ưu tiên lấy level hiện tại đang được LevelManager áp dụng (chính xác nhất vì
        // đây là LevelData thực sự đang chạy). Nếu không có LevelManager/level thì
        // fallback về GameManager.CurrentLevelIndex.
        int currentIndex = -1;

        if (levelManager != null && levelManager.level != null)
        {
            currentIndex = gameManager.allLevels.FindIndex(l => l == levelManager.level);
        }

        // Fallback nếu không xác định được qua LevelManager
        if (currentIndex < 0)
        {
            currentIndex = gameManager.CurrentLevelIndex;
        }

        Debug.Log($"TutorialController: currentIndex={currentIndex}");

        if (currentIndex == 0)
        {
            // Level đầu tiên -> giữ nguyên, không làm gì cả (tutorial vẫn hiện)
            return;
        }

        // Không phải level đầu tiên -> tự tắt bản thân
        gameObject.SetActive(false);
    }

    //private void Update()
    //{
    //    if (!gameObject.activeSelf)
    //        return;
    //    Debug.Log("TutorialController: Screen touched, disabling tutorial.");
    //    if (IsScreenTouched())
    //    {
    //        Debug.Log("TutorialController: Screen touched, disabling tutorial.");
    //        DisableSelf();
    //    }
    //}

    ///// <summary>
    ///// Kiểm tra người chơi có đang chạm/click vào màn hình hay không.
    ///// Hỗ trợ cả chuột (Editor/PC) lẫn cảm ứng (mobile).
    ///// </summary>
    //private bool IsScreenTouched()
    //{
        
    //    if (Input.GetMouseButtonDown(0))
    //        return true;

    //    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
    //        return true;

    //    return false;
    //}

    ///// <summary>
    ///// Tự tắt GameObject tutorial này. Có thể gọi trực tiếp từ Button.OnClick() trong Inspector nếu cần.
    ///// </summary>
    //public void DisableSelf()
    //{
    //    gameObject.SetActive(false);
    //}
}