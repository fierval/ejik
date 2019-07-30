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
    def __init__(self, obs_size, act_size, model_path=None):
        '''
        obs_size - (C, H, W) tuple of a visual observation
        act_size - action space size
        '''
        super().__init__()

        self.action_dim = act_size
        self.state_dim = obs_size
        
        conv_size = self.get_conv_out()
        self.fc_hidden = self.hidden_layers()

        fc_critic = self.fc_hidden \
            + [Flatten(), 
                nn.Linear(conv_size, conv_size // 2),
                nn.LeakyReLU(),
                nn.Linear(conv_size // 2, 1)]

        fc_actor = self.fc_hidden \
            + [Flatten(), 
                nn.Linear(conv_size, conv_size // 2),
                nn.Tanh(),
                nn.Linear(conv_size // 2, self.action_dim), 
                nn.Tanh()]
        
        self.actor = nn.Sequential(*fc_actor)
        self.critic = nn.Sequential(*fc_critic)
        self.log_std = nn.Parameter(torch.zeros(1, act_size))

        print(f"Actor: {self.actor}")
        print(f"Critic: {self.critic}")
        
        if model_path is None:
            self.init_weights()
        else:
            self.load_state_dict(torch.load(model_path))


    def init_weights(self):
        self.actor.apply(xavier)
        self.critic.apply(xavier)

    def hidden_layers(self):
        return [
            nn.Conv2d(self.state_dim[0], 16, 4, stride=4),
            nn.LeakyReLU(),
            nn.Conv2d(16, 32, 3, stride=2),
            nn.LeakyReLU(),
            nn.Conv2d(32, 64, 3, stride=2),
        ]

    def get_conv_out(self):
        o = nn.Sequential(*self.hidden_layers())(torch.zeros(1, *self.state_dim))
        return int(np.prod(o.size()))

    def forward(self, x, actions=None):
        value = self.critic(x).squeeze(-1)
        mu = self.actor(x)

        std = self.log_std.exp().expand_as(mu)
        dist = torch.distributions.Normal(mu, std)

        # actions are [-1, 1]
        if actions is None:
            actions = dist.sample()

        log_prob = dist.log_prob(actions)
        entropy = dist.entropy()

        log_prob = torch.sum(log_prob, dim=-1)
        entropy = torch.sum(dist.entropy(), dim=-1)

        return actions, log_prob, entropy, value

    def state_values(self, states):
        return self.critic(states)
