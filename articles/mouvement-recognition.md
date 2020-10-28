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

The second approach is to give each node studied a score, and then averaging all of these: it will allow use to get a general score for the position. But how do we calculate it? Initially, we tested some custom made calculations, but we finally settled with a score system used in [this study](https://www.researchgate.net/publication/226380251_A_Method_for_Comparing_Human_Postures_from_Motion_Capture_Data) by Wei-Ting Yang, Zhiqiang Luo, I-Ming Chen, and Song Huat Yeo.

#### First score system

At first, we calculated the score by doing the average of the differences of rotation between the actor position and the saved position.
<br><img src="https://render.githubusercontent.com/render/math?math=score\=\frac{\sum_{i=0}^{N}(\sum_{j=x}^{z} \Delta\theta\ij)}{N\times360\times3}\times100">
<br>With:
* _N_: the number of nodes.
* _∆θij_: the difference beteween the user's rotation and the saved rotation.

This means that the higher the score, the farther away the user is.
But at the time, it came with a big inconvenient. Indeed, we didn't properly excluded the useless nodes, and so even with a big difference in an angle the score wasn't very impacted.

#### Second score system

Another approach is to weight high outliers. We did it this way:
<br><img src="https://render.githubusercontent.com/render/math?math=score\=\sum_{i=0}^{N}(\sum_{j=x}^{z} (a\times\Delta\theta\ij)^2)">
<br>With:
* _N_: the number of nodes.
* _∆θij_: the difference beteween the user's rotation and the saved rotation.
* a: an adjuster, that allow us to choose from which angle the difference becomes important in the calculation of the score.

This method work well, but is less precise and tend to diminish considerably the performances.

#### Final score system

The final score system is from the study mentioned above. It work somewhat like the first one. It is calculated like this:
<br><img src="https://render.githubusercontent.com/render/math?math=score\=\frac{\sum_{i=0}^{N}[\sum_{j=x}^{z} (1-\frac{\Delta\theta\ij}{90})]/3}{N}">
<br>With:
* _N_: the number of nodes.
* _∆θij_: the difference beteween the user's rotation and the saved rotation.

This method return an easy to read result ranging from 0 to 1 (considering that due to biological limitation,  ∆θij can hardly be higher than 90°).

## Mouvement recognition

//TODO: dire en quoi consiste l'approche générale de la reconnaissance de mouvement
There are two main elements to detect when trying to recognize a movement: the beginning and the end. 
To recognize the beginning, we have chosen to try, at each frame, to recognize the first frame of the saved position. 
To detect the end, we check if the time elapsed since the detection of the first frame is greater than the total time of the animation.
The next step is to find out if during all this time the user was doing the movement or not.

Here's how we did it. Each movement saved also have a list of float attached to it. These lists are used to store the time passed since a first frame have been detected. We do someting like that:
    
    foreach(mouvement in allMouvementsToDetect)
    {
        if(mouvement.frame[0].position = user.position)
        {
            Debug("Beginning of a movement detected!");
            mouvement.listOfTimesPassedSinceFirstFrame.Add(0);
        }
        
        for (var i = mouvement.listOfTimesPassedSinceFirstFrame.Count-1; i >=0 ; i--)
        {
            mouvement.listOfTimesPassedSinceFirstFrame[i] = timeSinceLastFrame;
            
            if(mouvement.listOfTimesPassedSinceFirstFrame[i] >= mouvement.TotalTime)
            {
                Debug("End of a movement detected!");
                mouvement.listOfTimesPassedSinceFirstFrame.RemoveAt(i);
                continue;
            }
            
            nbFrame = calculateFrame(listOfTimesPassedSinceFirstFrame[i])
            
            if(mouvement.frame[nbFrame].position != user.position)
            {
                Debug("Movement interrupted before the end!");
                mouvement.listOfTimesPassedSinceFirstFrame.RemoveAt(i);
                continue;
            }
            
            Debug("Movement in progress!");
        }
    }
_This is pseudo code; the variables names are different and some details are missing_

Depending of the approach with movement recognition, we will do different things when we detect the beginning/end/interruption/progress of a movement. It will also use the differents position recognition approach.

### Mouvement recognition: Approach 1

This method will simply debug the name of the movement when his end has been detected. Can only be adjusted with the degree of margin. It is using only the first method to position recognition. 
It has two inconvenient: it is binary, meaning that it lack precision, and it will inform the user only after the end of his movement.

### Mouvement recognition: Approach 2

This second approach is a first attempt to answer the issues raised above. It implement a system of score, that will be attributed to every movements at every frames.
It use the first score system and the first approach to position recognition in order to limit the amount computing power used.
A new list is implemented, with each index corresponding to the index of the listOfTimesPassedSinceFirstFrame. It allow us to store the score associated with each instances of movement detected.
When every instances have a score computed, we take the lowest (corresponding to the best one), and it become the general score of the movement.
This approach of movement recognition have the same problems that the first score system of the position detection (before we were removing useless nodes).

### Mouvement recognition: Approach 3

This approach is almost the same as the second one, but with the second score system for position recognition. The other difference is that, in a concern of optimization, we only compute the score every 30 frames.

### Mouvement recognition: Approach 4

This last approach uses all the improvements made in the previous methods, and use the last score system.




//TODO: rajouter des graph chiffrés pour comparer les performances (en terme de detection) des différentes méthodes.
