using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Listener : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventMgr.Instance.AddEventListener("左键按下", Incident);
    }

    // Update is called once per frame
    void Update()
    {
    
    }

    public void Incident()
    {
        Debug.Log("123123");
    }
}
