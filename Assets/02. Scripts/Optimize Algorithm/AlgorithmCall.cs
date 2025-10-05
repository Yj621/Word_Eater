using UnityEngine;
using TMPro;
using WordEater.Systems;
using WordEater.Core;
using System.Collections.Generic;
using System.Collections;

public class AlgorithmCall : MonoBehaviour
{
    public PythonConnectManager pythonConnectManager;
    public TextMeshProUGUI resultText; // 관련 단어를 표시할 UI Text
    public WordEater.Core.WordEater wordEater; // WordEater 참조 추가
    public BatterySystem batterySystem;

    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform SecondCall;


    // 로딩 애니메이션 코루틴을 제어하기 위한 변수
    private Coroutine loadingAnimationCoroutine;
    public void OnShowSimilarWord()
    {
        // 만약 이전에 실행 중이던 로딩 애니메이션이 있다면 중지시킵니다.
        if (loadingAnimationCoroutine != null)
        {
            StopCoroutine(loadingAnimationCoroutine);
        }

        // 로딩 애니메이션 코루틴을 시작합니다.
        loadingAnimationCoroutine = StartCoroutine(AnimateLoadingText());

        string answerWord = wordEater != null ? wordEater.CurrentAnswer : "";
        StartCoroutine(pythonConnectManager.MostSimilarty(answerWord, 5, (result) =>
        {
            // Python으로부터 결과를 받으면 로딩 애니메이션을 즉시 중지합니다.
            if (loadingAnimationCoroutine != null)
            {
                StopCoroutine(loadingAnimationCoroutine);
                loadingAnimationCoroutine = null; // 참조를 비워줍니다.
            }

            if (result.Count == 1 && result[0] == "요청 실패")
            {
                resultText.text = "Connect Error!";
            }
            else if (result.Count == 1 && result[0] == "부정확한 단어")
            {
                resultText.text = "부정확한 단어";
            }
            else
            {
                // 배터리 소모 시도
                if (batterySystem != null && !batterySystem.TryConsume(ActionType.OptimizeAlgo))
                {
                    // 배터리가 부족하면 함수를 종료 (OnActionBlockedLowBattery 이벤트는 BatterySystem에서 이미 호출됨)
                    // "배터리 부족" 텍스트를 표시
                    resultText.text = "배터리가 부족합니다.";
                    return;
                }

                // 배터리 소모에 성공한 경우에만 텍스트 업데이트
                int randomIndex = UnityEngine.Random.Range(0, result.Count);
                resultText.text = $"관련 단어 : {result[randomIndex]}";

            }
        }));

    }

    /// <summary>
    /// "유사성 계산 중..." 텍스트 애니메이션을 처리하는 코루틴
    /// </summary>
    private IEnumerator AnimateLoadingText()
    {
        string baseText = "유사성 계산 중";
        int dotCount = 1;

        // 이 코루틴이 외부에서 중지되기 전까지 무한 반복
        while (true)
        {
            // 점(.)의 개수를 1개, 2개, 3개로 순환
            string dots = new string('.', dotCount);
            resultText.text = baseText + dots;

            dotCount++;
            if (dotCount > 3)
            {
                dotCount = 1;
            }

            // 0.4초 대기 후 다음 프레임으로 넘어감 (속도 조절 가능)
            yield return new WaitForSeconds(0.4f);
        }
    }

}
