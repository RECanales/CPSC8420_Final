using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // this script is for handling collisions between the fingers and objects in the scene 
    // objects that are to be grasped need a collider attached (convex preferred)
    int prev_contact = 0;
    public int number_contact = 0;
    public bool ungripped = false;
    public bool reached_goal = false;
    public bool hit_target = false;
    void Start()
    {
        
    }

    public void ResetState()
    {
        prev_contact = 0;
        number_contact = 0;
        ungripped = reached_goal = hit_target = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        prev_contact = number_contact;
        if (collision.collider.name.Contains("tip") || collision.collider.name.Contains("1"))
        {
            number_contact++;

            //ungripped = !collision.collider.gameObject.transform.parent.name.Contains("thumb");
            //number_contact = Mathf.Min(5, number_contact);
        }
        if (collision.collider.name.Contains("wall"))
        {
            ungripped = true;
            print("wall hit");
        }

        if (collision.collider.name == "Goal")
        {
            reached_goal = true;
            print("goal reached");
        }

        if(collision.collider.gameObject.transform.parent && collision.collider.gameObject.transform.parent.name == "Target")
        {
            hit_target = true;
        }
        //print("OnCollisionEnter " + number_contact.ToString());
    }

    private void OnCollisionExit(Collision collision)
    {
        prev_contact = number_contact;
        if (collision.collider.name.Contains("tip") || collision.collider.name.Contains("1"))
        {
            number_contact--;

            //number_contact = Mathf.Max(0, number_contact);
        }
        if (collision.collider.name == "Goal")
            reached_goal = false;
        //print("OnCollisionExit " + number_contact.ToString());
    }
}
