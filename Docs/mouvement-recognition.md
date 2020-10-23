# Mouvement Recognition

## Introduction

There are two main approaches to the problem of motion recognition: we could use motion comparison with a set of pre-recorded movements, or we could use machine learning and neural networks trained to recognize movements.
Having no experience at all with machine learning, we decided to develop the first solution.

Movement recognition, or more accurately movement comparison, is an expensive solution in terms of computing power. So, the first step is to limit to the maximum the data actually computed.
The second step is to accuratly compare two positions at any given moments, and finally the third step is to get make sense of the results of step 2 to recognize of movement in a flux.

## Limiting the amount of work

When comparing positions and movements, some data are not very useful. The bvh files we get from Axis Neuron have 59 nodes, with each a position (composed of three values) and a rotation (idem). So, when we compare the posture of the user with this bvh file, we should have a total of 354 calculations.

This can easily be reduced by firstly removing the comparison of the positions (of the nodes): depending of the body size of the user, these data may not match with the stored data. Furthermore, the rotation of each nodes seem to be enough to recognize of position.

We can also remove all the nodes which are mostly irrelevent in a position: the hips, the torso and the chest.

Finally, if we want to study a movement, the importants nodes are those who are in movement. We can therfore remove those who are mostly immobile.
To achieve that, we firstly calculate the variance of each node. Then, we compare these variances with the highest variance, and if these are superior or equal to a certain percentage then we consider this angle as interesting.  

## Position recognition

### Methode 1

### Methode 2

## Mouvement recognition

### Methode 1

### Methode 2

### Methode 3

### Methode 4
