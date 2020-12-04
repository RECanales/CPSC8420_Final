# -*- coding: utf-8 -*-
"""
Created on Fri Nov 20 15:11:28 2020

@author: kvadner
"""

import numpy as np
import matplotlib.pyplot as plt
import os 
cwd = os.getcwd()


gamma = 0.95
alpha = 0.1
policy = "Release"
objectType = "Sphere"
actions = 10
path = cwd + '/Release_RewardLog_Cube.csv'
#path = cwd + '\Grasp_RewardLog.csv'
data = np.loadtxt(path, delimiter=',', usecols = (0,1))
plt.xlabel("Number of Iterations")
#plt.ylabel("Number of steps for the episode")
plt.ylabel("Average Reward")
title = f"Iteration vs. Reward"
plt.title(title, fontsize=20)
plt.plot(data[:,0],data[:,1])
plt.savefig(cwd + '/Graphs/ReleaseReward_small_cube.png')