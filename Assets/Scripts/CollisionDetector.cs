using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // this script is for handling collisions between the fingers and objects in the scene 
    // objects that are to be grasped need a collider attached (convex preferred)

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        print(collision.collider.name);
    }
}
