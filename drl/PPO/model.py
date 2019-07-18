import torch
import torch.nn as nn
import numpy as np

def xavier(layer):
    if isinstance(layer, nn.Conv2d) or isinstance(layer, nn.Linear):
        nn.init.xavier_uniform_(layer.weight)

class Flatten(nn.Module):

    def forward(self, x):
        return x.view(x.size()[0], -1)

class ActorCritic(nn.Module):
    def __init__(self, obs_size, act_size):
        '''
        obs_size - (C, H, W) tuple of a visual observation
        act_size - action space size
        '''
        super().__init__()

        self.action_dim = act_size
        self.state_dim = obs_size
        
        conv_size = self.get_conv_out()
        fc_critic = self.hidden_layers() \
            + [Flatten(), 
                nn.Linear(conv_size, conv_size // 2), 
                nn.LeakyReLU(), 
                nn.Linear(conv_size // 2, 1)]

        fc_actor = self.hidden_layers() \
            + [Flatten(), 
                nn.LeakyReLU(), 
                nn.Linear(conv_size, self.action_dim), 
                nn.Tanh()]
        
        self.actor = nn.Sequential(*fc_actor)
        self.critic = nn.Sequential(*fc_critic)

        self.init_weights()

        self.log_std = nn.Parameter(torch.zeros(act_size))

    def init_weights(self):
        self.actor.apply(xavier)
        self.critic.apply(xavier)

    def hidden_layers(self):
        return [
            nn.Conv2d(self.state_dim[0], 16, 4, stride=4),
            nn.LeakyReLU(),
            nn.Conv2d(16, 32, 4, stride=2),
            nn.LeakyReLU(),
            nn.Conv2d(32, 64, 3, stride=2),
        ]

    def get_conv_out(self):
        o = nn.Sequential(*self.hidden_layers())(torch.zeros(1, *self.state_dim))
        return int(np.prod(o.size()))

    def forward(self, x):
        value = self.critic(x)
        mu = self.actor(x)

        std = self.log_std.exp().expand_as(mu)
        dist = torch.distributions.Normal(mu, std)

        # actions are [-1, 1]
        actions = torch.clamp(dist.sample(), -1, 1)

        log_prob = dist.log_prob(actions).mean()
        entropy = dist.entropy().mean()
        return actions, log_prob, entropy, value

    def state_values(self, states):
        return self.critic(states)
