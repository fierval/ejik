import torch
import torch.nn as nn
import torch.nn.functional as F

HID_SIZE = 512

def xavier(sequential):
    for layer in sequential:
        if isinstance(layer, nn.Linear):
            nn.init.xavier_uniform_(layer.weight.data)

def hidden_layers(obs_size, hid_size, hid_size_1):
    return nn.Sequential(
            nn.Linear(obs_size, hid_size),
            nn.LeakyReLU(),
            nn.Linear(hid_size, hid_size_1),
            nn.LeakyReLU(),
            )

class GaussianPolicyActorCritic(nn.Module):
    def __init__(self, obs_size, act_size):
        super().__init__()

        self.action_dim = act_size
        self.state_dim = obs_size

        hid_size = HID_SIZE
        hid_size_1 = HID_SIZE // 2

        self.fc_hidden = hidden_layers(obs_size, hid_size, hid_size_1)
        
        self.fc_actor = nn.Linear(hid_size_1, act_size)
        # value function
        self.fc_critic = nn.Linear(hid_size_1, 1)

        xavier(self.fc_hidden)                
        self.std = nn.Parameter(torch.zeros(act_size))

    def forward(self, states, actions=None):
        phi = self.fc_hidden(states)
        mu = torch.tanh(self.fc_actor(phi))
        value = self.fc_critic(phi).squeeze(-1)

        dist = torch.distributions.Normal(mu, F.softplus(self.std))
        if actions is None:
            actions = dist.sample()
        log_prob = dist.log_prob(actions)
        log_prob = torch.sum(log_prob, dim=-1)
        entropy = torch.sum(dist.entropy(), dim=-1)
        return actions, log_prob, entropy, value

    def state_values(self, states):
        phi = self.fc_hidden(states)
        return self.fc_critic(phi).squeeze(-1)
