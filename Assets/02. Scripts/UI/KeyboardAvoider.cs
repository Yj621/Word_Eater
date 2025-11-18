using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("키보드에 맞춰 올릴 RectTransform (Input_Group)")]
    [SerializeField] private RectTransform target;

    [Header("추가 보정값 (UI px 단위)")]
    [SerializeField] private float extraOffset = 200f;  // 필요하면 조절

    private Canvas rootCanvas;
    private Vector2 originalAnchoredPos;
    private float currentKeyboardHeight;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        rootCanvas = target.GetComponentInParent<Canvas>();
        originalAnchoredPos = target.anchoredPosition;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetKeyboardHeight();  // px

        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;

            // 스크린 픽셀 → UI 픽셀
            float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;

            // 추가 보정값을 UI px로 변환(스크린 px이면 곱해야 함)
            float extra = extraOffset;

            // 최종 이동
            target.anchoredPosition =
                originalAnchoredPos + new Vector2(0f, uiKeyboardHeight + extra);
        }
#endif
    }

    private float GetKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f;
#elif UNITY_ANDROID || UNITY_IOS
        if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area.height;
        return 0f;
#else
        return 0f;
#endif
    }
}
