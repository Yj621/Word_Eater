using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubmitManager : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject blockPanel;   // 터치 방지용 패널
    public GameObject resultPanel;  // 결과 보여줄 패널
    public TextMeshProUGUI resultText;         // 결과 텍스트 UI
    public Button submitButton;     // 제출 버튼
    public Button CloseBtn;         // 닫기 버튼

    [Header("연결 스크립트")]
    public PythonConnectManager pythonConnectManager;

    void Start()
    {
        // 버튼 클릭 시 OnSubmitButton 호출
        submitButton.onClick.AddListener(OnSubmitButton);
        CloseBtn.onClick.AddListener(ClosePanel);
    }


    private void OnSubmitButton()
    {
        string word1 = "바나나";
        string word2 = "사과";

        blockPanel.SetActive(true);

        StartCoroutine(pythonConnectManager.SimilartyTwoWord(word1, word2, (result) =>
        {
            blockPanel.SetActive(false);
            resultPanel.SetActive(true);

            if (result.HasValue)
            {
                resultText.text = $"유사도: {result.Value}";
            }
            else
            {
                resultText.text = "부정확한 단어 입력";
            }
        }));
    }

    private void ClosePanel() {
        resultPanel.SetActive(false);
    }
}
