using UnityEngine;
using WordEater.Systems;
using System.Collections.Generic;

public class SubmitManager : MonoBehaviour
{
    [Header("연결 스크립트")]
    public PythonConnectManager pythonConnectManager;
    public UIManager uimanager;
    public KeyBoardManager keyboardmanager;
    public WordEater.Core.WordEater wordeater;
    public GameManager gamemanager;
    public FileManager filemanager;

    public void OnSubmitButton()
    {
        // 1. 단어 조합 확인 (비용 없음 - 먼저 체크)
        string word1 = wordeater.CurrentEntry.word;
        if (!keyboardmanager.TryBuildWord(out var word2))
        {
            NoticeManager.Instance.ShowTimed("부정확한 단어", 1.3f);
            return;
        }

        // 2. 배터리 결제 시도 (콜백 방식으로 변경됨)
        // [중요] "성공 시 실행할 로직"을 람다 함수 () => { ... } 안에 넣습니다.
        wordeater.TryPayForSubmit(() =>
        {
            // === 여기서부터는 배터리 결제가 성공(또는 강제 제출)했을 때만 실행됩니다 ===
            uimanager.InteractPanel.SetActive(false);
            uimanager.CloseKeyboard();
            return;
        });

            // 키보드 알파벳 소모 확정
            keyboardmanager.ConfirmUse();

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

            // 성공적으로 제출 절차가 시작되었으므로 키보드를 닫습니다.
            uimanager.InteractPanel.SetActive(false);
            uimanager.CloseKeyboard();
        }

        // 참고: 만약 팝업에서 'No(포기)'를 누르면 위 { } 안의 코드는 실행되지 않고,
        // 키보드도 닫히지 않은 상태로 유지됩니다. (유저가 다시 수정할 수 있도록)
    

    public void OnRelevantButton()
    {
        // [수정] returnCurrentEnrty().word -> CurrentEntry.word
        string word1 = wordeater.CurrentEntry.word;

        if (gamemanager.RelevantResult.Count == 0)
        {

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
                    gamemanager.RelevantResult = new List<string>(result);
                    filemanager.SaveRelevant(gamemanager.RelevantResult);


                    int randomIndex = UnityEngine.Random.Range(0, result.Count);
                    NoticeManager.Instance.ShowSticky($"힌트 단어 : {result[randomIndex]}");
                }
            }));
        }
        else {
            int randomIndex = UnityEngine.Random.Range(0, gamemanager.RelevantResult.Count);
            NoticeManager.Instance.ShowSticky($"힌트 단어 : {gamemanager.RelevantResult[randomIndex]}");
        }
    }
}