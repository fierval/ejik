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
import matplotlib.pyplot as plt

MAX_EPISODE_LENGTH = 2000
NUM_CONSEQ_FRAMES = 6
NUM_RUNS = 3

debug = False
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
ax2 = ax1 = None

def plot(rewards, episode_lengths, is_random):
    plt.figure(figsize=(10, 10))

    global ax1, ax2

    if ax1 is None:
        ax1 = plt.subplot(121)
        ax1.set_title(f'Average reward')
    ax1.plot(rewards, label= "random" if is_random else "brain")
    ax1.legend()
    if ax2 is None:
        ax2 = plt.subplot(122)
        ax2.set_title("Episode length")
    ax2.plot(episode_lengths, label= "random" if is_random else "brain")
    ax2.legend()

def parse_args():
    parser = ArgumentParser()
    parser.add_argument("-m", "--model", default=None, help="full path to the model")
    
    args = parser.parse_args()
    return args

if __name__ == "__main__":
    
    ckpt_path = parse_args().model

    root_path = os.path.split(os.path.split(__file__)[0])[0]
    if root_path == '':
        root_path = os.path.abspath("..")

    # where the environment file is located
    env_path = os.path.join(root_path, "../env/ejik")

    if debug:
        env = UnityEnvironment(file_name=None)
    else:
        env = UnityEnvironment(file_name=env_path)
        
    brain_name = env.brain_names[0]
    brain = env.brains[brain_name]

    env_info = env.reset(train_mode=False)[brain_name]

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
    is_random_run = [False, True]

    for is_random in is_random_run:
        print(f"Staring {'' if is_random else 'non' } random run...")
        avg_rewards = []
        avg_episode_length = 0
        episode_lengths = []
        for _ in range(NUM_RUNS):
            sum_reward = 0
            for ep in range(MAX_EPISODE_LENGTH):
                if not is_random: 
                    actions = agent.act(state).cpu().numpy()
                else:
                    actions = np.random.randn(4)
                next_states, rewards, dones = trajectory_collector.next_observation(actions)

                sum_reward += rewards.cpu().sum()

                state = next_states
                if np.any(dones.cpu().numpy()):
                    trajectory_collector.reset()
                    state = trajectory_collector.last_states
                    avg_rewards.append(sum_reward / (ep + 1))
                    episode_lengths.append(ep + 1)
                    print(f"total time: {ep}: total reward: {sum_reward:.3f}: avg reward: {sum_reward / (ep + 1): .3f}")
                    break

        avg_episode_length = sum(episode_lengths) / NUM_RUNS
        print(f"Average episode length: {avg_episode_length:.2f}")

        plot(avg_rewards, episode_lengths, is_random)

    plt.savefig(r'c:\temp\comparison.png')
    plt.show()