using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public int hp = 10;

    public GameObject destroyEffect;
    // Start is called before the first frame update
    public void SetDamage(int value)
    {
        hp -= value;
        // Debug.Log("hp = "+hp.ToString());
        if (hp <= 0)
        {
            if (destroyEffect)
            {
                Instantiate(destroyEffect, transform.position, quaternion.identity);
            }
            Destroy(gameObject,0.1f);
        }
    }
    
    
}
