using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // this script is for handling collisions between the fingers and objects in the scene 
    // objects that are to be grasped need a collider attached (convex preferred)
    int prev_contact = 0;
    public int number_contact = 0;
    public bool wall_hit = false;
    public bool reached_goal = false;
    public bool hit_target = false;
    public bool hit_floor = false;
    public bool terminalCollision = false;
    string[] finger_names = new string[5] { "thumb", "index", "middle", "ring", "pinky" };
    List<GameObject> contacts = new List<GameObject>();

    void Start()
    {
    }

    public void ResetState()
    {
        prev_contact = 0;
        number_contact = 0;
        wall_hit = reached_goal = hit_target = hit_floor = terminalCollision = false;
        contacts = new List<GameObject>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name.Contains("wall"))
        {
            wall_hit = true;
            //print("wall hit");
        }

        if (collision.collider.name == "Goal")
        {
            reached_goal = true;
            //print("goal reached");
        }

        if(collision.collider.gameObject.transform.parent && collision.collider.gameObject.transform.parent.name == "Target")
        {
            hit_target = true;
            terminalCollision = true;
            //print("target_hit");
        }

        if (collision.collider.name.Contains("Floor"))
        {
            hit_floor = true;
            terminalCollision = true;
            //print("floor hit");
        }


        /*if (!contacts.Contains(collision.gameObject) && collision.collider.name.Contains("tip"))
        {
            number_contact++;
            contacts.Add(collision.gameObject);
        }*/
        number_contact++;
        //print("OnCollisionEnter " + number_contact.ToString());
    }

    private void OnCollisionExit(Collision collision)
    {
        number_contact--;
        if (collision.collider.name == "Goal")
            reached_goal = false;
        //print("OnCollisionExit " + number_contact.ToString());
    }
}
