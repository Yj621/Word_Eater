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
            NoticeManager.Instance.ShowTimed("부정확한 단어", 1.3f);
            return; //입력한 단어 

        }

        StartCoroutine(pythonConnectManager.SimilartyTwoWord(word1, word2, (result) =>
        {
            if (result.HasValue)
            {
                NoticeManager.Instance.ShowSticky($"유사도 : {result.Value}");

                wordeater.DoFeedData(word2);
            }
            else
            {
                NoticeManager.Instance.ShowTimed("Uncorrect Word!", 2f);
            }
        }));
    }

    public void OnRelevantButton() {
        string word1 = wordeater.returnCurrentEnrty().word; //정답 단어

        StartCoroutine(pythonConnectManager.MostSimilarty(word1,5, (result) =>
        {
            if (result.Count == 1 && result[0] == "요청 실패")
            {
                NoticeManager.Instance.ShowTimed("Connect Error!", 3f);
            }
            else if (result.Count == 1 && result[0] == "부정확한 단어")
            {
                NoticeManager.Instance.ShowTimed("Uncorrect Error!", 3f);
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, result.Count);
                NoticeManager.Instance.ShowSticky($"Relevant : {result[randomIndex]}");
            }
        }));

    }
}
