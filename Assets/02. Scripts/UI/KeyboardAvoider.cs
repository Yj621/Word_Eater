using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모바일 네이티브 키보드 높이에 대응하여 UI 위치를 동적으로 조절하는 컴포넌트
/// </summary>
public class KeyboardAvoider : MonoBehaviour
{
    [Header("UI Configuration")]
    [SerializeField] private RectTransform[] targets;        // 이동 대상 UI 요소들
    [SerializeField] private float heightMultiplier = 0.8f; // 키보드 높이 가중치
    [SerializeField] private float extraOffset = -50f;      // 미세 조정 오프셋

    private Canvas rootCanvas;
    // 초기 위치 보존을 위해 타겟별 앵커 위치를 매핑하여 관리 (데이터 무결성 유지)
    private Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    private float currentKeyboardHeight;

    void Awake()
    {
        if (targets != null && targets.Length > 0)
        {
            // Canvas scaleFactor를 활용한 정확한 픽셀 계산을 위해 부모 캔버스 참조
            rootCanvas = targets[0].GetComponentInParent<Canvas>();
            
            // 해상도 변화나 반복 활성화 시 위치 왜곡을 방지하기 위해 초기 좌표 저장
            foreach (var t in targets)
            {
                if (t != null && !originalPositions.ContainsKey(t))
                    originalPositions.Add(t, t.anchoredPosition);
            }
        }
    }

    void OnEnable()
    {
        ResetAllTargets(); // 오브젝트 활성화 시 위치 초기화
        currentKeyboardHeight = 0f;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (targets == null || targets.Length == 0) return;

        // 매 프레임 네이티브 키보드 영역을 체크하여 변화 감지
        float keyboardHeight = GetNativeKeyboardHeight();

        // 키보드가 닫혔을 때: 즉시 원래 위치로 복구
        if (keyboardHeight <= 0f)
        {
            if (!Mathf.Approximately(currentKeyboardHeight, 0f))
            {
                currentKeyboardHeight = 0f;
                // 원래 위치로 복원
                ResetAllTargets();
            }
            return;
        }

        // 키보드 높이가 변했을 때: UI 위치 재계산 및 적용
        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;
            // 네이티브 픽셀 단위의 키보드 높이를 UI 좌표계로 변환하여 적용하여
            // 타겟 UI들이 키보드 위로 올라가도록 함
            ApplyPositionToAllTargets(keyboardHeight);
        }
#endif
    }

    /// <summary>
    /// 저장된 초기 좌표(originalPositions)를 기반으로 모든 UI를 원복
    /// </summary>
    private void ResetAllTargets()
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t != null && originalPositions.ContainsKey(t))
                t.anchoredPosition = originalPositions[t];
        }
    }

    /// <summary>
    /// 네이티브 픽셀 높이를 Canvas Scale을 고려한 UI 단위 높이로 변환하여 적용
    /// </summary>
    private void ApplyPositionToAllTargets(float keyboardHeight)
    {
        if (rootCanvas == null) return;

        // 네이티브 해상도 높이를 Canvas의 scaleFactor로 나누어 UI 좌표계로 정규화
        float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;
        float finalY = uiKeyboardHeight * heightMultiplier + extraOffset;

        foreach (var t in targets)
        {
            if (t != null && originalPositions.ContainsKey(t))
            {
                // 절대 좌표가 아닌 상대적 이동을 통해 레이아웃 유지
                t.anchoredPosition = originalPositions[t] + new Vector2(0f, finalY);
            }
        }
    }

    /// <summary>
    /// 플랫폼별(Android/iOS) 네이티브 API를 호출하여 키보드가 점유 중인 실제 픽셀 높이를 반환
    /// </summary>
    private float GetNativeKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f;
#elif UNITY_ANDROID
        // Android: TouchScreenKeyboard.area가 부정확할 경우를 대비해 JNI를 통한 DecorView 높이 계산 로직 포함
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

            // 오차 범위를 고려하여 전체 화면의 15% 이상일 때만 키보드가 올라온 것으로 간주
            return (keyboardHeight < screenHeight * 0.15f) ? 0 : keyboardHeight;
        }
#elif UNITY_IOS
        return TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
#else
        return 0f;
#endif
    }
}