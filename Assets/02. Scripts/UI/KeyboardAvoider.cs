using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("키보드에 맞춰 올릴 RectTransform (Input_Group)")]
    [SerializeField] private RectTransform target;

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
        float keyboardHeight = GetKeyboardHeight();

        if (Mathf.Approximately(currentKeyboardHeight, keyboardHeight) == false)
        {
            currentKeyboardHeight = keyboardHeight;

            // 스크린 픽셀 → UI 픽셀
            float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;

            // 아래에서 위로 올리기
            target.anchoredPosition =
                originalAnchoredPos + new Vector2(0f, uiKeyboardHeight);
        }
#endif
    }

    private float GetKeyboardHeight()
    {
#if UNITY_EDITOR
        // 에디터에서는 실제 키보드가 없으니 0
        return 0f;
#elif UNITY_ANDROID || UNITY_IOS
        // 모바일에선 TouchScreenKeyboard.area 가 대부분 잘 동작
        if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area.height;
        return 0f;
#else
        return 0f;
#endif
    }
}
