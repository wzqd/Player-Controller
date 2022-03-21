using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timing : MonoBehaviour
{
    private Coroutine myCoroutine;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            myCoroutine = TimeMgr.Instance.StartTimer(1, () => { Debug.Log("yimiaojingguo"); });
        }

        if (Input.GetMouseButtonDown(1))
        {
            TimeMgr.Instance.StopTimer(myCoroutine);
        }
        

    }


}
