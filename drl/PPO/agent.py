import numpy as np
import random
import torch
import torch.nn.functional as F
import math

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

class PPOAgent():
    """Interacts with and learns from the environment."""

    def __init__(self, policy, tb_tracker=None, lr=None, epsilon=None, beta=None):
        """Initialize an Agent object.
        
        Params
        ======
            policy (Pytorch network): policy to be learned/executed
            optimizer (Pytorch optimizer): optimizer to be used
            policy_critic (Pytorch network): policy critic for V function
            optimizer_critic (Pytorch optimizer): optimizer for crtic
            tb_tracker (tensorboard tracker)
            epsilon - action clipping: [1 - epsilon, 1 + epsilon]
            beta - regularization parameter
        """
        
        self.policy = policy
        self.tb_tracker = tb_tracker
        
        if lr is not None:
            self.optimizer = torch.optim.Adam(self.policy.parameters(), lr=lr)        
            self.beta = beta
            self.epsilon = epsilon
        
        # Initialize time step (for updating every UPDATE_EVERY steps)
        self.t_step = 0

    def act(self, state):
        self.policy.eval()
        with torch.no_grad():
            actions, _, _, _ = self.policy(state)
        self.policy.train()

        return actions

    def learn(self, old_log_probs, states, actions, advantages, returns):
        """Learning step
        
        """
        _, log_probs, entropy, values = self.policy(states, actions)

        self.optimizer.zero_grad()

        # critic loss
        loss_values = F.mse_loss(values, returns)
        self.tb_tracker.track(f"loss_values", loss_values.to("cpu"), self.t_step)

        # actor loss
        ratio = torch.exp(log_probs - old_log_probs)
        ratio_clamped = torch.clamp(ratio, 1 - self.epsilon, 1 + self.epsilon)
        
        adv_PPO = torch.min(ratio * advantages, ratio_clamped * advantages)
        loss_policy = -torch.mean(adv_PPO + self.beta * entropy)

        self.tb_tracker.track(f"loss_policy", loss_policy.to("cpu"), self.t_step)

        # generalized loss
        loss = loss_policy + loss_values
        self.tb_tracker.track(f"loss", loss_policy.to("cpu"), self.t_step)

        loss.backward()

        torch.nn.utils.clip_grad_norm_(self.policy.parameters(), 10.)

        self.optimizer.step()

        del loss

        # decay epsilon and beta as we train
        #self.epsilon *= 0.9999
        #self.beta *= 0.9995
        self.t_step += 1

        