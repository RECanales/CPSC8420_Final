using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    Controller handControl;
    List<Transform> objects = new List<Transform>();
    //public int NUM_STATES = 12;
    //public int NUM_ACTIONS = 8;
    //public float gamma = 0.95;
    //public float eps = 1.0;
    //public float alpha = 0.1;
    //public int max_iterations = 1;
    //public float v[NUM_STATES][NUM_ACTIONS];
    //public float pi[NUM_STATES];
    //public float q[NUM_STATES][NUM_ACTIONS];

    // Start is called before the first frame update
    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        //handControl.GetCenterOfHand(); // get the hand position (near center of palm)
        GameObject testObj = GameObject.Find("Sphere");
        objects.Add(testObj.transform);

        //print(objects[0].position); // Vector3 (x, y, z)

        // example of distance (between ball and hand)
        float dist = Vector3.Magnitude(objects[0].position - handControl.GetCenterOfHand());
        //print(objects[0].rotation); // Quaternion
        //print(objects[0].rotation.eulerAngles); // degrees x, y, z rotation

        //print(Random.Range(0, 5)); // random int between 0 and 5 (inclusive)
        //print(Random.Range(0f, 5f)); // floats

    }

    float step(int action)
    {
        switch (action)
        {
            case 0:
                handControl.MoveHand("up");
                break;
            case 1:
                handControl.MoveHand("down");
                break;
            case 2:
                handControl.MoveHand("right");
                break;
            case 3:
                handControl.MoveHand("left");
                break;
            case 4:
                handControl.MoveFinger(0, "close");
                handControl.MoveFinger(1, "close");
                handControl.MoveFinger(2, "close");
                handControl.MoveFinger(3, "close");
                handControl.MoveFinger(4, "close");
                break;
            case 5:
                handControl.MoveFinger(0, "open");
                handControl.MoveFinger(1, "open");
                handControl.MoveFinger(2, "open");
                handControl.MoveFinger(3, "open");
                handControl.MoveFinger(4, "open");
                break;
        }
        return 0;
    }

    float reward()
    {
        return 0;
    }
    // Update is called once per frame
    void Update()
    {
            //s_, r, terminal, info = env.step(chosen_action)
            for (int i = 0; i < 6; i++)
            {
                step(Random.Range(0, 5));
            }
    }
}
