using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("전체 채팅 패널(ScrollRect + 하단바 통째로)")]
    [SerializeField] private RectTransform chatRoot;

    [Header("대략적인 키보드 높이(화면 높이 비율)")]
    [Range(0f, 1f)]
    [SerializeField] private float keyboardHeightRatio = 0.4f; // 40% 정도

    private Vector2 originalOffsetMin;

    void Awake()
    {
        if (chatRoot == null)
            chatRoot = GetComponent<RectTransform>();

        originalOffsetMin = chatRoot.offsetMin;
    }

    /// <summary>
    /// InputField 에 포커스 들어올 때 호출
    /// </summary>
    public void OnInputSelected()
    {
        float kb = Screen.height * keyboardHeightRatio;
        chatRoot.offsetMin = new Vector2(originalOffsetMin.x,
                                         originalOffsetMin.y + kb);
    }

    /// <summary>
    /// InputField 포커스 빠질 때 호출
    /// </summary>
    public void OnInputDeselected()
    {
        chatRoot.offsetMin = originalOffsetMin;
    }
}