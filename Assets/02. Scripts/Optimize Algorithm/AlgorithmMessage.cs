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
        resultRect.anchoredPosition = hiddenPos;
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
        if (!resultPanel.activeSelf)
        {
            resultPanel.SetActive(true);
            resultRect.anchoredPosition = hiddenPos;
            resultRect.DOAnchorPos(shownPos, duration).SetEase(Ease.OutBack);
        }
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
