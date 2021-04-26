using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CommandID
{
    NULL = -1,
    ATTACK,
    COVER,
    GOTO,
    RETREAT
}
public enum ConMod // Context Modifier
{
    NONE,
    ENEMY,
    DOOR,
};
struct Command
{
    CommandID id;
    Vector3 coord;
    ConMod context;
};

public class SquadManager : MonoBehaviour
{
    // Squads
    [HideInInspector] public SquadManager _i;
    [SerializeField] int[] squad_sizes;
    [SerializeField] Vector3[] squad_spawns;
    List<Squad> squads;
    int current_squad_id;
    Squad current_squad;

    // interface
    GameObject marker;
    GameObject menu;
    Vector2 menu_center;
    public GameObject hud;

    Vector3 command_point;
    ConMod con_mod = ConMod.NONE;

    const float SCAN_TMR = 0.1f;
    float scan_tmr = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (_i)
        {
            Debug.Log("WARNING: MULTIPLE INSTANCES OF SQUAD MANAGER");
            return;
        }
        _i = this;

        squads = new List<Squad>();
        // max = lowest val
        int max = squad_sizes.Length > squad_spawns.Length ? squad_spawns.Length : squad_sizes.Length;
        for (int i = 0; i < max; ++i)
        {
            squads.Add(new Squad(squad_sizes[i], squad_spawns[i]));
        }
        selectSquad(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("1"))
            selectSquad(0);
        else if (Input.GetKeyDown("2"))
            selectSquad(1);
        else if (Input.GetKeyDown("3"))
            selectSquad(2);

        if (Input.GetMouseButtonDown(1))
        {
            openMenu();
        }
        if (Input.GetMouseButtonUp(1))
        {
            closeMenu();
        }
        if (Input.GetMouseButton(1))
        {
            updateMenu();
        }

        //scan_tmr -= Time.deltaTime;
        //if(scan_tmr < 0)
        //{
        //    scan_tmr = SCAN_TMR;
        //    foreach (Squad s in squads)
        //    {
        //        s.buildSquadSight();
        //    }
        //}
        foreach (Squad s in squads)
        {
            s.buildSquadSight();
        }
    }

    public Squad getCurrentSquad()
    {
        return current_squad;
    }

    void selectSquad(int id)
    {
        if (id >= squads.Count || id < 0)
            return;
        current_squad_id = id;
        current_squad = squads[id];
    }

    void openMenu()
    {
        if(menu != null)
        {
            Destroy(menu);
        }
        // Find cursor aim
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit))
            return;

        // grab target point
        command_point = hit.point;
        command_point.y = 0;
        // con_mod = getContext(hit);

        // spawn menu on hud
        menu = Instantiate(Resources.Load("Interface/Menu") as GameObject);
        menu.transform.SetParent(hud.transform, false);
        menu.GetComponent<RectTransform>().position = Input.mousePosition;
    }
    
    void updateMenu()
    {
        Vector3 diff = Input.mousePosition - menu.GetComponent<RectTransform>().position;

        int quad = (int)checkQuadrant(diff);
        float mult;
        for (int i = 0; i < 4; ++i)
        {
            mult = (i == quad) ? 2 : 1;
            RectTransform rt = menu.transform.GetChild(i).GetComponent<RectTransform>();
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * mult, Time.deltaTime * 3); // change to smoothstep? SS is float only
        }
    }

    void closeMenu()
    {
        Destroy(marker);
        marker = null;
        Destroy(menu);


        Vector3 diff = Input.mousePosition - menu.GetComponent<RectTransform>().position;
        CommandID quad = checkQuadrant(diff);
        if (quad == CommandID.NULL)
            return;

        // Display indicator
        marker = Instantiate(Resources.Load("Interface/Marker") as GameObject);
        marker.transform.position = command_point;
        marker.GetComponent<Renderer>().material.color = colourLookup(quad);
        Destroy(marker, 2);

        current_squad.command(quad, command_point);
    }

    /*
    Menu options
    0 Attack
    1 Cover            // Tighter vision cone? Vision cone using an assload of rays
    2 Advance
    3 Retreat from
      0
    3   1
      2
    */
    CommandID checkQuadrant(Vector3 diff)
    {
        if (diff.magnitude < 40)
        {
            return CommandID.NULL;
        }
        float x = diff.x;
        float y = diff.y;

        if (y > Mathf.Abs(x))
            return CommandID.ATTACK; // 0
        if (-y > Mathf.Abs(x))
            return CommandID.GOTO; // 2
        if (x > 0)
            return CommandID.COVER; // 1
        else
            return CommandID.RETREAT; // 3
    }

    Color colourLookup(CommandID q)
    {
        switch ((int)q)
        {
            case 0:
                return Color.red;
            case 1:
                return Color.blue;
            case 2:
                return Color.green;
            case 3:
                return Color.yellow;
            default:
                return Color.magenta;
        }
    }
}
