using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{

    private int damage; //传一个伤害给造成伤害事件
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
        EventMgr.Instance.EventTrigger("PlayerDealDamage", damage); //触发造成伤害 具体扣血看被打的一方
    }
}
