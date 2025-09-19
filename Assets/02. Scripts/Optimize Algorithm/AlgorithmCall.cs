using UnityEngine;
using TMPro;    

public class AlgorithmCall : MonoBehaviour
{
    public PythonConnectManager pythonConnectManager;
    public TextMeshProUGUI resultText; // 관련 단어를 표시할 UI Text
    public WordEater.Core.WordEater wordEater; // WordEater 참조 추가

    public void OnShowSimilarWord()
    {
        string answerWord = wordEater != null ? wordEater.CurrentAnswer : ""; // WordEater의 currentAnswer 사용
        StartCoroutine(pythonConnectManager.MostSimilarty(answerWord, 5, (result) =>
        {
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
                int randomIndex = UnityEngine.Random.Range(0, result.Count);
                resultText.text  = $"관련 단어ㅋ : {result[randomIndex]}";
            }
        }));
    }
}
