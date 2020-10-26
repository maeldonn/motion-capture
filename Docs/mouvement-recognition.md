# Mouvement Recognition

## Introduction

There are two main approaches to the problem of motion recognition: we could use motion comparison with a set of pre-recorded movements, or we could use machine learning and neural networks trained to recognize movements.
Having no experience at all with machine learning, we've decided to develop the first solution.

Movement recognition, or more accurately movement comparison, is an expensive solution in terms of computing power. So, the first step is to limit to the maximum the data actually computed.
The second step is to accuratly compare two positions at any given moments, and finally the third step is to get make sense of the results of step 2 to recognize movements in a flux.

## Limiting the amount of work

When comparing positions and movements, some data are not very useful. The bvh files we get from Axis Neuron have 59 nodes, with each a position (composed of three values) and a rotation (idem). So, when we compare the posture of the user with this bvh file, we should have a total of 354 calculations.

This can easily be reduced by firstly removing the comparison of the positions (of the nodes): depending of the body size of the user, these data may not match with the stored data. Furthermore, the rotation of each nodes seem to be enough to recognize a position.

We can also remove all the nodes which are mostly irrelevent in a position: the hips, the torso and the chest.

Finally, if we want to study a movement, the importants nodes are those who are in movement. We can therfore remove those who are mostly immobile.
To achieve that, we firstly calculate the variance of each node. Then, we compare these variances with the highest variance, and if these are superior or equal to a certain percentage then we consider this angle as interesting.

## Position recognition

To recognise a position, we have no choice but to iterate through all of the selected nodes (in step 1), and compare their values with those saved. It will allow us to tell if the position of the user is roughly the same as the one saved.
There are two way to interpret the results: a binary one and a continuous one (in the form of a score).

### Position recognition: Approach 1

We go through all the nodes and every axes of rotation that have been considered interesting in step 1, and we make the following test:

    if( absolute value of (the user's rotation along an axis - the saved rotation along the same axis) >= a degree of margin selected by the user)
    then return false


### Position recognition: Approach 2

The second approach is to give each node studied a score, and then averaging all of these: it will allow use to get a general score for the position. But how do we calculate it? Initially, we tested some custom made calculation, but we finally settled with a score system used in [this paper](https://www.researchgate.net/publication/226380251_A_Method_for_Comparing_Human_Postures_from_Motion_Capture_Data) by Wei-Ting Yang, Zhiqiang Luo, I-Ming Chen, and Song Huat Yeo.

#### First score system

At first, we calculated the score by doing the average of the differences of rotation between the actor position and the saved position.
<br><img src="https://render.githubusercontent.com/render/math?math=score\=\frac{\sum_{i=0}^{N}(\sum_{j=x}^{z} \Delta\theta\ij)}{N\times360\times3}\times100">
<br>With:
* _N_: the number of nodes
* _∆θij_: the difference beteween the user's rotation and the saved rotation
<br>This means that the higher the score, the farther away the user is.
But at the time, it came with a big inconvenient. Indeed, we didn't properly excluded the useless nodes, and so even with a big difference in an angle the score wasn't very impacted.

#### Second score system

Another approach is to weight high outliers. We did it this way:
<br><img src="https://render.githubusercontent.com/render/math?math=score\=\sum_{i=0}^{N}(\sum_{j=x}^{z} (a\times\Delta\theta\ij)^2)">
<br>With:
* _N_: the number of nodes.
* _∆θij_: the difference beteween the user's rotation and the saved rotation.
* a: an adjuster, that allow us to choose from which angle the difference becomes important in the calculation of the score.
<br>This method work well, but is less precise and tend to diminish considerably the performances.

#### Final score system

## Mouvement recognition

//TODO: dire en quoi consiste l'approche générale de la reconnaissance de mouvement

### Mouvement recognition: Approach 1



### Mouvement recognition: Approach 2



### Mouvement recognition: Approach 3



### Mouvement recognition: Approach 4
