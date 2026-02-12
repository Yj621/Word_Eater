using UnityEngine;
using UnityEngine.UI;
public class BGMChange : MonoBehaviour
{
    public Button FirstBtn;
    public Button SecondBtn;
    public Button ThridBtn;

    public int CurBGM;

    public SoundManager soundmanager;


    public void Start()
    {
        CurBGM = 1;

        FirstBtn.interactable = false;
        SecondBtn.interactable = true;
        ThridBtn.interactable = true;
    }


    public void BGMChanger(int type) {
        if (type == 1)// 첫 번째 버튼
        {
            FirstBtn.interactable = false;
            SecondBtn.interactable = true;
            ThridBtn.interactable = true;

            CurBGM = 1;
            soundmanager.BGMStart(CurBGM);

        }
        else if (type == 2)
        {// 두번째 버튼
            FirstBtn.interactable = true;
            SecondBtn.interactable = false;
            ThridBtn.interactable = true;

            CurBGM = 2;
            soundmanager.BGMStart(CurBGM);
        }
        else {// 세 번째 버튼
            FirstBtn.interactable = true;
            SecondBtn.interactable = true;
            ThridBtn.interactable = false;

            CurBGM = 3;
            soundmanager.BGMStart(CurBGM);
        }
    }
}
