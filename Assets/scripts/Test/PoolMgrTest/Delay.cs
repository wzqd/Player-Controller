using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Delay : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        Invoke("Push", 1);
    }

    void Push()
    {
        PoolMgr.Instance.PushObj(gameObject.name, gameObject);
    }

}
