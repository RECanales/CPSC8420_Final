using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    public GameObject rewardDisplay;
    Controller handControl;
    public GameObject scene_obj;
    int NUM_STATES = 100; // discretized hand position (2d 10x10 = 100 position grid for now) and finger rotation (0 - 60, @ rotate speed)
    int NUM_ACTIONS = 2; // move hand in some direction, rotate fingers in
    public float gamma = 0.95f; // discount factor
    public float eps = 1.0f;    // epsilon-greedy parameter
    public float alpha = 0.1f;  // learning rate
    public int max_iterations = 1000;
    //public float v[NUM_STATES][NUM_ACTIONS];
    //public float pi[NUM_STATES];
    List<List<float>> Q = new List<List<float>>();  // Q values
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy
    Dictionary<int, float> logReward = new Dictionary<int, float>(); // logger
    Dictionary<int, float> logEpisode = new Dictionary<int, float>(); // logger
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
    float CountDown = 1f;
    float total_iterations = 0;
    float total_reward = 0;

    float AnimationTime = 4f; // playback for 4 seconds

    List<int> stored_states = new List<int>(); // store the sequence of states/actions after policy learned

    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        //NUM_STATES = 10 * 10 * 20 * (int)(handControl.max_joint_rotation / handControl.rotate_speed); //* 4;
        NUM_STATES = 20 * (int)(handControl.max_joint_rotation / handControl.rotate_speed);
        print("Number of states = " + NUM_STATES.ToString());

        // init all Q values to 0
        for(int i = 0; i < NUM_STATES; ++i)
        {
            List<float> action_list = new List<float>();
            for (int j = 0; j < NUM_ACTIONS; ++j)
                action_list.Add(0);
            Q.Add(action_list);
            pi.Add(i, 0);
        }

        scene_obj.AddComponent<CollisionDetector>();
        Time.fixedDeltaTime /= 10; // faster physics
    }

    void Step(int action)
    {
        switch (action)
        {
            /*case 0:
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
                break;*/
            case 0:
                handControl.MoveHand("up");
                break;
            case 1:
                // close fingers
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
            //case 5:
                // adjust grip open
                //handControl.AdjustGrip(1);
                //break;
            //case 6:
                //handControl.AdjustGrip(-1);
                //break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!start_training && handControl.ready) // makes sure hand is intialized before training
        {
            initial_dist = Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand());
            initial_fingertip_dist = handControl.GetAvgDistFromBall();
            start_training = true;
            StartCoroutine("PhysicsQLearn");
        }

        else
        {
            if (iteration_number < max_iterations)
            {
                //handControl.ResetState();
                //Q_learn();

                //if (episode_done)
                //{
                    //episode_done = false;
                    //episode_loop = 0;

                    //StartCoroutine("PhysicsQLearn");
                    //iteration_number++;
                    
                //}

                float avg_reward = total_reward / (float)total_iterations;
                rewardDisplay.GetComponent<Text>().text = "Average reward: " + avg_reward.ToString("F3");
            }

            else
            {
                
                if (!done)
                {
                    print("DONE");
                    handControl.ResetState();
                    print(handControl.GetState());
                    scene_obj.GetComponent<Rigidbody>().isKinematic = false;
                    StopCoroutine("PhysicsQLearn");
					
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


                        using (var writer = new StreamWriter("EpisodeLog.csv"))
                        {
                            foreach (var pair in logEpisode)
                            {
                                writer.WriteLine("{0},{1},", pair.Key, pair.Value);
                            }
                        }

                        using (var writer = new StreamWriter("RewardLog.csv"))
                        {
                            foreach (var pair in logReward)
                            {
                                writer.WriteLine("{0},{1},", pair.Key, pair.Value);
                            }
                        }
                        print("Episode and reward values have been logged.");


                        written = true; // do not need this since we have the done variable
                    }

                    done = true;
                }

                if (!stop_animation)
                {
                    int hand_state = handControl.GetState();
                    int action = pi[hand_state];
                    //print(hand_state);
                    Step(action);
                    AnimationTime -= Time.deltaTime;
                }

                if (AnimationTime <= 0)
                {
                    stop_animation = true;
                    AnimationTime = 4;
                    print("Starting countdown...");
                    StartCoroutine("timer");
                }
            }
        }
    }

    IEnumerator timer()
    {
        yield return new WaitForSeconds(CountDown);
        handControl.ResetState();
        print("State reset.");
        stop_animation = false;
    }
    

    IEnumerator PhysicsQLearn()
    {
        for (int i = 0; i < max_iterations; ++i)
        {
            handControl.ResetState();
            int s = handControl.GetState();
            episode_loop = 0;
            while (true)
            {
                
                Debug.Assert(s < Q.Count);
                int action = Random.Range(0, NUM_ACTIONS); // random action
                float random_val = Random.value;

                // no index issue
                if (random_val > eps)
                    action = GetMaxIndex(Q[s]);

                Debug.Assert(action < Q[0].Count);

                Step(action);
                int next_state = handControl.GetState();
                bool terminal = handControl.IsTerminal();
                float r = Reward();
                float target = r;
                if (!terminal)
                {
                    int next_action = Random.Range(0, NUM_ACTIONS);
                    if (random_val > eps)
                        next_action = GetMaxIndex(Q[s]);

                    Debug.Assert(next_state < Q.Count);
                    target = r + gamma * Q[next_state][next_action];
                }

                Q[s][action] = (1 - alpha) * Q[s][action] + alpha * target;

                Debug.Assert(s < pi.Keys.Count);
                pi[s] = GetMaxIndex(Q[s]);
                s = next_state;

                episode_loop++;
                total_reward += r;
                total_iterations++;

                if (terminal || episode_loop > 1000) // prevent infinite loop
                    break;

                yield return new WaitForFixedUpdate();
            }

            iteration_number = i;
            logEpisode.Add(iteration_number, episode_loop);
            logReward.Add(iteration_number, total_reward);
            eps = Mathf.Max(0.1f, (1 - (float)i / (float)(max_iterations)));
        }

        iteration_number = max_iterations;
        print("Total iterations: " + total_iterations.ToString());
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
        //float dist = handControl.GetAvgDistFromBall();
        if (scene_obj.transform.position.y <= 0.0325f || dist >= initial_dist)
            return -1;

        return (1 - dist/initial_dist) + 10 * scene_obj.transform.position.y;
        /*
        if (dist < initial_fingertip_dist)
        {
            //initial_dist = dist;
            return 10 * scene_obj.transform.position.y;
        }

        else
        {
            return -1;
        }*/
    }
}
