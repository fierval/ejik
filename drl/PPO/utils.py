"""Auxiliary classes and functions from https://github.com/Shmuma/Deep-Reinforcement-Learning-Hands-On

"""

import sys
import time
import operator
from datetime import timedelta
import numpy as np
import collections
import copy

import torch
import torch.nn as nn

class TBMeanTracker:
    """
    TensorBoard value tracker: allows to batch fixed amount of historical values and write their mean into TB

    Designed and tested with pytorch-tensorboard in mind
    """
    def __init__(self, writer, batch_size):
        """
        :param writer: writer with close() and add_scalar() methods
        :param batch_size: integer size of batch to track
        """
        assert isinstance(batch_size, int)
        assert writer is not None
        self.writer = writer
        self.batch_size = batch_size
        self._batches = collections.defaultdict(list)

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.writer.close()

    @staticmethod
    def _as_float(value):
        assert isinstance(value, (float, int, np.ndarray, np.generic, torch.autograd.Variable)) or torch.is_tensor(value)
        tensor_val = None
        if isinstance(value, torch.autograd.Variable):
            tensor_val = value.data
        elif torch.is_tensor(value):
            tensor_val = value

        if tensor_val is not None:
            return tensor_val.float().mean()
        elif isinstance(value, np.ndarray):
            return float(np.mean(value))
        else:
            return float(value)

    def track(self, param_name, value, iter_index):
        assert isinstance(param_name, str)
        assert isinstance(iter_index, int)

        data = self._batches[param_name]
        data.append(self._as_float(value))

        if len(data) >= self.batch_size:
            self.writer.add_scalar(param_name, np.mean(data), iter_index)
            data.clear()

class RewardTracker:

    def __init__(self, writer, mean_window = 100, print_every=30):
        """Reward Tracing
        
        Arguments:
            writer {tensorboardX} -- tensorboard saver
        
        Keyword Arguments:
            mean_window {int} -- sliding window for average rewards (default: {100})
            print_every {int} -- how often to print things out (default: {30})
        """

        self.writer = writer
        self.total_rewards = []
        self.mean_window = mean_window
        self.print_every = print_every

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.writer.close()

    def reward(self, reward, frame, duration, epsilon=None):
        self.total_rewards.append(reward)
        i_episode = len(self.total_rewards)
        mean_reward = np.mean(self.total_rewards[-self.mean_window :])

        # output every so often to stdout
        if i_episode % self.print_every == 0:
            print("%d: reward %.3f, mean reward %.3f, duration %.2f s" % (
                i_episode, reward, mean_reward, duration))
            sys.stdout.flush()

        if epsilon is not None:
            self.writer.add_scalar("epsilon", epsilon, frame)
        self.writer.add_scalar(f"reward_{self.mean_window}", mean_reward, frame)
        self.writer.add_scalar("reward", reward, frame)
        self.writer.add_scalar("duration", duration, frame)
        
        return mean_reward if len(self.total_rewards) > 30 else None
