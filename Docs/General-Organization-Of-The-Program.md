# General Organization Of The Program

## Introduction

In unity, each scene represents a level. A scene is composed of a number of GameObject, which are all the elements that are present. They range from light source to 3d objects and UI elements.
The behavior of these gameObjects is defined by their components. Components are usually scripts, but can also be shaders, colliders or particle effects.
What we are interested in are the scripts we have made and put on different gameObject, and how we link them together.

When a script is meant to be attached to a gameObject (acting as a component), the class that are inside must inherit from [MonoBehavior](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html), the base class from which every Unity script derives.
It grants access to some useful methods, such as the "Start" and "Update", which are respectively called when the scene is launched and at every frame. But, for the sake of clarity, we choose to limit as much as possible the instance of such classes.

Indeed, having multiple scripts with routine makes it more difficult for a developer to proofread the code. So, we've decided to centralize as much as the code execution, with a single class acting as a "main". This class is the "GameManager".

Another problem to take care of is the access to certain objects or variables in the differents scripts.
For example, many scripts need the "player" gameObject: the usual method in unity is to make the variable accessible in the script, and then drag and drop the gameObject in the inspector of unity. But it becomes a bit redundant if many objects needs it, and the developer must know the script very well to avoid forgetting an instance of the object.
To remedy this problem, we have chosen to put all the variable that the developer needs to drag and drop on only on class, the gameManager. Then, every other class will only call the gameManager and ask for such variables.
Moreover, some variables are needed in many other classes, but aren't specified by the player: for theses we have a class "Store".

Finally, another important note about the organisation of the program is our focus on the ease of use. This translates into our choice to limit to the maximum the number of scripts the developer must add to the scene for it to work.

## Schematic

//TODO: the script organization might still change, will do later

## Role of each classes

### GameManager

The GameManger is the entry point of our program. It contains all the variables the developer must specify, takes care of the initialization of the different scripts and their updates. 

### Store

The Store contains some of the general variables.

### Menu Manager

Manage the menu on the UI.

### Mvt Recognition

Contains the methods to recognize movements. Also manage the different states of the program ("Entrainement", "Reconnaissance de mouvement").

### Pointing Handler

Manage the inputs that are related to the menu or the keyboard. Handle the pointing and the selecting actions.

### Neuron Animator

Exist in two version: the basic version of Perception Neuron, which allow the program to communicate with the Axis Neuron software, and the tweaked one, which can be used to make a dummy play an animation from a bvh file.
