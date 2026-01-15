using UnityEngine;
using System.Collections.Generic; // Dictionary 사용을 위해 추가

public class KeyboardAvoider : MonoBehaviour
{
    [Header("키보드에 맞춰 올릴 타겟들 (여러 개 등록 가능)")]
    [SerializeField] private RectTransform[] targets; // 배열로 변경

    [Header("키보드 높이에 곱해줄 값 (1.0 = 그대로, 0.9 = 살짝 덜 올리기)")]
    [SerializeField] private float heightMultiplier = 1f;

    [Header("추가로 미세하게 조절할 오프셋 (UI px 단위, 음수면 아래로)")]
    [SerializeField] private float extraOffset = -50f;

    private Canvas rootCanvas;

    // 각 타겟별 원래 위치를 저장할 Dictionary (Key: 타겟, Value: 위치)
    private Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();

    // 이전 프레임 키보드 높이
    private float currentKeyboardHeight;

    void Awake()
    {
        // 캔버스는 첫 번째 타겟 기준으로 찾거나, 직접 할당된 곳에서 찾음
        if (targets != null && targets.Length > 0)
        {
            rootCanvas = targets[0].GetComponentInParent<Canvas>();
            
            // 모든 타겟의 초기 위치 저장
            foreach (var t in targets)
            {
                if (t != null && !originalPositions.ContainsKey(t))
                {
                    originalPositions.Add(t, t.anchoredPosition);
                }
            }
        }
    }

    void OnEnable()
    {
        // 활성화될 때 모든 타겟 위치 리셋
        ResetAllTargets();
        currentKeyboardHeight = 0f;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        // 타겟이 하나도 없으면 연산하지 않음
        if (targets == null || targets.Length == 0) return;

        float keyboardHeight = GetNativeKeyboardHeight();

        // 1. 키보드가 내려간 경우 (높이 0)
        if (keyboardHeight <= 0f)
        {
            if (!Mathf.Approximately(currentKeyboardHeight, 0f))
            {
                currentKeyboardHeight = 0f;
                ResetAllTargets();
            }
            return;
        }

        // 2. 키보드가 올라와 있고, 높이가 변한 경우
        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;
            ApplyPositionToAllTargets(keyboardHeight);
        }
#endif
    }

    // 모든 타겟을 원래 위치로 되돌리기
    private void ResetAllTargets()
    {
        if (targets == null) return;

        foreach (var t in targets)
        {
            if (t != null && originalPositions.ContainsKey(t))
            {
                t.anchoredPosition = originalPositions[t];
            }
        }
    }

    // 모든 타겟을 키보드 높이만큼 이동시키기
    private void ApplyPositionToAllTargets(float keyboardHeight)
    {
        if (rootCanvas == null) return;

        float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;
        float finalY = uiKeyboardHeight * heightMultiplier + extraOffset;

        foreach (var t in targets)
        {
            if (t != null && originalPositions.ContainsKey(t))
            {
                // 각자의 원래 위치(originalPositions[t])를 기준으로 더해줌
                t.anchoredPosition = originalPositions[t] + new Vector2(0f, finalY);
            }
        }
    }

    private float GetNativeKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f;
#elif UNITY_ANDROID
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            return TouchScreenKeyboard.area.height;

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var rootView = currentActivity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
            var visibleRect = new AndroidJavaObject("android.graphics.Rect");

            rootView.Call("getWindowVisibleDisplayFrame", visibleRect);

            int screenHeight = rootView.Call<int>("getHeight");
            int visibleHeight = visibleRect.Call<int>("height");
            int keyboardHeight = screenHeight - visibleHeight;

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