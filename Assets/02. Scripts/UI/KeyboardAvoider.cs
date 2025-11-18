using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("키보드에 맞춰 올릴 RectTransform (Input_Group)")]
    [SerializeField] private RectTransform target;

    [Header("추가 배율 (1.0 = 키보드 높이 그대로)")]
    [SerializeField] private float heightMultiplier = 1.05f;

    [Header("추가 오프셋 (UI px 단위로 조금 더 올리고 싶을 때)")]
    [SerializeField] private float extraOffset = 20f;

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

    void OnEnable()
    {
        // 혹시 다른 씬에서 돌아왔다가 다시 올 때 초기화용
        if (target != null)
            target.anchoredPosition = originalAnchoredPos;
        currentKeyboardHeight = 0f;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetNativeKeyboardHeight();   // 스크린 픽셀

        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;

            // Canvas Scaler 고려해서 UI 좌표로 변환
            float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;

            float finalY = uiKeyboardHeight * heightMultiplier + extraOffset;

            // 아래에서 위로 올리기
            target.anchoredPosition =
                originalAnchoredPos + new Vector2(0f, finalY);
        }
#endif
    }

    /// <summary>
    /// 네이티브 방식으로 실제 키보드 높이 가져오기 (px)
    /// </summary>
    private float GetNativeKeyboardHeight()
    {
#if UNITY_EDITOR
        // 에디터에선 0 (원하면 여기서 임의 값 넣고 테스트해도 됨)
        return 0f;
#elif UNITY_ANDROID
        // 1) TouchScreenKeyboard 값 먼저 사용
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            return TouchScreenKeyboard.area.height;

        // 2) 안드로이드 네이티브로 계산 (네가 처음 썼던 방식)
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var rootView = currentActivity.Call<AndroidJavaObject>("getWindow")
                                          .Call<AndroidJavaObject>("getDecorView");
            var visibleRect = new AndroidJavaObject("android.graphics.Rect");

            rootView.Call("getWindowVisibleDisplayFrame", visibleRect);

            int screenHeight = rootView.Call<int>("getHeight");
            int visibleHeight = visibleRect.Call<int>("height");
            int keyboardHeight = screenHeight - visibleHeight;

            // 네비게이션 바 등 오인 방지
            if (keyboardHeight < screenHeight * 0.15f)
                return 0;

            return keyboardHeight;
        }
#elif UNITY_IOS
        if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area.height;
        return 0f;
#else
        return 0f;
#endif
    }
}