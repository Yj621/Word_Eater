using UnityEngine;
using UnityEngine.UI;

public class DogamitselfInfo : MonoBehaviour
{
    public string sprid;
    public bool isOn = false; // 본 적 있는 애인지 확인하는 함수
    Image img;
    void Awake()
    {
        // 체인 이미지 숨기고
        Transform chain = transform.Find("Lock_Image");
        chain.gameObject.SetActive(false);

        // 검은색으로
        img = GetComponent<Image>();
        img.color = Color.black;
    }

    public void DogamOn() {
        if (isOn) {
            //흰 색으로
            img.color = Color.white;

            // 메테리얼 제거
            img.material = null;
        }
    }

}
