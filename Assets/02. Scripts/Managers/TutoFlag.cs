using UnityEngine;

public class TutoFlag : MonoBehaviour
{
    int tutorialFlags;

    public GameObject CallTuto;
    public GameObject MsgTuto;
    public GameObject LockTuto;
    public GameObject SubMitTuto;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tutorialFlags = PlayerPrefs.GetInt("TutorialFlags", 0);

        // 값 초기화
        /*
        tutorialFlags = 0;
        PlayerPrefs.SetInt("TutorialFlags", 0);
        PlayerPrefs.Save();
        */
    }

    public void CheckTuto(int type) {

        switch (type) {

            case 0: // 전화
                if ((tutorialFlags & (1 << 0)) == 0)
                {
                    infoPanel CallTutoInfo = CallTuto.GetComponent<infoPanel>();

                    CallTutoInfo.showExplainPanel();
                    CallTutoInfo.SetContent(0);

                    tutorialFlags |= 1 << 0;
                    PlayerPrefs.SetInt("TutorialFlags", tutorialFlags);
                    PlayerPrefs.Save();
                }
                break;

            case 1://메세지
                if ((tutorialFlags & (1 << 1)) == 0)
                {
                    infoPanel MsgTutoInfo = MsgTuto.GetComponent<infoPanel>();

                    MsgTutoInfo.showExplainPanel();
                    MsgTutoInfo.SetContent(1);

                    tutorialFlags |= 1 << 1;
                    PlayerPrefs.SetInt("TutorialFlags", tutorialFlags);
                    PlayerPrefs.Save();
                }
                break;

            case 2: // 잠금
                if ((tutorialFlags & (1 << 2)) == 0)
                {
                    infoPanel LockTutoInfo = LockTuto.GetComponent<infoPanel>();

                    LockTutoInfo.showExplainPanel();
                    LockTutoInfo.SetContent(2);

                    tutorialFlags |= 1 << 2;
                    PlayerPrefs.SetInt("TutorialFlags", tutorialFlags);
                    PlayerPrefs.Save();
                }
                break;

            case 3: // 제출
                if ((tutorialFlags & (1 << 3)) == 0)
                {
                    infoPanel SubTutoInfo = SubMitTuto.GetComponent<infoPanel>();

                    SubTutoInfo.showExplainPanel();
                    SubTutoInfo.SetContent(3);

                    tutorialFlags |= 1 << 3;
                    PlayerPrefs.SetInt("TutorialFlags", tutorialFlags);
                    PlayerPrefs.Save();
                }
                break;
        }
    }
}
