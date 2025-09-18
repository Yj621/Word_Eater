using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlgorithmMessage : MonoBehaviour
{
    public TMP_InputField inputField; // 인스펙터에서 할당
    public PythonConnectManager pythonConnectManager; // 인스펙터에서 할당
    public TextMeshProUGUI inputText; // 유사도 결과를 표시할 UI Text
    public TextMeshProUGUI resultText; // 유사도 결과를 표시할 UI Text

    private string answerWord = "정답단어"; // 실제 정답 단어로 변경

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
        StartCoroutine(pythonConnectManager.SimilartyTwoWord(answerWord, userInput, (similarity) =>
        {
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
