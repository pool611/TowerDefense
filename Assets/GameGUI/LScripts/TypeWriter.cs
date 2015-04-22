using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TypeWriter : MonoBehaviour {

    public Text DataText;
    public AudioSource audioSource;

    private float time = (float)0.2;

    private string mywords = "大家好,我是打印机！";

	// Use this for initialization
	void Start () {

        DataText.text = "";
        audioSource.Play();
        StartCoroutine("Example");
        
        

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    IEnumerator Example()
    {
        print(Time.time+"   Example开始");

        for (int i = 0; i < mywords.Length;i++ )
        {

            
            DataText.text += mywords[i];
            yield return new WaitForSeconds(time);

        }

        audioSource.Stop();
        print(Time.time+"   Example结束");
    }
}
