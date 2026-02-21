using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum KeyType { Single, Double }

public class LongPressKey : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("연결")]
    public KeyBoardManager manager;

    [Header("키 설정")]
    public KeyType keyType = KeyType.Single;
    public int index = 0;
    public float longPressThreshold = 0.35f;
  
    [SerializeField] private Image gaugeFill;  

    bool pressing;
    bool fired;
    Coroutine waitCo;
    Vector2 lastDownPos;
    int lastPointerId;

    public void SetValue(int count, int max)
    {
        if (!gaugeFill) return;

        max = Mathf.Max(1, max);
        count = Mathf.Clamp(count, 0, max);

        float remain01 = count / (float)max;      // 남은 비율
    
        gaugeFill.fillAmount = remain01;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        lastDownPos = eventData.position;
        lastPointerId = eventData.pointerId;
        pressing = true; fired = false;
        if (waitCo != null) StopCoroutine(waitCo);
        waitCo = StartCoroutine(WaitLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 꾹 누르지 않고(fired=false) 손을 뗐다면 -> 단순 클릭으로 간주하고 경고 표시
        if (pressing && !fired)
        {
            if (manager) manager.ShowPushWarning();
        }

        pressing = false;
        if (waitCo != null) StopCoroutine(waitCo);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressing = false;
        if (waitCo != null) StopCoroutine(waitCo);
    }

    IEnumerator WaitLongPress()
    {
        float t = 0f;
        while (pressing && t < longPressThreshold)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (pressing && !fired)
        {
            fired = true;
            if (manager)
            {
                // 포인터 이벤트를 복제하여 매니저에 전달
                PointerEventData fakeEvent = new PointerEventData(EventSystem.current)
                {
                    position = lastDownPos,
                    pointerId = lastPointerId
                };

                if (keyType == KeyType.Single) manager.PressSingle(index, fakeEvent);
                else manager.PressDouble(index, fakeEvent);
            }
        }
    }

    public void RefreshVisuals(int count, int globalMax)
    {
        SetValue(count, globalMax);
    }
}
