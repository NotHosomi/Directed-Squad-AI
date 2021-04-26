using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    [SerializeField] const float CAM_HEIGHT = 20;
    [SerializeField] const float CAM_PITCH = 80;
    static float CAM_SPEED = 15;
    static float CAM_X_BOUND = 20;
    static float CAM_Z_BOUND = 50;
    static float EDGEPAN_SIZE = 10;
    Vector3 grip_pos;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 pos = transform.position;
        pos.y = CAM_HEIGHT;
        transform.position = pos;
        
        Quaternion rot = Quaternion.Euler(CAM_PITCH, 0, 0);
        transform.rotation = rot;
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 offset = new Vector3();
        if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - EDGEPAN_SIZE)
            offset.z += 1;
        if (Input.GetKey("s") || Input.mousePosition.y <= EDGEPAN_SIZE)
            offset.z -= 1;
        if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - EDGEPAN_SIZE)
            offset.x += 1;
        if (Input.GetKey("a") || Input.mousePosition.x <= EDGEPAN_SIZE)
            offset.x -= 1;
        offset *= CAM_SPEED * Time.deltaTime;
        offset += transform.forward * Input.mouseScrollDelta.y;

        offset += gameObject.transform.position;
        if (Input.GetKey("space"))
        {
            //offset.x = SquadManager._i.getCurrentSquad().getSquad()[0].gameObject.transform.position.x;
            //offset.z = SquadManager._i.getCurrentSquad().getSquad()[0].gameObject.transform.position.z - 6;
        }

        if (offset.magnitude == 0)
            return;
        offset.x = Mathf.Clamp(offset.x, -CAM_X_BOUND, CAM_X_BOUND);
        offset.z = Mathf.Clamp(offset.z, -CAM_Z_BOUND, CAM_Z_BOUND);
        gameObject.transform.position = offset;

        if (Input.GetMouseButtonDown(2))
        {
            grip_pos = Input.mousePosition;
        }
        if (Input.GetMouseButton(2))
        {
            Vector3 diff = Input.mousePosition - grip_pos;
            grip_pos = Input.mousePosition;
        }
        //TODO: look at SmoothDamp() for cam jump
    }
}
