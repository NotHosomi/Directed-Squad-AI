using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // for Union

// Interface class for accessing Units and storing squad-wide data
public class Squad
{
    List<Unit> members;

    enum SquadState
    {
        IDLE,
        ASSAULTING,
        COVERING,
        ADVANCING,
        RETREATING,
        // TODO: add more context-wise states
        // HFSM Overhaul
        RUNNING,
        O_ADVANCE,
        O_SEEK_COVER,
        O_WAIT,
        O_COMBAT,
        A_ADVANCE,
        A_COMBAT
        // Simplification overhaul:
        // RUNNING
        // ADVANCING
        // COMBAT
    }
    SquadState state = SquadState.O_WAIT;

    public Squad(int size, Vector3 start_pos)
    {
        members = new List<Unit>();
        for (int i = 0; i < size; ++i)
        {
            Unit u = GameObject.Instantiate(Resources.Load("unit") as GameObject, start_pos, Quaternion.identity).GetComponent<Unit>();
            u.init(this);
            members.Add(u);
        }
        Vector3[] dests = new Vector3[members.Count];
        dests = buildDestinations(Vector3.zero);
        for (int i = 0; i < size; ++i)
        {
            members[i].transform.position += dests[i];
        }
    }

    public bool isAlive()
    {
        bool alive = false;
        foreach(Unit member in members)
        {
            if (member.isAlive())
                alive = true;
        }
        return alive;
    }

    public void command(CommandID command_id, Vector3 coord, ConMod context = ConMod.NONE)
    {
        switch(command_id)
        {
            default:
                break;
            case CommandID.GOTO:
                moveRequest(coord);
                break;
            case CommandID.ATTACK:
                attackRequest(coord);
                break;
            case CommandID.COVER:
                coverRequest(coord);
                break;
            case CommandID.RETREAT:
                Debug.Log("This order is not yet implemented");
                break;
        }
    }

    // generate SquadSight
    public void buildSquadSight()
    {
        // we ignore enemies when running
        if (state == SquadState.RUNNING)
        {
            foreach (Unit m in members)
            {
                m.setSquadSight(new List<Enemy>());
            }
            return;
        }

        List<Enemy> enemies = new List<Enemy>();
        // find what each unit can see
        foreach (Unit m in members)
        {
            enemies = enemies.Union<Enemy>(m.my_vision).ToList<Enemy>();
        }
        // distribute info
        foreach (Unit m in members)
        {
            m.setSquadSight(enemies);
        }
    }

    // -------------
    //   INTERNALS
    // -------------
    void moveRequest(Vector3 destination)
    {
        // overwrite squadsight
        foreach (Unit m in members)
        {
            m.setSquadSight(new List<Enemy>());
        }
        state = SquadState.RUNNING;

        Vector3[] dests = new Vector3[members.Count];
        dests = buildDestinations(destination);
        for (int i = 0; i < members.Count; ++i)
        {
            members[i].setModeRunning(dests[i]);
        }
    }

    void attackRequest(Vector3 destination)
    {
        Vector3[] dests = new Vector3[members.Count];
        dests = buildDestinations(destination);
        for (int i = 0; i < members.Count; ++i)
        {
            members[i].setModeAdvance(dests[i]);
        }
    }

    void coverRequest(Vector3 destination)
    {
        Vector3[] dests = new Vector3[members.Count];
        dests = findOverwatchDests(destination);

        for (int i = 0; i < members.Count; ++i)
        {
            members[i].setModeAdvance(dests[i]);
        }
    }

    Vector3[] buildDestinations(Vector3 center)
    {
        Vector3[] dests = new Vector3[members.Count];
        dests[0] = center;
        if (members.Count == 1)
        {
            return dests;
        }
        Vector3 offset = Vector3.one;
        float rot_increment = 360.0f / members.Count - 1;
        // todo add raycasts
        for(int i = 1; i < members.Count; ++i)
        {
            dests[i] = center + Quaternion.Euler(0, rot_increment * i - 1, 0) * offset;
        }

        return dests;
    }

    // TODO: generate dests based on cover, not by offsets
    const int SWEEP_RESOLUTION = 360;
    const float SWEEP_DIST = 25;
    const float SWEEP_Y = 1.3f;
    Vector3[] findOverwatchDests(Vector3 origin)
    {
        List<(Vector3, float)> circumfrence = new List<(Vector3, float)>();
        
        origin.y = SWEEP_Y;
        float step = 360.0f / SWEEP_RESOLUTION;
        RaycastHit hit = new RaycastHit();
        LayerMask lm = LayerMask.GetMask("Units", "Enemy", "Projectiles");
        lm = ~lm;
        for(float rot = 0; rot < SWEEP_RESOLUTION; rot += step)
        {
            if (Physics.Raycast(origin, Quaternion.Euler(0, rot, 0) * new Vector3(0, 0, 1), out hit, SWEEP_DIST, lm))
            {
                (Vector3, float) point = (hit.point - origin, Vector3.Distance(origin, hit.point));
                circumfrence.Add(point);
                Debug.DrawLine(origin, hit.point, Color.green, 3);
            }
            else
            {
                (Vector3, float) point = (Quaternion.Euler(0, rot, 0) * new Vector3(0, 0, SWEEP_DIST), SWEEP_DIST);
                circumfrence.Add(point);
                Debug.DrawLine(origin, origin + Quaternion.Euler(0, rot, 0) * new Vector3(0, 0, SWEEP_DIST), Color.yellow, 3);
            }
        }
        circumfrence.Add(circumfrence[0]);

        Vector3[] dests = buildDestinations(origin);
        int cover_points = 0;
        for(int i = 1; i < SWEEP_RESOLUTION; ++i)
        {
            float diff = circumfrence[i].Item2 - circumfrence[i - 1].Item2;
            Vector3 offset;
            float limit;
            if (diff > 1)
            {
                //
                offset = circumfrence[i - 1].Item1;
                limit = circumfrence[i].Item1.magnitude;
            }
            else if(diff < -1)
            {
                offset = circumfrence[i].Item1;
                limit = circumfrence[i - 1].Item1.magnitude;
            }
            else
            {
                continue;
            }
            
            // find the first open position behind 
            while(offset.magnitude < limit)
            {
                Debug.DrawLine(origin + offset, origin + offset + new Vector3(0, -1, 0), Color.green, 6);
                if (Physics.OverlapSphere(origin + offset, 0.5f, lm).Length == 0)
                {
                    Debug.DrawLine(origin + offset, origin + offset + new Vector3(0, 1, 0), Color.red, 6);
                    Vector3 pos = origin + offset;
                    pos.y = 0;
                    dests[cover_points] = pos;
                    cover_points++;
                    break;
                }               
                offset += offset.normalized * 0.5f;
            }
            if(cover_points == members.Count)
            {
                break;
            }
        }
        Debug.Log("Spots found: " + cover_points);
        return dests;
    }

    void OnDrawGizmos()
    {

    }
}
