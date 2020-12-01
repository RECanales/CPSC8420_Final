# -*- coding: utf-8 -*-
"""
Created on Fri Nov 20 15:11:28 2020

@author: kvadner
"""

import numpy as np
import matplotlib.pyplot as plt


gamma = 0.95
alpha = 0.1
policy = "Release"
objectType = "Sphere"
actions = 10

#path = 'C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/Release_RewardLog.csv'
path = 'C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/Release_EpisodeLog.csv'
data = np.loadtxt(path, delimiter=',', usecols = (0,1))
plt.xlabel("Number of Iterations")
plt.ylabel("Number of steps for the episode")
#plt.ylabel("Average reward collected")
title = f"Iterations vs. the Episode Length for the Object {policy} policy learned"
plt.title(title, fontsize=20)
plt.plot(data[:,0],data[:,1])
plt.savefig('C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/Grasping_Reward_Graph.png')