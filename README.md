# CPSC8420_Final
Requirements: 
Unity 2019.2.17f1 or newer can be downloaded [here.](https://unity3d.com/unity/qa/lts-releases?page=3)

# Instructions for running the project:
Make sure Unity is installed on your machine.

Clone the git repository.

Open "CPSC8420_Final\Assets\Scenes\Main.unity". You will see a window open similar to the image below:

![alt text](https://github.com/RECanales/CPSC8420_Final/blob/main/Unity_start.png)

Click on PlaybackPolicies tab in the Hierarchy panel (Left of the game-scene and below the toolbar). You will now see different options available in the "Inspector panel" (Right of the game-scene).

Make sure the PlaybackPolicies module and the Play Policeis script check boxes and checked. Then hit the play button at the top-center of the screen. You should be abel to see the hand pick up the object and releasing it at the target marked by "X". In this configuration, the program is just playing back the already learned policies to perform these tasks.

You can also make the agent re-train the policies fo grasping and releasing in the following way:

Uncheck the PlaybackPolicies module and the Play Policies script. 

Click on the QLearning tab in the Hierarchy panel. A new set of options should be available in the Inspector panel.

Check the QLearning module checkbox. In the Q Learning script part, You can select and change various confugurations like the policy type (Grasp/Release) you want to learn, To number of actions the hand can take, Max Episode steps, gamma, alpha, max iterations, and so on. Once you are satisfied with your desired configurations, click on the play button on the top-center of the screen. You should now be able to see the hand trying to learn to perform the tasks. You can also see the changing values in real-time in the Inspector panel.
