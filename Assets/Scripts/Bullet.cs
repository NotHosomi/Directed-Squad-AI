using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    const float SPEED = 15;
    const float OFFSET = 0.7f;
    const float SPREAD = 2;

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);

        Enemy e = collision.collider.GetComponent<Enemy>();
        if (e)
        {
            e.hurt();
        }
    }

    public void init(Vector3 pos, Vector3 target)
    {
        Vector3 dir = target - pos;
        dir = Quaternion.Euler(Random.Range(-SPREAD, SPREAD), Random.Range(-SPREAD, SPREAD), Random.Range(-SPREAD, SPREAD)) * dir;
        dir.Normalize();
        transform.position = pos + dir * OFFSET;
        GetComponent<Rigidbody>().velocity = dir * SPEED;
    }
}
