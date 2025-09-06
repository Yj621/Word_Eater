using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MiniGame1_Manager : MonoBehaviour
{
    [Header("UI OBJS")]
    public RectTransform player;
    public RectTransform goal;
    public Image sign;
    public TMP_Text timerText;

    public TouchPanel touchPanel;

    [Header("Setting")]
    public float speed = 100f;
    public int TimeLimit = 30;

    int RemainTime;
    bool SignColor = true; //True << 초록, False << 빨강
    bool Move_Possible = true;
    bool GameEndCheck = true; //FALSE << 게임 오버

    void Start()
    {
        RemainTime = TimeLimit;
        if (timerText != null)
            timerText.text = RemainTime.ToString();


        Move_Possible = true;
        GameEndCheck = true;
        SignColor = true;

        StartCoroutine(SignalCoroutine());
        StartCoroutine(TimerCoroutine());
    }

    void Update()
    {
        // 빨간 신호일때 건넘 << 게임오버
        if (GameEndCheck && !SignColor && touchPanel.isPressed) {

            GameOver();
        }

        //움직이기
        if (touchPanel.isPressed && Move_Possible)
        {
            player.anchoredPosition += Vector2.up * speed * Time.deltaTime;
        }

        //게임 클리어
        if (GameEndCheck)
        {
            GameClear();
        }
    }

    void GameClear()
    {
        if (player.anchoredPosition.y >= goal.anchoredPosition.y)
        {
            Move_Possible = false;
            GameEndCheck = false;

            //이후 성공 기능 추가
            Debug.Log("Success!");
        }
    }

    void GameOver() {
        Move_Possible = false;
        GameEndCheck = false;

        // 이후 실패 함수 추가
        Debug.Log("Fail!");
    }


    //신호 코루틴
    IEnumerator SignalCoroutine()
    {
        bool isGreen = true;

        while (GameEndCheck) {
            float waitTime = Random.Range(1f, 3f); //Random time Between 1~3f

            if (isGreen)
            {
                sign.color = Color.green;
                SignColor = true;
            }
            if (!isGreen) {
                sign.color = Color.red;
                SignColor = false;
            }

            yield return new WaitForSeconds(waitTime);
            isGreen = !isGreen;
        }
    }


    //타이머 코루틴
    IEnumerator TimerCoroutine() {
        while (GameEndCheck) {
            yield return new WaitForSeconds(1f);
            RemainTime--;
            if (GameEndCheck && timerText != null)
                timerText.text = RemainTime.ToString();

            //Time Out
            if (RemainTime <= 0) {
                GameOver();
            }
        }
    }
}
