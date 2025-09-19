using UnityEngine;
public class SubmitManager : MonoBehaviour
{
    [Header("연결 스크립트")]
    public PythonConnectManager pythonConnectManager;
    public UIManager uimanager;
    public KeyBoardManager keyboardmanager;
    public WordEater.Core.WordEater wordeater;
    public void OnSubmitButton()
    {
        string word1 = wordeater.returnCurrentEnrty().word; //정답 단어

        if (!keyboardmanager.TryBuildWord(out var word2))
        {
            Debug.Log("TryBuildWord 실패, word2 = " + word2);
            return; //입력한 단어 

        }

        StartCoroutine(pythonConnectManager.SimilartyTwoWord(word1, word2, (result) =>
        {
            if (result.HasValue)
            {
                uimanager.Test_PopUp($"Similarty : {result.Value}");

                wordeater.DoFeedData(word2);
            }
            else
            {
                uimanager.Test_PopUp("Uncorrect Word!");
            }
        }));
    }

    public void OnRelevantButton() {
        string word1 = wordeater.returnCurrentEnrty().word; //정답 단어

        StartCoroutine(pythonConnectManager.MostSimilarty(word1,5, (result) =>
        {
            if (result.Count == 1 && result[0] == "요청 실패")
            {
                uimanager.Test_PopUp("Connect Error!");
            }
            else if (result.Count == 1 && result[0] == "부정확한 단어")
            {
                uimanager.Test_PopUp("Uncorrect Error!");
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, result.Count);
                uimanager.Test_PopUp($"Relevant : {result[randomIndex]}");
            }
        }));

    }
}
