from model import GaussianPolicyActorCritic
import torch
import numpy as np
import time
import os
import sys

from unityagents import UnityEnvironment
from agent import PPOAgent
import tensorboardX
from utils import RewardTracker, TBMeanTracker
from trajectories import TrajectoryCollector
import torch.optim.lr_scheduler as lr_scheduler


LR = 1e-03              # learing rate
EPSILON = 0.1           # action clipping param: [1-EPSILON, 1+EPSILON]
BETA = 0.01             # regularization parameter for entropy term
EPOCHS = 20              # train for this number of epochs at a time
TMAX = 1024              # maximum trajectory length
AVG_WIN = 100           # moving average over...
SEED = 12                # leave everything to chance
BATCH_SIZE = 128         # number of tgajectories to collect for learning
SOLVED_SCORE = 100      # score at which we are done
STEP_DECAY = 2000       # when to decay learning rate
GAMMA = 0.99            # discount factor
GAE_LAMBDA = 0.96       # lambda-factor in the advantage estimator for PPO

debug = False
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

if __name__ == "__main__":

    root_path = os.path.split(os.path.split(__file__)[0])[0]
    if root_path == '':
        root_path = os.path.abspath("..")

    if sys.platform == 'linux':
        env = UnityEnvironment(file_name=os.path.join(root_path, "Tennis_Linux/Tennis.x86_64"))
    else:
        env = UnityEnvironment(file_name=os.path.join(root_path, "Tennis_Win/Tennis"))
    
    brain_name = env.brain_names[0]
    brain = env.brains[brain_name]

    env_info = env.reset(train_mode=True)[brain_name]

    num_agents = len(env_info.agents)
    print('Number of agents:', num_agents)

    # size of each action
    action_size = brain.vector_action_space_size
    print('Size of each action:', action_size)

    # examine the state space 
    states = env_info.vector_observations
    state_size = states.shape[1]
    
    # torch.manual_seed(SEED)
    # np.random.seed(SEED)

    # create policy to be trained & optimizer
    policy = GaussianPolicyActorCritic(state_size + 1, action_size).to(device)

    writer = tensorboardX.SummaryWriter(comment=f"-mappo_{SEED}")
    
    trajectory_collector = TrajectoryCollector(env, policy, num_agents, tmax=TMAX, gamma=GAMMA, gae_lambda=GAE_LAMBDA, debug=debug)
    tb_tracker = TBMeanTracker(writer, EPOCHS)

    agent = PPOAgent(policy, tb_tracker, LR, EPSILON, BETA)
    
    #scheduler = lr_scheduler.LambdaLR(agent.optimizer, lambda ep: 0.1 if ep == STEP_DECAY else 1)
    scheduler = lr_scheduler.MultiStepLR(agent.optimizer, [k * STEP_DECAY for k in range(1, 2)], gamma=0.1)
    n_episodes = 0
    max_score = - np.Inf

    traj_attributes = ["states", "actions", "log_probs", "advantages", "returns"]
    solved = False
    start = None

    with RewardTracker(writer, mean_window=AVG_WIN, print_every=AVG_WIN // 2) as reward_tracker:

        while True:
            
            trajectories = trajectory_collector.create_trajectories()
            
            n_samples = trajectories['actions'].shape[0]
            n_batches = int((n_samples + 1) / BATCH_SIZE)

            idx = np.arange(n_samples)
            np.random.shuffle(idx)
            for k, v in trajectories.items():
                trajectories[k] = v[idx]

            # first see our rewards and then train
            rewards = trajectory_collector.scores_by_episode[n_episodes : ]

            end_time = time.time()
            for idx_r, reward in enumerate(rewards):
                mean_reward = reward_tracker.reward(reward, n_episodes + idx_r, end_time - start if start is not None else 0)
                
                # we switch LR to 1e-4 in the middle
                scheduler.step()

                # keep current spectacular scores
                if reward > max_score and reward > 1:
                    torch.save(policy.state_dict(), os.path.join(root_path, f'checkpoint_actor_{reward:.03f}.pth'))
                    max_score = reward

                if mean_reward is not None and mean_reward >= SOLVED_SCORE:
                    torch.save(policy.state_dict(), os.path.join(root_path, f'checkpoint_actor_{mean_reward:.03f}.pth'))
                    solved_episode = n_episodes + idx_r - AVG_WIN - 1
                    print(f"Solved in {solved_episode if solved_episode > 0 else n_episodes + idx_r} episodes")
                    solved = True
                    break

            if solved:
                break

            start = time.time()
            # train agents in a round-robin for the number of epochs
            for epoch in range(EPOCHS):
                for batch in range(n_batches):    

                    idx_start = BATCH_SIZE * batch
                    idx_end = idx_start + BATCH_SIZE

                    # select the batch of trajectory entries
                    params = [trajectories[k][idx_start : idx_end] for k in traj_attributes]

                    (states, actions, log_probs, advantages, returns) = params

                    agent.learn(log_probs, states, actions, advantages, returns)

            end_time = time.time()

            n_episodes += len(rewards)
