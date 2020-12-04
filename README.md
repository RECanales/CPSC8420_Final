# CPSC8420_Final
Requirements: 
Unity 2019.2.17 can be downloaded [here.](https://unity3d.com/get-unity/download/archive)

# Instructions for running the project:
Make sure Unity 2019.2.17 is installed on your machine.

Clone the git repository.

Open "CPSC8420_Final\Assets\Scenes\Main.unity". You will see a window open similar to the image below:

![alt text](https://github.com/RECanales/CPSC8420_Final/blob/main/Unity_start.png)

Click on PlaybackPolicies tab in the Hierarchy panel (Left of the game-scene and below the toolbar). You will now see different options available in the "Inspector panel" (Right of the game-scene).

The *PlaybackPolicies* gameobject should be active (the box in the inspector on the right hand side is checked). Then hit the play button at the top-center of the screen. You should see the hand pick up a sphere, move, then release it over the target represented by a white "X". In this configuration, the program is just playing back the already learned policies to perform these tasks.

You can also make the agent re-train the policies fo grasping and releasing by doing the following:

1. Uncheck the *PlaybackPolicies* gameobject. 
2. Select the *QLearning* gameobject in the Hierarchy panel on the left. A new set of options should be available in the Inspector panel on the right.
3. Activate the *QLearning* gameobject by checking the box at the top of the inspector just to the left of where it says "QLearning". 
4. In the "Q Learning (script)" component underneath the transform component in the inspector, you can select and change variables, including the policy (PolicyType) you want to train, the maximum number of episodes (Max_iterations), and so on. 
5. Make sure that PolicyType is set to "Grasping" by clicking on the dropdown menu next to it and selecting "Grasping".
6. Click on the play button on the top-center of the screen to begin training. You should now be able to see the hand trying to learn to perform the task. You can see the values for epsilon and the current iteration in real-time in the Inspector panel. You can also see the average reward in black text on the bottom right in the Game view.
7. After the grasping policy is finished training, stop the program by pressing the play button again.
8. To train the release policy, change PolicyType in the inspector to "Grasping" by clicking on the dropdown menu next to it and selecting "Releasing". Change any other parameters you'd like.
9. Press the play button to train the release policy.
