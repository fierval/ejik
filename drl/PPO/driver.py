import torch
import numpy as np
import time, datetime
import os
import sys
from model import ActorCritic

from mlagents.envs import UnityEnvironment
from agent import PPOAgent
import tensorboardX
from utils import RewardTracker, TBMeanTracker
from trajectories import TrajectoryCollector
import torch.optim.lr_scheduler as lr_scheduler


LR = 5e-04              # learing rate
EPSILON = 0.1           # action clipping param: [1-EPSILON, 1+EPSILON]
BETA = 0.01             # regularization parameter for entropy term
EPOCHS = 100              # train for this number of epochs at a time
TMAX = 512              # maximum trajectory length
AVG_WIN = 100           # moving average over...
SEED = 12                # leave everything to chance
BATCH_SIZE = 128         # number of tgajectories to collect for learning
SOLVED_SCORE = 0.5      # score at which we are done
STEP_DECAY = 2000       # when to decay learning rate
GAMMA = 0.99            # discount factor
GAE_LAMBDA = 0.96       # lambda-factor in the advantage estimator for PPO
NUM_CONSEQ_FRAMES = 6   # number of consequtive frames that make up a state

SAVE_EVERY = 1000
debug = False
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

if __name__ == "__main__":

    root_path = os.path.split(os.path.split(__file__)[0])[0]
    if root_path == '':
        root_path = os.path.abspath("..")

    # where the environment file is located
    env_path = os.path.join(root_path, "../env/ejik")
    # where to save the model
    ckpt_path = os.path.join(root_path, "saved_model")

    if not os.path.exists(ckpt_path):
        os.makedirs(ckpt_path)

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
    
    # torch.manual_seed(SEED)
    # np.random.seed(SEED)

    # create policy to be trained & optimizer
    policy = ActorCritic(state_size, action_size).to(device)

    writer = tensorboardX.SummaryWriter(comment=f"-ejik")
    
    trajectory_collector = TrajectoryCollector(env, policy, num_agents, tmax=TMAX, gamma=GAMMA, gae_lambda=GAE_LAMBDA, debug=debug, is_visual=True, visual_state_size=NUM_CONSEQ_FRAMES)

    tb_tracker = TBMeanTracker(writer, EPOCHS)

    agent = PPOAgent(policy, tb_tracker, LR, EPSILON, BETA)
    
    #scheduler = lr_scheduler.LambdaLR(agent.optimizer, lambda ep: 0.1 if ep == STEP_DECAY else 1)
    scheduler = lr_scheduler.StepLR(agent.optimizer, step_size=STEP_DECAY, gamma=GAMMA)
    n_episodes = 0
    max_score = - np.Inf

    traj_attributes = ["states", "actions", "log_probs", "advantages", "returns"]
    solved = False
    start = None
    step = 0

    with RewardTracker(writer, mean_window=AVG_WIN, print_every=AVG_WIN // 2) as reward_tracker:
        d = datetime.datetime.today()

        print(f"Started training run: at {d.strftime('%d-%m-%Y %H:%M:%S')}")

        while True:
            
            trajectories = trajectory_collector.create_trajectories()
            
            n_samples = trajectories['actions'].shape[0]
            n_batches = int((n_samples + BATCH_SIZE - 1) / BATCH_SIZE)

            # idx = np.arange(n_samples)
            # np.random.shuffle(idx)
            # for k, v in trajectories.items():
            #    trajectories[k] = v[idx]

            # first see our rewards and then train
            rewards = trajectory_collector.scores_by_episode[n_episodes : ]

            # record the number of "dones" per trajectory
            writer.add_scalar("episodes_per_trajectory", len(rewards), step)
            step += 1

            end_time = time.time()
            for idx_r, reward in enumerate(rewards):
                mean_reward = reward_tracker.reward(reward, n_episodes + idx_r, end_time - start if start is not None else 0)
                
                # we switch LR to 1e-4 in the middle
                scheduler.step()

                # keep current spectacular scores
                if n_episodes > 0 and (reward > max_score or (n_episodes + idx_r) % SAVE_EVERY == 0):
                    torch.save(policy.state_dict(), os.path.join(ckpt_path, f'checkpoint_actor_{reward:.03f}.pth'))
                    max_score = reward

                if mean_reward is not None and mean_reward >= SOLVED_SCORE:
                    torch.save(policy.state_dict(), os.path.join(ckpt_path, f'checkpoint_actor_{mean_reward:.03f}.pth'))
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