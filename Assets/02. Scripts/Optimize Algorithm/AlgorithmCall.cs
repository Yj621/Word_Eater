using UnityEngine;
using TMPro;
using WordEater.Systems;
using WordEater.Core;
using System.Collections;
using System.Collections.Generic;

public class AlgorithmCall : MonoBehaviour
{
    [Header("전화 패널 관련")]
    [SerializeField] private PythonConnectManager pythonConnectManager;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private WordEater.Core.WordEater wordEater;
    [SerializeField] private BatterySystem batterySystem;
    [SerializeField] private UILoadingText loading; //공용 로딩 컴포넌트
    [SerializeField] private GameManager gameamnager;
    [SerializeField] private FileManager filemanager;
    /// <summary>
    /// 관련 단어 찾기 요청 메서드
    /// </summary>
    public void OnShowSimilarWord()
    {
        GameManager.Instance.StopRingingEffect();
        // 배터리 부족 시 네트워크 호출 자체를 막음
        if (!AlgoGuards.EnsureBattery(batterySystem, ActionType.OptimizeAlgoCall, resultText))
            return;

        if (gameamnager.RelevantResult.Count == 0) // 사실 0 일 경우는 없긴 해
        {
            loading?.StartAnim("관련 단어 찾는 중");

            string answerWord = wordEater ? wordEater.Answer : string.Empty;

            StartCoroutine(pythonConnectManager.MostSimilarty(answerWord, 5, (result) =>
            {
                loading?.StopAnim();

                if (result == null || result.Count == 0)
                {
                    resultText.text = "결과 없음";
                    return;
                }

                if (result.Count == 1 && result[0] == "요청 실패")
                {
                    resultText.text = "Connect Error!";
                    return;
                }
                if (result.Count == 1 && result[0] == "부정확한 단어")
                {
                    resultText.text = "부정확한 단어";
                    return;
                }
                gameamnager.RelevantResult = new List<string>(result);
                filemanager.SaveRelevant(gameamnager.RelevantResult);


                int idx = Random.Range(0, result.Count);
                resultText.text = $"관련 단어 : {result[idx]}";
            }));
        }
        else {
            int idx = Random.Range(0, gameamnager.RelevantResult.Count);
            resultText.text = $"관련 단어 : {gameamnager.RelevantResult[idx]}";
        }
    }
}
