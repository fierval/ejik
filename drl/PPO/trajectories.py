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

    def __init__(self, env, policy, num_agents, tmax=3, gamma = 0.99, gae_lambda = 0.96, is_visual = False, visual_state_size=1, debug = False, is_training=True):
        self.env = env
        self.policy = policy

        self.num_agents = num_agents

        self.tmax = tmax
        self.gae_lambda = gae_lambda
        self.gamma = gamma

        self.debug = debug

        self.is_visual = is_visual
        self.visual_state_size = visual_state_size

        self.rewards = None
        self.scores_by_episode = []

        self.brain_name = self.env.brain_names[0]
        self.action_space_size = self.env.brains[self.brain_name].vector_action_space_size[0]

        self.last_states = None
        self.is_training = is_training
        self.reset()

    @staticmethod
    def to_tensor(x, dtype=np.float32):
        return torch.from_numpy(np.array(x).astype(dtype)).to(device)

    @staticmethod
    def get_agent_observations(env_info):
        '''
        Retrieve all visual observations for all agents
        '''

        obs = np.squeeze(np.array(env_info.visual_observations), axis=1)
        return TrajectoryCollector.to_tensor(obs)

    @staticmethod
    def get_torch_obs(observations):
        return torch.cat(observations, dim=3).permute(0, 3, 1, 2)

    def collect_visual_observation(self, actions=None, initial=False):
        # frames are in CHW format, they come back from unity in HWC
        observations = []
        rewards = []
        dones = []
        
        if initial:
            env_info = self.env.step(actions)[self.brain_name]
        
            observations = [self.get_agent_observations(env_info)] * self.visual_state_size
            return self.get_torch_obs(observations)

        for i in range(self.visual_state_size):
            # keep advancing with the current actions
            env_info = self.env.step(actions, text_action="act")[self.brain_name]            

            obs_per_agent = np.array(env_info.visual_observations)
            observations.append(self.get_agent_observations(env_info))

            rewards.append(env_info.rewards)
            dones.append(env_info.local_done)

            if any(env_info.local_done):
                break

        # done early!
        # simply copy remaining states
        if i < self.visual_state_size - 1:
            for j in range(i + 1, self.visual_state_size):
                observations.append(observations[-1])
                rewards.append(np.zeros_like(np.array(env_info.rewards)))

        rewards = np.array(rewards)
        rewards = rewards.sum(axis=0)
        dones = np.array(dones).sum(axis=0)

        return self.get_torch_obs(observations), self.to_tensor(rewards), self.to_tensor(dones, dtype=np.uint8)
       

    def reset(self):
        env_info = self.env.reset(train_mode=self.is_training)[self.brain_name]

        # for visual observations we are doing the stacking
        if self.is_visual:
            self.last_states = self.collect_visual_observation(initial=True)
        else:
            self.last_states = self.to_tensor(env_info.vector_observations)

    def next_observation(self, actions):
            
        if self.is_visual:
            next_states, rewards, dones = self.collect_visual_observation(actions, initial=False)
        else:            
            # agent will act on the action vector where everything is set to "0"
            # signal it to ignore these actions and only listent to us
            env_info = self.env.step(actions, text_action="act")[self.brain_name]
            rewards = self.to_tensor(env_info.rewards)
            dones = self.to_tensor(env_info.local_done, dtype=np.uint8)

            next_states = self.to_tensor(env_info.vector_observations)
        return next_states, rewards, dones

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
           
            memory["next_states"], memory["rewards"], memory["dones"] = self.next_observation(actions_np)

            # stack one step memory to buffer
            for k, v in memory.items():
                buffer[k].append(v.unsqueeze(0))

            self.last_states = memory["next_states"]
            r = np.array(memory["rewards"].cpu().numpy())[None,:]
            if self.rewards is None:
                self.rewards = r
            else:
                self.rewards = np.r_[self.rewards, r]

            if memory["dones"].any():
                rewards_mean = self.rewards.sum(axis=0).mean()
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
            if len(v.shape) == 5: # images
                buffer[k] = v.squeeze(1)
            elif len(v.shape) == 3:
                buffer[k] = v.reshape([-1, v.shape[-1]])
            else:
                buffer[k] = v.reshape([-1])

        return buffer    
