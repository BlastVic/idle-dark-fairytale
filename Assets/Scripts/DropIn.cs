using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropIn : MonoBehaviour
{
    public float howHigh = 10;
    public float origY = 0;
    public float moveSpeed = .1f;
    GameObject body, lifebar;
    float origScale;
    Vector3 origScaleLifebar;
    private void OnEnable()
    {
        lifebar = transform.GetChild(0).gameObject;
        body = transform.GetChild(1).gameObject;
        origScaleLifebar = lifebar.transform.localScale;

        lifebar.transform.localScale = Vector3.zero;
        origY = body.transform.position.y;
        origScale = body.transform.localScale.x;

        body.transform.localScale= new Vector3(body.transform.localScale.x * .8f, body.transform.localScale.y, body.transform.localScale.z);
        body.transform.position += new Vector3(0,howHigh,0);
    }

   public bool isFalling = true;
    private void FixedUpdate()
    {
        if (isFalling)
        {
            body.transform.position -= new Vector3(0, moveSpeed, 0);
        }
        else { return; }
        if (body.transform.position.y <= origY)
        {
            isFalling = false;
            body.transform.position.Set(body.transform.position.x, origY, body.transform.position.z);
            body.transform.localScale.Set(origScale, body.transform.localScale.y, body.transform.localScale.z);
            lifebar.transform.localScale = origScaleLifebar;
            Enemy enemy = GetComponent<Enemy>();

            bool instantAttack = false;
            if (Random.Range(0, 100) < 50)
            {
                instantAttack = true;
            }
            enemy.StartAttacking(instantAttack);

        }

    }


}
