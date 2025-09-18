using UnityEngine;
using TMPro;    

public class AlgorithmCall : MonoBehaviour
{
    public PythonConnectManager pythonConnectManager;
    public TextMeshProUGUI resultText; // 관련 단어를 표시할 UI Text

    public void OnShowSimilarWord()
    {
        string answerWord = "정답단어"; // 실제 정답 단어로 교체
        StartCoroutine(pythonConnectManager.MostSimilarty(answerWord, 5, (result) =>
        {
            if (result.Count == 1 && result[0] == "요청 실패")
            {
                resultText.text = "Connect Error!";
            }
            else if (result.Count == 1 && result[0] == "부정확한 단어")
            {
                resultText.text = "Uncorrect Error!";
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, result.Count);
                resultText.text  = $"Relevant : {result[randomIndex]}";
            }
        }));
    }
}
