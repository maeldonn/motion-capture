
# Table of Contents

* [Préparation du matériel](#préparation-du-matériel)
* [Axis Neuron](#axis-neuron)
  * [Installation de Axis Neuron](#installation-de-axis-neuron)
  * [Enfiler la tenue Perception Neuron](#enfiler-la-tenue-perception-neuron)
  * [Configuration de Axis Neuron](#configuration-de-axis-neuron)
  * [Obtention d’un fichier BVH sur Axis Neuron](#obtention-dun-fichier-bvh-sur-axis-neuron)
    * [Enregistrement de l'animation](#enregistrement-de-lanimation)
    * [Exportation de l'animation au format BVH](#exportation-de-lanimation-au-format-bvh)
* [Unity](#unity)
  * [Installation de la librairie de Perception Neuron](#installation-de-la-librairie-de-perception-neuron)
  * [Configuration de Unity](#configuration-de-unity)

# Préparation du matériel

Il existe trois modes d'utilisation du matériel de Perception Neuron :

 * **Le "Single arm mode"** : 9 à 11 capteurs.

* **Le "Upper body mode"** : 11 à 25 capteurs.

* **Le "Fullbody mode"** : 18 à 32 capteurs.

On pourra connecter l'équipement en filaire ou sans-fil.

# Axis Neuron

PERCEPTION NEURON va se connecter au logiciel AXIS Neuron pour l’étalonnage et la gestion du système, ainsi que l’enregistrement et l’exportation de fichiers de données pour une manipulation dans Unity.

Un certain nombre de tutoriels sont disponible [sur le site du constructeur](https://neuronmocap.com/tutorials), ainsi qu'une [FAQ](https://support.neuronmocap.com/hc/en-us). ATTENTION: certaines de ces ressources ne sont plus a jour ou concernent une ancienne version de la tenue.

## Installation de Axis Neuron

On commence par installer le logiciel Axis Neuron disponible [ici](https://neuronmocap.com/software/unity-sdk) (ou avec ce [lien directe](https://neuronmocap.com/sites/default/files/Axis_Neuron_x64_3_8_42_8591_20180411034619617000_X64.zip)).

Lancez le fichier d'installation. Suivez les instructions.

## Enfiler la tenue Perception Neuron

Nous conseillons d'enfiler la tenue dans cet ordre: jambes, tronc/tête, bras et finalement mains.
Celle-ci doit être enfilée comme suis:
//IMAGES DU MANUEL DU MANEQUIN AVEC LA TENUE.

## Configuration de Axis Neuron

Tout d'abord quand on lance l'application, un pop-up apparait nous demandant de 
connecter un équipement. On peut connecter de façon filaire ou sans-fil.

**Filaire**

Appuyez sur le bouton "Connect" pour afficher les tenues détectées.
//INSERER IMAGE DU BOUTON
Sur la ligne correspondante à la tenue que vous souaitez utiliser, appuyez sur le bouton "Connect".
//IMAGE

**Sans-fil**

// TODO : Apprendre a se servir du sans-fil, expliquer la démarche à suivre


---

Une fois l'équipement connecté, il va falloir calibrer l'équipement de motion capture. Pour cela il faut cliquer sur l'icone "Calibrate" à droite de l'écran et de suivre les instructions.
//IMAGE DU BOUTON CALIBRATE

Il est aussi possible de choisir la taille du corps de l'utilisateur dans le même menu.
//IMAGE DU MENU DEROULANT AVEC LA TAILLE A SELECTIONNER

Ensuite il va falloir se rendre dans les paramètres puis dans l'onglet "Broadcasting", on activera "Enable BVH" et on cochera "Binary". Cela permettra au logiciel d'envoyer les données lues a Unity.
//IMAGES DES DIFFERENTS MENUS

Les autres paramètres par défaut n'ont pas besoin d'être modifiés.

## Obtention d’un fichier BVH sur Axis Neuron 

### Enregistrement de l'animation

On peut enregistrer une animation sur le logiciel Axis Neuron (à condition d'avoir une tenue de connectée préalablement). Pour ce faire, il faut appuyer sur le bouton "Record":
//IMAGE DU BOUTON

On devra alors donner un nom a notre enregistrement:
//IMAGE

L'appuie sur le bouton "Ok" va lancer cet enregistrement. Pour l'arrêter, il faut réappuyer sur le bouton "Record". Un fichier .raw aura été créer. Ces fichiers sont lus par Axis Neuron. 

### Exportation de l'animation au format BVH

Pour exporter une animation au format BVH, il faut dans un premier temps charger le fichier .raw qu'on souhaite obtenir:
//IMAGE SELECTION FICHIER

On doit ensuite appuyer sur "File", puis "Export":
//IMAGE DE CA

Une nouvelle fenêtre s'affiche. Notez que l'on peut choisir les frames de début et de fin: cela peut-être utile si on souhaite isoler un mouvement dans une série d'actions. On peut aussi modifier le nom dans "File Name". Il n'est pas necessaire de modifier les autres options. Il faut appuyer sur export pour exporter.
//IMAGE DE LA FENETRE

Le fichier nouvellement créé sera situé dans le même répertoire que les fichiers .raw. Il est possible d'y accéder directement en appuyant sur le bouton suivant:
//IMAGE DU BOUTON SUR LEQUEL IL FAUT APPUYER

# Unity

## Installation de la librairie de Perception Neuron

NOTE: si on clone le projet directement, la librairie est déjà présente et il n'est alors pas nécessaire de l'importer.
Sur Unity, on importe la librairie de Perception Neuron disponible [ici](https://neuronmocap.com/software/unity-sdk) (ou en [lien direct](https://neuronmocap.com/sites/default/files/NeuronUnityIntegration0.2.18.zip)).

Choisissez Ressources> Importer le package> Packages personnalisés pour importer le SDK PerceptionNeuron pour Unity.
//IMAGE

De nombreux exemples basiques sont disponible.

La librairie est composé de plusieurs scripts : 

1.  Assets/Neuron/Scripts/Mocap/NeuronDataReaderManaged.cs Version gérée en C# pour la réception des données BVH de Axis Neuron.
 
2.  Assets/Neuron/Scripts/Mocap/NeuronConnection.cs  
    NeuronConnection gère les connexions avec Axis Neuron en utilisant NeuronDataReaderManaged.cs. Vous pouvez vous connecter à plusieurs instances d'Axis Neuron et chaque connexion sera mappée à une instance de NeuronSource.
    
3.  Assets/Neuron/Scripts/Mocap/NeuronSource.cs  
    NeuronSource gère des instances de NeuronActor avec deux dictionnaires appelés ActiveActors et SuspendedActors. NeuronSource surveille la dernière mise à jour de l'horodatage dans NeuronActor par la méthode OnUpdate et utilise un seuil pour juger si un acteur est perdu (le nombre d'acteurs dans Axis Neuron a changé ou la connexion a été complètement perdue). Lorsque cela se produit, NeuronSource ajoute ou supprime des acteurs entre les deux dictionnaires et en informe NeuronActor.
    
4.  Assets/Neuron/Scripts/Mocap/NeuronActor.cs  
    Classe de données qui permet de ne stocker que la trame de données de mouvement la plus récente, fournit également des méthodes pour analyser les données de mouvement reçues qui sont reçues comme valeurs flottantes du réseau. NeuronActor sauvegarde également les informations de la mocap et fournit des méthodes pour enregistrer les rappels lorsqu'ils ont été repris ou suspendus par NeuronSource.
    
5.  Assets/Neuron/Scripts/Mocap/NeuronInstance.cs  
    Classe de base pour toutes sortes d'instances pour la réception de données de mouvement. Héritage de UnityEngine.MonoBehaviour. NeuronInstance fournit des callbacks pour les changements d'état et la réception d'informations mocap d'une instance NeuronActor qui était liée à cette instance par des méthodes de connexion ou autres. Cette classe n'est pas destinée à être utilisée directement mais peut être héritée pour fournir des méthodes personnalisées pour appliquer des données de mouvement, gérer les changements d'état et les informations mocap.
    
6.  Assets/Neuron/Scripts/Mocap/NeuronAnimatorInstance.cs  
    Inherited from NeuronInstance. Provides custom methods to apply Neuron motion data to the transform components of the bones bound in the Unity animator component. Needs a humanoid skeleton setup to work properly.
    
7.  Assets/Neuron/Scripts/Mocap/NeuronAnimatorPhysicalReference.cs  
    Data class for initialization and cleanup of a reference skeleton used for motions based upon Unity’s rigidbody component. Used by NeuronAnimatorInstance if physics toggle is enabled.
    
8.  Assets/Neuron/Scripts/Mocap/NeuronTransformsInstance.cs  
    Inherited from NeuronInstance. Provides custom methods to apply Neuron motion data directly to transform components. Use this for non-humanoid skeletons or skeletons with more bones then the default setup used in Unity.
    
9.  Assets/Neuron/Scripts/Mocap/NeuronTransformsPhysicalReference.cs  
    Classe de données pour l'initialisation et le nettoyage d'un squelette de référence utilisé pour les mouvements basés sur le composant de corps rigide de Unity. Utilisée par NeuronTransformsInstance si le basculement physique est activé.
    
10.  Assets/Neuron/Scripts/Mocap/NeuronInstancesManager.cs  
    Pour chaque NeuronActeur, le NeuronInstancesManager conserve exactement une NeuronAnimatorInstance. Utilisé dans NeuronDebugViewer.
    
11.  Assets/Neuron/Scripts/Utilities/BoneLine.cs  
    Classe d'utilité utilisant un logiciel de rendu de lignes pour dessiner des lignes osseuses.
    
12.  Assets/Neuron/Scripts/Utilities/BoneLines.cs  
    Classe d'utilité pour l'éditeur Neuron permettant d'ajouter ou de supprimer des BoneLines.
    
13.  Assets/Neuron/Scripts/Utilities/BoneRigidbodies.cs  
    Classe d'utilitaire pour l'éditeur Neuron permettant d'ajouter ou de supprimer des corps rigides.
    
14.  Assets/Neuron/Scripts/Utilities/FPSCounter.cs Classe d'utilité pour calculer le FPS (Frames-Per-Second).

## Configuration de Unity

Une fois la librairie importée, il faut configurer le script NeuronConnection.cs et modifier les paramètres de connexion. La configuration est différente en fonction de type de connexion à l'équipement Perception Neuron.
//IMAGE

**Filaire**

// TODO : Détailler

**Sans-fil**

// TODO : Détailler

