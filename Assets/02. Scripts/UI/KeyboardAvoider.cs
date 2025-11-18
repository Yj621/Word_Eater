using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("전체 채팅 패널 (메시지 + Input_Group 부모)")]
    [SerializeField] private RectTransform chatRoot;

    [Header("추가로 더 올리고 싶을 때(UI px)")]
    [SerializeField] private float extraBottomPadding = 400f;

    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private Vector2 originalOffsetMin;
    private float currentKeyboardHeight;

    void Awake()
    {
        if (chatRoot == null)
            chatRoot = GetComponent<RectTransform>();

        rootCanvas = chatRoot.GetComponentInParent<Canvas>();
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        // 원래 하단 offset 저장
        originalOffsetMin = chatRoot.offsetMin;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetKeyboardHeight(); // 화면 픽셀 단위

        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;

            // ── 화면픽셀 → 캔버스 좌표 변환 ──
            // (CanvasScaler 설정 상관없이 안전하게 변환)
            float screenHeight = rootCanvas.pixelRect.height;
            float canvasHeight = canvasRect.rect.height;
            float pixelToCanvas = canvasHeight / screenHeight;

            float uiKeyboardHeight = keyboardHeight * pixelToCanvas;

            float bottom = originalOffsetMin.y + uiKeyboardHeight + extraBottomPadding;

            chatRoot.offsetMin = new Vector2(originalOffsetMin.x, bottom);
        }
#endif
    }

    private float GetKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f;
#elif UNITY_ANDROID
        // 1) TouchScreenKeyboard 값이 있으면 그거 사용
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            return TouchScreenKeyboard.area.height;

        // 2) 그래도 0이면, 안드로이드 네이티브로 계산
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var window = activity.Call<AndroidJavaObject>("getWindow");
                var decorView = window.Call<AndroidJavaObject>("getDecorView");
                var rect = new AndroidJavaObject("android.graphics.Rect");

                decorView.Call("getWindowVisibleDisplayFrame", rect);

                int visibleHeight = rect.Call<int>("height");
                int screenHeight = decorView.Call<int>("getHeight");
                int keyboardHeight = screenHeight - visibleHeight;

                // 네비게이션바 오인 방지용(너무 작으면 0 처리)
                if (keyboardHeight < screenHeight * 0.10f)
                    return 0f;

                return keyboardHeight;
            }
        }
        catch
        {
            return 0f;
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