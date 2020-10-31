using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    Controller handControl;
    List<Transform> objects = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        handControl.GetCenterOfHand(); // get the hand position (near center of palm)
        GameObject testObj = GameObject.Find("Sphere");
        objects.Add(testObj.transform);

        print(objects[0].position); // Vector3 (x, y, z)

        // example of distance (between ball and hand)
        float dist = Vector3.Magnitude(objects[0].position - handControl.GetCenterOfHand());
        print(objects[0].rotation); // Quaternion
        print(objects[0].rotation.eulerAngles); // degrees x, y, z rotation

        print(Random.Range(0, 5)); // random int between 0 and 5 (inclusive)
        print(Random.Range(0f, 5f)); // floats
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
