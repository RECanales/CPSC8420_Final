using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    Controller handControl;
    List<Transform> objects = new List<Transform>();
    int NUM_STATES = 100; // discretized hand position (2d 10x10 = 100 position grid for now) and finger rotation (0 - 60, @ rotate speed)
    int NUM_ACTIONS = 6; // move hand in some direction, rotate fingers in/out
    public float gamma = 0.95f; // discount factor
    public float eps = 1.0f;    // epsilon-greedy parameter
    public float alpha = 0.1f;  // learning rate
    public int max_iterations = 5000;
    //public float v[NUM_STATES][NUM_ACTIONS];
    //public float pi[NUM_STATES];
    List<List<float>> Q = new List<List<float>>();  // Q values
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy


    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        NUM_STATES = 400 * (int)(60 / handControl.rotate_speed);
        //NUM_STATES = 400 * 30;
        print("Number of states = " + NUM_STATES.ToString());

        // init all Q values to 0
        for(int i = 0; i < NUM_STATES; ++i)
        {
            Q.Add(new List<float> { 0, 0, 0, 0, 0, 0 });

            //print(Q[i][0].ToString() + "," + Q[i][1].ToString());
        }

        //handControl.GetCenterOfHand(); // get the hand position (near center of palm)
        GameObject testObj = GameObject.Find("Sphere");
        objects.Add(testObj.transform);

        // example of distance (between ball and hand)
        //float dist = Vector3.Magnitude(objects[0].position - handControl.GetCenterOfHand());
        //print(objects[0].rotation); // Quaternion
        //print(objects[0].rotation.eulerAngles); // degrees x, y, z rotation

        //print(Random.Range(0, 5)); // random int between 0 and 5 (inclusive)
        //print(Random.Range(0f, 5f)); // floats


    }

    void Step(int action)
    {
        switch (action)
        {
            case 0:
                handControl.MoveHand("right");
                break;
            case 1:
                handControl.MoveHand("left");
                break;
            case 2:
                handControl.MoveHand("forward");
                break;
            case 3:
                handControl.MoveHand("backward");
                break;
            case 4:
                // close
                handControl.MoveFinger(0, "close");
                handControl.MoveFinger(1, "close");
                handControl.MoveFinger(2, "close");
                handControl.MoveFinger(3, "close");
                handControl.MoveFinger(4, "close");
                break;
            case 5:
                // open
                handControl.MoveFinger(0, "open");
                handControl.MoveFinger(1, "open");
                handControl.MoveFinger(2, "open");
                handControl.MoveFinger(3, "open");
                handControl.MoveFinger(4, "open");
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

        //s_, r, terminal, info = env.step(chosen_action)
        /*for (int i = 0; i < 6; i++)
        {
            step(Random.Range(0, 5));
        }*/

        Q_learn();
    }

    void Q_learn()
    {
        for (int i = 0; i < max_iterations; ++i)
        {
            int s = handControl.GetState();
            handControl.ResetState();
            int c = 0;
            while (true)
            {
                int action = Random.Range(0, 5); // random action
                float random_val = Random.value;

                if (random_val > eps)
                    action = GetMaxIndex(Q[s]);

    
                Step(action);     
                int curr_state = handControl.GetState();
                bool terminal = handControl.IsTerminal();
                float r = Reward();
                float target = r;

                if (terminal)
                    break;

                else
                {
                    int next_action = Random.Range(0, 5);
                    if (random_val > eps)
                        next_action = GetMaxIndex(Q[s]);

                    int state = handControl.GetState();
                    target = r + gamma * Q[state][next_action];
                }

                Q[s][action] = (1 - alpha) * Q[s][action] + alpha * target;
                pi[s] = GetMaxIndex(Q[s]);
                if(s > NUM_STATES)
                    print(NUM_STATES.ToString() + "," + s.ToString());
                //print(Q[s][action]);
                c++;
                eps = Mathf.Max(0, 1 - (float)c * 0.001f);
            }
        }

        print("done");
    }

    int GetMaxIndex(List<float> l)
    {
        int max_idx = 0;
        float max_val = l[0];
        for(int i = 1; i < l.Count; ++i)
        {
            if (l[i] > max_val)
            {
                max_idx = i;
                max_val = l[i];
            }
        }
        if (max_idx > 5)
            print("ASDASDASDASD");
        return max_idx;
    }

    float Reward()
    {
        return handControl.GetAvgDistFromBall(); //+ Vector3.Magnitude(handControl.GetCenterOfHand() - objects[0].position);
    }
}
