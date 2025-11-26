using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Core;
using WordEater.Systems;
using System.Collections;
using DG.Tweening;

public class AlgorithmMessage : MonoBehaviour
{
    [Header("메세지 패널 관련")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private PythonConnectManager pythonConnectManager;
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private WordEater.Core.WordEater wordEater;
    [SerializeField] private BatterySystem batterySystem;
    [SerializeField] private UILoadingText loading;

    [Header("애니메이션 관련")]
    [SerializeField] private float duration = 0.2f;
    private RectTransform resultRect;
    private Vector2 shownPos;
    private Vector2 hiddenPos;

    void Awake()
    {
        resultRect = resultPanel.GetComponent<RectTransform>();
        shownPos = resultRect.anchoredPosition;
        hiddenPos = shownPos + new Vector2(-Screen.width, 0f);
        resultRect = resultPanel.GetComponent<RectTransform>();

        // 초기화: 패널을 끄고 스케일을 0으로 맞춰둡니다.
        resultRect.localScale = Vector3.zero;
        resultPanel.SetActive(false);
    }

    void Start()
    {
        inputField.onEndEdit.AddListener(UpdateInputText);
    }

    private void UpdateInputText(string value) => inputText.text = value;

    /// <summary>
    /// 결과 패널 표시 애니메이션
    /// </summary>
    private void ShowResultPanel()
    {
        resultPanel.SetActive(true);
        //  시작 상태 설정: 크기 0 (보이지 않음)
        resultRect.localScale = Vector3.zero;

        // 애니메이션 실행
        // DOScale(1f, duration): 크기를 1(원래크기)로 키움
        // SetEase(Ease.OutBack): 목표 크기를 살짝 넘어갔다가 돌아오는 '탱~' 하는 탄성 효과
        resultRect.DOScale(1f, duration).SetEase(Ease.OutBack);
        Handheld.Vibrate(); // 진동

    }

    /// <summary>
    /// 유사도 계산 요청 메서드
    /// </summary>
    public void OnCheckSimilarity()
    {
        // 먼저 배터리 확인
        if (!AlgoGuards.EnsureBattery(batterySystem, ActionType.OptimizeAlgo, resultText))
            return;

        ShowResultPanel();
        loading?.StartAnim("유사도 계산 중");

        string userInput = inputField ? inputField.text : string.Empty;
        string answerWord = wordEater ? wordEater.CurrentAnswer : string.Empty;

        StartCoroutine(pythonConnectManager.SimilartyTwoWord(answerWord, userInput, (similarity) =>
        {
            loading?.StopAnim();

            if (similarity.HasValue)
            {
                if (similarity.Value == 1)
                {
                    resultText.text = "정답!";
                }
                else
                {
                    resultText.text = $"유사도: {similarity.Value.ToString("F2")}";
                }
            }
            else
            {
                resultText.text = "부정확한 단어 또는 요청 실패";
            }
        }));
    }
}
