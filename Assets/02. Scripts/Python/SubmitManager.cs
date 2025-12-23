using UnityEngine;
using WordEater.Systems;

public class SubmitManager : MonoBehaviour
{
    [Header("연결 스크립트")]
    public PythonConnectManager pythonConnectManager;
    public UIManager uimanager;
    public KeyBoardManager keyboardmanager;
    public WordEater.Core.WordEater wordeater;
    public GameManager gamemanager;

    public void OnSubmitButton()
    {
        // 1. 단어 조합 확인 (비용 없음)
        // [수정] returnCurrentEnrty().word -> CurrentEntry.word
        string word1 = wordeater.CurrentEntry.word;
        if (!keyboardmanager.TryBuildWord(out var word2))
        {
            NoticeManager.Instance.ShowTimed("부정확한 단어", 1.3f);
            return;
        }

        // 2. 배터리 선결제 확인 (WordEater에 복구한 메서드 사용)
        if (!wordeater.TryPayForSubmit())
        {
            uimanager.CloseKeyboard();
            return;
        }

        // 3. 서버 통신
        StartCoroutine(pythonConnectManager.SimilartyTwoWord(word1, word2, (result) =>
        {
            if (result.HasValue)
            {
                if (result.Value == 1)
                {
                    NoticeManager.Instance.ShowSticky("정답!");
                }
                else
                {
                    NoticeManager.Instance.ShowSticky($"유사도 : {(result.Value * 100f).ToString("F0")}%");
                    gamemanager.HistoryLIne += word2 + "," + (result.Value * 100f).ToString("F0") + "%" + "|";
                    gamemanager.UpdateHistoryLineInFile(gamemanager.HistoryLIne);
                }

                wordeater.DoFeedData(word2);
            }
            else
            {
                NoticeManager.Instance.ShowTimed("Uncorrect Word!", 2f);
            }
        }));

        uimanager.CloseKeyboard();
    }

    public void OnRelevantButton()
    {
        // [수정] returnCurrentEnrty().word -> CurrentEntry.word
        string word1 = wordeater.CurrentEntry.word;

        StartCoroutine(pythonConnectManager.MostSimilarty(word1, 5, (result) =>
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