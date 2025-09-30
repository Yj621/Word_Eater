using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Core;
using WordEater.Systems;
using System.Collections;

public class AlgorithmMessage : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField; // 인스펙터에서 할당
    [SerializeField] private PythonConnectManager pythonConnectManager; // 인스펙터에서 할당
    [SerializeField] private TextMeshProUGUI inputText; // 현재 입력한 단어를 표시할 UI Text
    [SerializeField] private TextMeshProUGUI resultText; // 유사도 결과를 표시할 UI Text
    [SerializeField] private GameObject resultPanel; // 결과 패널
    public WordEater.Core.WordEater wordEater;
    public BatterySystem batterySystem;

    // 로딩 애니메이션 코루틴을 제어하기 위한 변수
    private Coroutine loadingAnimationCoroutine;

    void Start()
    {
        inputField.onEndEdit.AddListener(UpdateInputText);
    }

    private void UpdateInputText(string value)
    {
        inputText.text = value;
    }

    public void OnCheckSimilarity()
    {
        // 만약 이전에 실행 중이던 로딩 애니메이션이 있다면 중지
        if (loadingAnimationCoroutine != null)
        {
            StopCoroutine(loadingAnimationCoroutine);
        }

        // 결과 패널을 먼저 활성화하고 로딩 애니메이션 시작
        resultPanel.SetActive(true);
        loadingAnimationCoroutine = StartCoroutine(AnimateLoadingText());

        string userInput = inputField.text;
        string answerWord = wordEater != null ? wordEater.CurrentAnswer : "";
        StartCoroutine(pythonConnectManager.SimilartyTwoWord(answerWord, userInput, (similarity) =>
        {
            // 결과를 받으면 로딩 애니메이션 즉시 중지
            if (loadingAnimationCoroutine != null)
            {
                StopCoroutine(loadingAnimationCoroutine);
                loadingAnimationCoroutine = null; // 참조 정리
            }
            if (similarity.HasValue)
            { 
                // 배터리 소모 시도
                if (batterySystem != null && !batterySystem.TryConsume(ActionType.OptimizeAlgo))
                {
                    // 배터리가 부족하면 함수를 종료
                    resultText.text = "배터리가 부족합니다.";
                    return;
                }

                // 배터리 소모에 성공한 경우에만 텍스트 업데이트
                resultText.text = $"유사도: {similarity.Value}";
            }
            else
            {
                resultText.text = "부정확한 단어 또는 요청 실패";
            }
        }));
    }
    /// <summary>
    /// "관련 단어 찾는 중..." 텍스트 애니메이션을 처리하는 코루틴
    /// </summary>
    private IEnumerator AnimateLoadingText()
    {
        string baseText = "관련 단어 찾는 중";
        int dotCount = 1;

        while (true)
        {
            string dots = new string('.', dotCount);
            resultText.text = baseText + dots;

            dotCount++;
            if (dotCount > 3)
            {
                dotCount = 1;
            }

            yield return new WaitForSeconds(0.4f);
        }
    }
}
