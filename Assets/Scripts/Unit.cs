using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    //const float BASE_VIEW_ANGLE = 120;
    const float VIEW_DIST = 25;
    const float ATK_DIST = 7;

    [SerializeField] bool debugging = false;

    public enum UnitState
    {
        RUNNING,
        ALERT,
        ENGAGING,
        SHOOTING
    }
    UnitState state = UnitState.ALERT;

    int health = 100;
    
    //List<Enemy> known_enemies;

    NavMeshAgent agent;
    GameObject bullet; // for prefabbing

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        bullet = Resources.Load("bullet") as GameObject;
    }
    Squad squad;
    public void init(Squad _squad)
    {
        squad = _squad;
    }

    // Update is called once per frame
    void Update()
    {
        switch(state)
        {
            case UnitState.RUNNING: modeRunning();
                break;
            case UnitState.ALERT: modeAdvance();
                break;
            case UnitState.ENGAGING: modeEngage();
                break;
            case UnitState.SHOOTING: modeShoot();
                break;
        }
        if(eng_tmr > 0)
        {
            eng_tmr -= Time.deltaTime;
        }
    }

    public void setState(UnitState s, Vector3 dest)
    {
        switch (state)
        {
            case UnitState.RUNNING:
                setModeRunning(dest);
                break;
            case UnitState.ALERT:
                setModeAdvance(dest);
                break;
            case UnitState.ENGAGING:
                setModeEngage(dest);
                break;
            case UnitState.SHOOTING:
                setModeShoot();
                break;
        }
    }

    public void hurt(int amount)
    {
        health -= amount;
    }

    public bool isAlive()
    {
        return health > 0;
    }

    List<Enemy> squad_sight = new List<Enemy>();
    public void setSquadSight(List<Enemy> enemies)
    {
        squad_sight = enemies;
    }



    // Check if I have LOS to any enemies
    public List<Enemy> my_vision = new List<Enemy>();
    [SerializeField] bool debug_me = false;
    public void scan()
    {
        my_vision = new List<Enemy>();
        Collider[] contacts = Physics.OverlapSphere(transform.position, VIEW_DIST, LayerMask.GetMask("Enemy"));
        LayerMask lm = LayerMask.GetMask("Projectiles");
        lm = ~lm;

        foreach (Collider other in contacts)
        {
            Enemy e = other.GetComponent<Enemy>();
            if(e == null)
            {
                continue;
            }
            // set height offset, to look over mid-height cover
            Vector3 eyes = transform.position;
            eyes.y += 0.3f;
            Vector3 target = e.transform.position;
            target.y += 0.3f;
            
            RaycastHit hit;
            if(Physics.Raycast(eyes, target - eyes, out hit, VIEW_DIST, lm))
            {
                if(hit.collider.GetComponent<Enemy>() == e)
                {
                    Debug.DrawLine(eyes, target, Color.cyan);
                    my_vision.Add(e);
                }
                else
                {
                    Debug.DrawLine(eyes, target, Color.black);
                }
            }
        }
    }

    /****************
     *  BEHAVIOURS  *
     ****************/
    #region behaviours

    Vector3 main_dest;
    Vector3 temp_dest;
    // RUNNING
    public void setModeRunning(Vector3 dest)
    {
        state = UnitState.RUNNING;
        agent.isStopped = false;
        agent.destination = dest;
        main_dest = dest;
    }
    void modeRunning()
    {
        if (agent.remainingDistance < 1)
            setModeAdvance(agent.destination);
    }

    // APPROACH
    public void setModeAdvance(Vector3 dest)
    {
        state = UnitState.ALERT;
        agent.isStopped = false;
        agent.destination = dest;
        main_dest = dest;
    }
    void modeAdvance()
    {
        scan();
        if(my_vision.Count > 0)
        {
            setModeShoot();
        }
        else if (squad_sight.Count > 0)
        {
            setModeEngage();
            eng_tmr = ENG_TMR;
        }
        if(debugging)
            Debug.Log("SS count: " + squad_sight.Count);
    }

    // ENGAGE
    float eng_tmr = 0;
    const float ENG_TMR = 0.5f;
    public void setModeEngage(Vector3 dest)
    {
        Debug.Log("Engaging!");
        state = UnitState.ENGAGING;
        agent.isStopped = false;
        temp_dest = dest;
    }
    public void setModeEngage()
    {
        if(eng_tmr > 0)
        {
            if (debugging)
                Debug.Log("Engage rejected!");
            return;
        }
        if (debugging)
            Debug.Log("Engaging!");
        state = UnitState.ENGAGING;
        
        Enemy closest = null;
        NavMeshPath path;
        float shortest = Mathf.Infinity;
        squad.buildSquadSight();

        foreach (Enemy e in squad_sight)
        {
            if (e == null)
            {
                squad.buildSquadSight();
                break;
            }
        }

        foreach (Enemy e in squad_sight)
        {
            if(e == null)
            {
                Debug.Log("Squadsight STILL outdated!");
                continue;
            }
            path = new NavMeshPath();
            //NavMesh.CalculatePath(agent.transform.position, e.transform.position, NavMesh.AllAreas, path);
            if(!agent.CalculatePath(e.transform.position, path))
            {
                // no valid path found, skip
                continue;
            }
            // sum dist
            float dist = 0;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            // compare dist
            if(dist < shortest)
            {
                shortest = dist;
                closest = e;
            }
        }
        if(closest == null)
        {
            Debug.Log("!!! ERR - NO ENGAGE PATH FOUND !!!");
            eng_tmr = ENG_TMR;
            setModeAdvance(main_dest);
            return;
        }
        temp_dest = closest.transform.position;

        agent.isStopped = false;
        agent.destination = temp_dest;
    }
    void modeEngage()
    {
        scan();
        if(my_vision.Count > 0)
        {
            setModeShoot();
        }
        else if(squad_sight.Count > 0)
        {
            // seek a new target to reengage
            setModeEngage();
        }
        else
        {
            setModeAdvance(main_dest);
        }
    }

    // SHOOT
    public void setModeShoot()
    {
        state = UnitState.SHOOTING;
        agent.isStopped = true;
    }
    void modeShoot()
    {
        scan();

        Enemy e = pickTarget(my_vision);
        if (e != null)
        {
            shootAt(e);
            return;
        }
        if (squad_sight.Count > 0)
        {
            setModeEngage();
        }
        else
        {
            setModeAdvance(main_dest);
        }
    }
    #endregion

    // Combat funcs
    #region combat

    Enemy pickTarget(List<Enemy> enemies)
    {
        if (enemies.Count == 0)
            return null;

        float smallest_dist = float.MaxValue;
        Enemy enemy = null;
        foreach (Enemy e in enemies)
        {
            float dist = (e.transform.position - transform.position).magnitude;

            // TODO raycast
            if (dist < smallest_dist) // && !hit)
                enemy = e;
        }

        return enemy;
    }

    const float FIRE_TMR = 0.2f;
    float fire_tmr = FIRE_TMR;
    void shootAt(Enemy enemy)
    {
        fire_tmr -= Time.deltaTime;
        if (fire_tmr > 0)
            return;
        fire_tmr = FIRE_TMR;

        GameObject b = Instantiate(bullet);
        b.GetComponent<Bullet>().init(transform.position, enemy.transform.position);
    }
    #endregion
}

// TODO: NavMesh obstacle toggling
// should only be active when NavAgent is stopped