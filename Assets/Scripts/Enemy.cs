using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject path;
    Vector3[] path_corners;
    int dest_index = 0;
    int health = 10;

    NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (path == null)
        {
            agent.enabled = false;
            GetComponent<NavMeshObstacle>().enabled = true;
            return;
        }

        path_corners = new Vector3[path.transform.childCount + 1];
        path_corners[0] = path.transform.position;
        for(int i = 1; i < path_corners.Length; ++i)
        {
            path_corners[i] = path.transform.GetChild(i-1).position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(agent.enabled)
        {
            float dist = agent.remainingDistance;
            if (agent.remainingDistance == 0)
            {
                dest_index++;
                dest_index %= path_corners.Length;
                agent.destination = path_corners[dest_index];
            }
        }
    }

    public void hurt()
    {
        --health;
        if (health < 0)
            Destroy(gameObject);
    }
}
