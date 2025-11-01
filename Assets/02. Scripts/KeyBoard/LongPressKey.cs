using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public enum KeyType { Single, Double }

public class LongPressKey : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("연결")]
    public KeyBoardManager manager;

    [Header("키 설정")]
    public KeyType keyType = KeyType.Single;
    public int index = 0;
    public float longPressThreshold = 0.35f;

    bool pressing;
    bool fired;
    Coroutine waitCo;
    PointerEventData lastDownEvent;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastDownEvent = eventData;
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
                if (keyType == KeyType.Single) manager.PressSingle(index, lastDownEvent);
                else manager.PressDouble(index, lastDownEvent);
            }
        }
    }
}
