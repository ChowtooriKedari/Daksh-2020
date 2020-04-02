using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class CS : MonoBehaviour
{
    float curtime = 0f;
    float stime = 10f;
    public Text cdownText;
    public float text2 = 0f;
    void Start()
    {
        curtime = stime;
      

        //print(curtime);
      //  print(stime);
    }
    void Update()
    {
        curtime -= 1 * Time.deltaTime;
        cdownText.text = curtime.ToString("0");
        print(curtime);
        text2 = curtime;
        if (curtime <= 0)
        {
            curtime = 0;
             SceneManager.LoadScene("Moon");
        }
    }


}
