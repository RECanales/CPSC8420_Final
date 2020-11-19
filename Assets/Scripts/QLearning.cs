using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.IO;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    Controller handControl;
    public GameObject scene_obj;
    int NUM_STATES = 100; // discretized hand position (2d 10x10 = 100 position grid for now) and finger rotation (0 - 60, @ rotate speed)
    int NUM_ACTIONS = 5; // move hand in some direction, rotate fingers in
    public float gamma = 0.95f; // discount factor
    public float eps = 1.0f;    // epsilon-greedy parameter
    public float alpha = 0.1f;  // learning rate
    public int max_iterations = 1000;
    //public float v[NUM_STATES][NUM_ACTIONS];
    //public float pi[NUM_STATES];
    List<List<float>> Q = new List<List<float>>();  // Q values
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy
    bool done = false;

    bool start_training = false;


    bool written = false;

    public int iteration_number = 0;
    bool stop_animation = false;
    public bool episode_done = true;
    public int episode_loop = 0;
    public int initial_state = 0;
    float initial_dist = 0;
    float initial_fingertip_dist = 0;
    float CountDown = 2.5f;

    List<int> stored_states = new List<int>(); // store the sequence of states/actions after policy learned

    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        NUM_STATES = 1600 * (int)(handControl.max_joint_rotation / handControl.rotate_speed); //* 4;
        print("Number of states = " + NUM_STATES.ToString());

        // init all Q values to 0
        for(int i = 0; i < NUM_STATES; ++i)
        {
            Q.Add(new List<float> { 0, 0, 0, 0, 0 });
            pi.Add(i, 0);
        }

        scene_obj.AddComponent<CollisionDetector>();
        Time.fixedDeltaTime /= 5;
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
            //case 5:
                //handControl.MoveFinger(0, "open");
                //handControl.MoveFinger(1, "open");
                //handControl.MoveFinger(2, "open");
                //handControl.MoveFinger(3, "open");
                //handControl.MoveFinger(4, "open");
                //break;
            case 5:
                // adjust grip open
                handControl.AdjustGrip(1);
                break;
            case 6:
                handControl.AdjustGrip(-1);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!start_training && handControl.ready) // makes sure hand is intialized before training
        {
            initial_dist = 10000;
            //initial_fingertip_dist = handControl.GetAvgDistFromBall();
            start_training = true;
            //StartCoroutine("PhysicsQLearn");
        }

        else
        {
            if (iteration_number < max_iterations)
            {
                //handControl.ResetState();
                //Q_learn();

                if (episode_done)
                {
                    episode_done = false;
                    episode_loop = 0;
                    handControl.ResetState();
                    initial_state = handControl.GetState();
                    StartCoroutine("PhysicsQLearn");
                    iteration_number++;
                    eps = Mathf.Max(0.1f, (1 - (float)(iteration_number) / (float)(max_iterations)));
                }
            }

            else
            {
                
                if (!done)
                {
                    print("DONE");
                    handControl.ResetState();
                    print(handControl.GetState());
                    scene_obj.GetComponent<Rigidbody>().isKinematic = false;
                    //StopCoroutine("PhysicsQLearn");
					
					// RC moved this here
					if (!written) 
					{
						//Writing the policy to a .csv file.
						using (var writer = new StreamWriter("Grasping_Policy.csv"))
						{
							foreach (var pair in pi)
							{
								writer.WriteLine("{0},{1},", pair.Key, pair.Value);
							}
						}

						print("Learned Policy has been written to Grasping_Policy.csv file.");
						written = true; // do not need this since we have the done variable
                    }

                    done = true;
                }

                if (!stop_animation)
                {
                    int hand_state = handControl.GetState();
                    int action = pi[hand_state];
                    //stored_states.Add(hand_state);
                    Step(action);
                }

                if (scene_obj.GetComponent<CollisionDetector>().reached_goal && !stop_animation)
                {
                    stop_animation = true;
                    print("starting coroutine");
                    StartCoroutine("timer");
                    //stop_animation = false; // loop animation
                }
            }
        }
    }

    IEnumerator timer()
    {
        yield return new WaitForSeconds(CountDown);
        handControl.ResetState();
        print(handControl.GetState());

        stop_animation = false;
    }

    IEnumerator PhysicsQLearn()
    {
        //for (int i = 0; i < max_iterations; ++i)
        while(true)
        {
            int s = initial_state;
            Debug.Assert(s < Q.Count);
            int action = Random.Range(0, NUM_ACTIONS); // random action
            float random_val = Random.value;

            // no index issue
            if (random_val > eps)
                action = GetMaxIndex(Q[s]);

            Debug.Assert(action < Q[0].Count);

            Step(action);
            int curr_state = handControl.GetState();
            bool terminal = handControl.IsTerminal();
            float r = Reward();
            float target = r;
            if (!terminal)
            {
                int next_action = Random.Range(0, NUM_ACTIONS);
                if (random_val > eps)
                    next_action = GetMaxIndex(Q[s]);

                int state = handControl.GetState();
                Debug.Assert(state < Q.Count);
                // no index issue
                target = r + gamma * Q[state][next_action];
            }

            Q[s][action] = (1 - alpha) * Q[s][action] + alpha * target;

            Debug.Assert(s < pi.Keys.Count);
            pi[s] = GetMaxIndex(Q[s]);

            episode_loop++;
            //eps = Mathf.Max(0, (1 - (float)(episode_loop) / (float)(max_iterations)));
            //eps = Mathf.Max(0.1f, eps*(1 - (float)(iteration_number) / (float)(max_iterations)));
            initial_state = curr_state;
            episode_done = terminal;
            if (episode_loop > 500)
            {
                episode_done = true;
            }
            if (terminal)
            {
                yield break;
            }
            yield return new WaitForFixedUpdate();
            //yield return new WaitForEndOfFrame();
            //yield return new WaitForSeconds(0.01f);
        }
        //done = true;
        //handControl.ResetState();
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
        Debug.Assert(max_idx < NUM_ACTIONS);
        return max_idx;
    }

    float Reward()
    {
        // distance of ball from center of hand
        float dist = Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand());
        //dist = handControl.GetAvgDistFromBall();
        if (dist < initial_dist)
        {
            initial_dist = dist;
            //if (scene_obj.GetComponent<CollisionDetector>().reached_goal)
                //return 2;
            return 1;
        }
        if (dist > initial_dist)
        {
            initial_dist = dist;
            return -1;
        }
        return 0;
        //return (1 - dist/initial_dist) + (1 - handControl.GetAvgDistFromBall()/ initial_fingertip_dist);
        //return scene_obj.GetComponent<CollisionDetector>().number_contact / 5f;
    }
}
