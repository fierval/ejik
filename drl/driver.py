import torch
import numpy as np
import time
import os
import sys

from mlagents.envs import UnityEnvironment
import tensorboardX
import matplotlib.pyplot as plt

debug = False
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

if __name__ == "__main__":

    env = UnityEnvironment()
    
    brain_name = env.brain_names[0]
    brain = env.brains[brain_name]

    env_info = env.reset(train_mode=True)[brain_name]

    num_agents = len(env_info.agents)
    print('Number of agents:', num_agents)

    # size of each action
    action_size = brain.vector_action_space_size
    print('Size of each action:', action_size)

    # examine the state space 
    states = env_info.visual_observations[0][0]
    state_size = states.shape

    plt.imshow(states)
    plt.show()

    
    
