# Ejik Goes Home

![ejik.jpg](images/ejik1.jpg)

This is a game + [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) environment used to train the main character to survive enemy attacks.

## Setup

* CUDA 10.0 + cuDNN 7.4
* Unity 2018.x+
*  Visual Studio 2017.

Clone the repo making sure LFS is enabled (should be by default in the newer Git clients)

Create conda [virtual environment](https://docs.conda.io/projects/conda/en/latest/user-guide/tasks/manage-environments.html) and install [PyTorch](https://pytorch.org/get-started/locally/) with [tensorboardX](https://github.com/lanpa/tensorboardX) in it:

Python version in the environment should be set to 3.6:

```sh
$ conda create -n myenv python=3.6 anaconda
$ conda install pytorch torchvision cudatoolkit=10.0 -c pytorch
$ pip install tensorboardX
```

## Deep Reinforcement Learning Environment  

Build the `MainScene` in Unity for DRL experiments.

`drl\PPO\driver.py` - to train  
`drl\PPO\eval.py` - to evaluate.

```sh
python drl\PPO\eval.py
```

## Model Weights

Can be downloaded from [here](https://www.dropbox.com/s/dbphgxb6jdjw0a0/all_enemies_3_frames_net2_1.320.pth?dl=0).

## In Detail

[This]() blog article describes the model in some detail.
