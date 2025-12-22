using UnityEngine;
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
        string word1 = wordeater.returnCurrentEnrty().word;
        if (!keyboardmanager.TryBuildWord(out var word2))
        {
            Debug.Log("TryBuildWord 실패, word2 = " + word2);
            NoticeManager.Instance.ShowTimed("부정확한 단어", 1.3f);
            return;
        }

        // [추가] 2. 배터리 선결제 확인
        // 여기서 배터리가 부족하면(false), 사망 로직이 WordEater 내부에서 돌고 여기서는 즉시 리턴합니다.
        // 따라서 서버 통신도 안 하고, 점수도 안 오릅니다.
        if (!wordeater.TryPayForSubmit())
        {
            uimanager.CloseKeyboard();
            return;
        }

        // 3. 서버 통신 (배터리 지불에 성공했을 때만 실행)
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

                // 이미 위에서 배터리를 냈으므로, 여기서는 그냥 로직만 태움
                wordeater.DoFeedData(word2);
            }
            else
            {
                NoticeManager.Instance.ShowTimed("Uncorrect Word!", 2f);
            }
        }));

        uimanager.CloseKeyboard();
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
