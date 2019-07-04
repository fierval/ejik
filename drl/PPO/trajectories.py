import numpy as np

import torch
import numpy as np

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

class TrajectoryCollector:
    """
    Collects trajectories and splits them between agents
    """
    buffer_attrs = [
            "states", "actions", "next_states",
            "rewards", "log_probs", "dones",
            "values", "advantages", "returns"
        ]

    def __init__(self, env, policy, num_agents, tmax=3, gamma = 0.99, gae_lambda = 0.96, debug = False):
        self.env = env
        self.policy = policy

        self.num_agents = num_agents
        self.idx_me = torch.tensor([index+1 for index in range(num_agents)], dtype=torch.float).unsqueeze(1).to(device)

        self.tmax = tmax
        self.gae_lambda = gae_lambda
        self.gamma = gamma

        self.debug = debug

        self.rewards = None
        self.scores_by_episode = []
        self.brain_name = None
        self.last_states = None
        self.reset()
        
    @staticmethod
    def to_tensor(x, dtype=np.float32):
        return torch.from_numpy(np.array(x).astype(dtype)).to(device)

    def add_agents_to_state(self, state):
        return state
        # return torch.cat((state, 0.001 * self.idx_me), dim=1)

    def reset(self):
        self.brain_name = self.env.brain_names[0]
        env_info = self.env.reset(train_mode=True)[self.brain_name]
        self.last_states = self.to_tensor(env_info.vector_observations)
        self.last_states = self.add_agents_to_state(self.last_states)

    def calc_returns(self, rewards, values, dones, last_values):
        n_step, n_agent = rewards.shape

        # Create empty buffer
        GAE = torch.zeros_like(rewards).float().to(device)
        returns = torch.zeros_like(rewards).float().to(device)

        # Set start values
        GAE_current = torch.zeros(n_agent).float().to(device)
        returns_current = last_values
        values_next = last_values

        for irow in reversed(range(n_step)):
            values_current = values[irow]
            rewards_current = rewards[irow]
            gamma = self.gamma * (1. - dones[irow].float())

            # Calculate TD Error
            td_error = rewards_current + gamma * values_next - values_current
            # Update GAE, returns
            GAE_current = td_error + gamma * self.gae_lambda * GAE_current
            returns_current = rewards_current + gamma * returns_current
            # Set GAE, returns to buffer
            GAE[irow] = GAE_current
            returns[irow] = returns_current

            values_next = values_current

        return GAE, returns

    def create_trajectories(self):
        """
        Inspired by: https://github.com/tnakae/Udacity-DeepRL-p3-collab-compet/blob/master/PPO/agent.py
        Creates trajectories and splites them between all agents, so each one gets individualized trajectories

        Returns:
        A list  of dictionaries, where each list contains a trajectory for its agent
        """

        buffer = {k: [] for k in self.buffer_attrs}

        for t in range(self.tmax):
            memory = {}

            # draw action from model
            memory["states"] = self.last_states
            pred = self.policy(memory["states"])
            pred = [v.detach() for v in pred]
            memory["actions"], memory["log_probs"], _, memory["values"] = pred

            # one step forward
            actions_np = memory["actions"].cpu().numpy()
            env_info = self.env.step(actions_np)[self.brain_name]
            memory["next_states"] = self.to_tensor(env_info.vector_observations)
            memory["rewards"] = self.to_tensor(env_info.rewards)
            memory["dones"] = self.to_tensor(env_info.local_done, dtype=np.uint8)

            # stack one step memory to buffer
            for k, v in memory.items():
                buffer[k].append(v.unsqueeze(0))

            self.last_states = memory["next_states"]
            r = np.array(env_info.rewards)[None,:]
            if self.rewards is None:
                self.rewards = r
            else:
                self.rewards = np.r_[self.rewards, r]

            if memory["dones"].any():
                rewards_mean = self.rewards.sum(axis=0).max()
                self.scores_by_episode.append(rewards_mean)
                self.rewards = None
                self.reset()

        # create tensors
        for k, v in buffer.items():
            # advantages and returns have not yet been computed
            if len(v) > 0:
                buffer[k] = torch.cat(v, dim=0)

        # append returns and advantages
        values = self.policy.state_values(self.last_states).detach()
        advantages, buffer["returns"] = self.calc_returns(buffer["rewards"], buffer["values"], buffer["dones"], values)
        buffer["advantages"] = (advantages - advantages.mean()) / (advantages.std() + 1e-10)

        for k, v in buffer.items():
            # flatten everything.
            if len(v.shape) == 3:
                buffer[k] = v.reshape([-1, v.shape[-1]])
            else:
                buffer[k] = v.reshape([-1])

        return buffer    
