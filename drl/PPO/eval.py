import torch
import numpy as np
import time, datetime
import os
import sys
from model import ActorCritic

from mlagents.envs import UnityEnvironment
from agent import PPOAgent
from trajectories import TrajectoryCollector
from argparse import ArgumentParser

MAX_EPISODE_LENGTH = 2000

debug = False
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

def parse_args():
    parser = ArgumentParser()
    parser.add_argument("-m", "--model", default=None, help="full path to the model")

    return parser

if __name__ == "__main__":

    ckpt_path = parse_args.model

    root_path = os.path.split(os.path.split(__file__)[0])[0]
    if root_path == '':
        root_path = os.path.abspath("..")

    # where the environment file is located
    env_path = os.path.join(root_path, "../env/ejik")
    # where to save the model
    ckpt_path = os.path.join(root_path, "saved_model")

    if debug:
        env = UnityEnvironment(file_name=None)
    else:
        env = UnityEnvironment(file_name=env_path)
        
    brain_name = env.brain_names[0]
    brain = env.brains[brain_name]

    env_info = env.reset(train_mode=True)[brain_name]

    num_agents = len(env_info.agents)
    print('Number of agents:', num_agents)

    # size of each action
    action_size = brain.vector_action_space_size[0]
    print('Size of each action:', action_size)

    # examine the state space 
    states = env_info.visual_observations
    state_size = list(states[0][0].transpose(2, 0, 1).shape)
    state_size[0] *= NUM_CONSEQ_FRAMES
    
    # create policy
    policy = ActorCritic(state_size, action_size, model_path=ckpt_path).to(device)

    trajectory_collector = TrajectoryCollector(env, policy, num_agents, is_visual=True, visual_state_size=NUM_CONSEQ_FRAMES, is_training=False)

    agent = PPOAgent(policy)
    
    state = trajectory_collector.last_states

    sum_reward = 0
    for ep in range(MAX_EPISODE_LENGTH):
        actions = agent.act(state).cpu().numpy()
        next_states, rewards, dones = trajectory_collector.next_observation()

        rewards = rewards.cpu().numpy()
        dones = dones.cpu.numpy()
        
        sum_reward += rewards.sum()
        
        state = next_states
        if np.any(dones):
            print(f"{ep}: total reward: {sum_reward}")
            break
            
