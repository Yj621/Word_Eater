using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private WordEater.Core.WordEater wordeater;
    [SerializeField] private GameObject touchblockPanel;
    void Start()
    {
        //시작 하면 첫 정답 단어 선정
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
    }

    //type 에 따라 게임이 끝났을 때 행동 변화.
    //type이=1 인 경우 <<< 게임 오버.
    //type이=2 인 경우 <<< 게임 클리어
    public void EndingController(int type) {
        //게임 오버
        if (type == 1) {
            // 재시작 하는 동안(애니메이션이 나올 예정이라) 일단 터지 방지
            touchblockPanel.SetActive(true);
            NoticeManager.Instance.ShowTimed("게임 오버!", 3f);
            //재시작
            StartCoroutine(RestartWithDelay(3f));
        }
        //게임 클리어
        else if (type == 2) {
            // 재시작 하는 동안(애니메이션이 나올 예정이라) 일단 터지 방지
            touchblockPanel.SetActive(true);
            NoticeManager.Instance.ShowTimed("게임 클리어!", 3f);
            //도감 등록



            //재시작
            StartCoroutine(RestartWithDelay(3f));
        }
    
    }

    //일단은 N초뒤 시작이지만, 나중에 애니메이션을 넣으면 애니메이션 쪽에서 restart함수 실행으로 변경
    private IEnumerator RestartWithDelay(float delay)
    {


        yield return new WaitForSeconds(delay);
        Restart();
    }

    private void Restart() {
        touchblockPanel.SetActive(false);
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
    }
}
