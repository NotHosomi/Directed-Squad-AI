using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavVisualiser : MonoBehaviour
{
    LineRenderer line;
    NavMeshAgent nav;

    private void Start()
    {
        nav = GetComponent<NavMeshAgent>();

        line = this.gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
    }

    void OnDrawGizmosSelected()
    {
        if (nav == null || nav.path == null)
            return;

        NavMeshPath path = nav.path;

        line.positionCount = path.corners.Length;

        for (int i = 0; i < path.corners.Length; i++)
        {
            line.SetPosition(i, path.corners[i]);
        }

    }
}
