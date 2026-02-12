using UnityEngine;
using UnityEngine.UI;

public class LoadingBarUI : MonoBehaviour
{
    [Header("프레임을 표시할 Image")]
    public Image targetImage;

    [Header("프레임 스프라이트 (Inspector에서 직접 넣거나, 아래 자동 로딩 사용)")]
    public Sprite[] frames;
    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // 초기 프레임
        if (frames != null && frames.Length > 0 && targetImage != null)
            targetImage.sprite = frames[0];
    }

    /// <summary>
    /// 0~1 진행률을 해당하는 프레임으로 표시
    /// </summary>
    public void SetProgress(float t)
    {
        if (frames == null || frames.Length == 0 || targetImage == null) return;

        t = Mathf.Clamp01(t);
        int idx = Mathf.RoundToInt(t * (frames.Length - 1));
        targetImage.sprite = frames[idx];
    }
}
