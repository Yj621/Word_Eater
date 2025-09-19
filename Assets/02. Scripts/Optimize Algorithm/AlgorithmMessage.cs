using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Core;

public class AlgorithmMessage : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField; // 인스펙터에서 할당
    [SerializeField] private PythonConnectManager pythonConnectManager; // 인스펙터에서 할당
    [SerializeField] private TextMeshProUGUI inputText; // 현재 입력한 단어를 표시할 UI Text
    [SerializeField] private TextMeshProUGUI resultText; // 유사도 결과를 표시할 UI Text
    [SerializeField] private GameObject resultPanel; // 결과 패널
    public WordEater.Core.WordEater wordEater;

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
        string userInput = inputField.text;
        string answerWord = wordEater != null ? wordEater.CurrentAnswer : "";
        StartCoroutine(pythonConnectManager.SimilartyTwoWord(answerWord, userInput, (similarity) =>
        {
            resultPanel.SetActive(true);
            if (similarity.HasValue)
            {
                resultText.text = $"유사도: {similarity.Value}";
            }
            else
            {
                resultText.text = "부정확한 단어 또는 요청 실패";
            }
        }));
    }
}
