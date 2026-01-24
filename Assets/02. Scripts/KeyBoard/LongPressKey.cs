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
  
[SerializeField] private Image gaugeFill;   // Type: Filled 인 이미지
//[SerializeField] private bool showUsedRatio = true; // true=쓴 비율, false=남은 비율

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
                // [Fix] create new PointerEventData with saved values
                PointerEventData fakeEvent = new PointerEventData(EventSystem.current)
                {
                    position = lastDownPos,
                    pointerId = lastPointerId
                };

                if (keyType == KeyType.Single) manager.PressSingle(index, fakeEvent);
                else manager.PressDouble(index, fakeEvent);

                //SoundManager.Instance.SFXStart(SoundManager.SFXType.jaMoDrag);
            }
        }
    }

    public void RefreshVisuals(int count, int globalMax)
    {
        SetValue(count, globalMax);
    }
}
