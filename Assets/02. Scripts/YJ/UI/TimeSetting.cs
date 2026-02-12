using UnityEngine;
using TMPro;
using System.Collections;

public class TimeSetting : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText; // call_Second/Text 할당

    private int totalSeconds = 0;
    private Coroutine timerCoroutine;

    void OnEnable()
    {
        // 타이머 초기화
        totalSeconds = 0;

        // Text가 Inspector에서 안 할당돼 있으면 자동으로 자식 Text 가져오기
        if (timerText == null)
            timerText = transform.Find("TimerText").GetComponent<TextMeshProUGUI>();

        // 코루틴 시작
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    void OnDisable()
    {
        // 코루틴 정지
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator TimerCoroutine()
    {
        while (true)
        {
            // 분:초 계산
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            // 00:00 형식으로 텍스트 갱신
            timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            // 1초 대기
            yield return new WaitForSeconds(1f);

            // 초 증가
            totalSeconds++;
        }
    }

}
