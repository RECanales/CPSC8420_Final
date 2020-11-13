﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // this script is for handling collisions between the fingers and objects in the scene 
    // objects that are to be grasped need a collider attached (convex preferred)
    public int number_contact = 0;
    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name.Contains("tip"))
            number_contact++;

        number_contact = Mathf.Min(5, number_contact);
        //print("OnCollisionEnter " + number_contact.ToString());
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.name.Contains("tip"))
            number_contact--;

        number_contact = Mathf.Max(0, number_contact);
        //print("OnCollisionExit " + number_contact.ToString());
    }
}
