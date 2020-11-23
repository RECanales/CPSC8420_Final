# -*- coding: utf-8 -*-
"""
Created on Fri Nov 20 15:11:28 2020

@author: kvadner
"""

import numpy as np
import matplotlib.pyplot as plt

path = 'C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/RewardLog.csv'
#path = 'C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/EpisodeLog.csv'
data = np.loadtxt(path, delimiter=',', usecols = (0,1),dtype=('i4','f4'))
plt.xlabel("Number of Iterations")
#plt.ylabel("Number of steps in that episode")
plt.ylabel("Total reward collected")
plt.plot(data[:,0],data[:,1])
plt.savefig('C:/Users/kvadner/Desktop/Fall 2020/Adv. ML/Project/CPSC8420_Final/Reward_Graph.png')