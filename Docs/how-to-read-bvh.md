
# Lire et comprendre un fichier BVH

## Note
Ce texte est une traduction de la ressource disponible [ici](https://research.cs.wisc.edu/graphics/Courses/cs-838-1999/Jeff/BVH.html) 
(Le lien est aussi disponible dans la bibliographie).


### Le BVH de Biovision

Le format de fichier BVH a été développé à l'origine par Biovision, une société de services de capture de mouvements, pour fournir des données de capture de mouvements à ses clients. Le nom BVH est l'abréviation de BioVision Hierarchical data. Ce format a principalement remplacé un format antérieur qu'ils ont développé, le format BVA, qui est discuté dans la section suivante, comme moyen de fournir des informations hiérarchiques schématiques en plus des données de mouvement. Le format BVH est un excellent format général, son seul inconvénient est l'absence d'une définition complète de la pose de base (ce format n'a que des décalages translationnels des segments enfants par rapport à leur parent, aucun décalage rotationnel n'est défini), il manque également d'informations explicites sur la façon de dessiner les segments mais cela n'a aucune incidence sur la définition du mouvement.

### Analyse du fichier

Un fichier BVH se compose de deux parties, une section d'en-tête qui décrit la hiérarchie et la pose initiale du squelette, et une section de données qui contient les données de mouvement. Examinez l'exemple de fichier BVH appelé ["Example1.bvh"](https://research.cs.wisc.edu/graphics/Courses/cs-838-1999/Jeff/Example1.bvh). Le début de la section d'en-tête commence par le mot-clé "**HIERARCHY**". La ligne suivante commence par le mot-clé "**ROOT**" suivi du nom du segment racine de la hiérarchie à définir. Après avoir décrit cette hiérarchie, il est possible de définir une autre hiérarchie, qui sera également désignée par le mot-clé "**ROOT**". En principe, un fichier BVH peut contenir un nombre illimité de hiérarchies squelettes. En pratique, le nombre de segments est limité par le format de la section de mouvement, un échantillon dans le temps pour tous les segments se trouve sur une ligne de données et cela posera des problèmes aux lecteurs qui supposent une limite à la taille d'une ligne dans un fichier.

Le format BVH devient alors une définition récursive. Chaque segment de la hiérarchie contient des données qui ne concernent que ce segment, puis il définit récursivement ses enfants. La ligne qui suit le mot-clé "**ROOT**" contient un seul accolade gauche "{", l'accolade est alignée sur le mot-clé "**ROOT**". La ligne qui suit un accolade est indentée par un caractère de tabulation, ces indentations sont principalement destinées à rendre le fichier plus lisible, mais certains analyseurs de fichiers BVH attendent des tabulations, donc si vous créez un fichier BVH, veillez à ce qu'il s'agisse de tabulations et pas seulement d'espaces. La première information d'un segment est le décalage de ce segment par rapport à son parent, ou dans le cas de l'objet racine, le décalage sera généralement égal à zéro. Le décalage est spécifié par le mot-clé "**OFFSET**" suivi du décalage X,Y et Z du segment par rapport à son parent. L'information sur le décalage indique également la longueur et la direction utilisées pour dessiner le segment parent. Dans le format BVH, il n'y a pas d'information explicite sur la façon dont un segment doit être dessiné. Cette information est généralement déduite du décalage du premier enfant défini pour le parent. En règle générale, seuls les segments de la racine et du haut du corps auront plusieurs enfants.

La ligne qui suit le décalage contient les informations d'en-tête du canal. Elle contient le mot-clé "**CHANNELS**" suivi d'un nombre indiquant le nombre de chaînes, puis une liste de ces nombreuses étiquettes indiquant le type de chaque chaîne. Le lecteur de fichiers BVH doit garder une trace du nombre de chaînes et des types de chaînes rencontrés au fur et à mesure que les informations de la hiérarchie sont analysées. Plus tard, lorsque les informations de mouvement seront analysées, cet ordre sera nécessaire pour analyser chaque ligne de données de mouvement. Ce format semble avoir la souplesse nécessaire pour permettre des segments comportant un nombre quelconque de canaux qui peuvent apparaître dans n'importe quel ordre. Si vous écrivez votre analyseur pour gérer cela, tant mieux, cependant, je n'ai jamais rencontré un fichier BVH qui n'avait pas 6 canaux pour l'objet racine et 3 canaux pour chaque autre objet dans la hiérarchie.

Vous pouvez voir que l'ordre des canaux de rotation semble un peu bizarre, il va de la rotation Z, suivie de la rotation X et enfin de la rotation Y. Ce n'est pas une erreur, le format BVH utilise un ordre de rotation quelque peu inhabituel. Placez les éléments de données dans votre structure de données dans cet ordre.

Sur la ligne de données suivant la spécification des canaux, il peut y avoir l'un des deux mots-clés suivants : soit vous trouverez le mot-clé "**JOINT**", soit vous verrez le mot-clé "**End Site**". Une définition de joint est identique à la définition racine, sauf en ce qui concerne le nombre de canaux. C'est là que la récursion a lieu, le reste de l'analyse des informations de joint se déroule comme avec une racine. L'information de site final met fin à la récursion et indique que le segment actuel est un effecteur final (sans enfant). La définition du site de fin fournit une information supplémentaire, elle donne la longueur du segment précédent tout comme le décalage d'un enfant définit la longueur et la direction du segment de ses parents.

La fin de toute définition d'articulation, de site terminal ou de racine est indiquée par une accolade bouclée à droite "}". Cette accolade est alignée avec son accolade droite correspondante.

Une dernière remarque sur la hiérarchie des BVH : le repère global est défini comme un système de coordonnées à droite, l'axe Y étant le vecteur "haut" mondial. Ainsi, vous constaterez généralement que les segments de squelette des BVH sont alignés le long de l'axe Y ou de l'axe Y négatif (puisque les personnages ont souvent une pose zéro où le personnage se tient droit avec les bras tendus vers le côté).

La fin d'une articulation, d'un site d'extrémité ou d'une racine est indiquée par une accolade bouclée à droite "}". Cette accolade est alignée avec l'accolade droite correspondante.

La section de mouvement commence par le mot-clé "**MOTION**" sur une ligne à part. Cette ligne est suivie d'une ligne indiquant le nombre d'images, cette ligne utilise le mot-clé "Frames :" (les deux-points font partie du mot-clé) et un nombre indiquant le nombre d'images, ou d'échantillons de mouvement qui se trouvent dans le fichier. Sur la ligne qui suit la définition des images se trouve la définition "Frame Time :", qui indique le taux d'échantillonnage des données. Dans l'exemple de fichier BVH, le taux d'échantillonnage est de 0,033333, ce qui correspond à 30 images par seconde, soit le taux d'échantillonnage habituel dans un fichier BVH.

Le reste du fichier contient les données de mouvement réel. Chaque ligne est un échantillon de données de mouvement. Les numéros apparaissent dans l'ordre des spécifications des canaux, car la hiérarchie du squelette a été analysée.

### Interprétation des données

Pour calculer la position d'un segment, vous devez d'abord créer une matrice de transformation à partir des informations de translation et de rotation locales pour ce segment. Pour tout segment commun, les informations de translation seront simplement le décalage tel que défini dans la section de hiérarchie. Les données de rotation proviennent de la section de mouvement. Pour l'objet racine, les données de translation seront la somme des données de décalage et des données de translation de la section de mouvement. Le format BVH ne tient pas compte des échelles, il n'est donc pas nécessaire de se soucier d'inclure un calcul de facteur d'échelle.

Une façon simple de créer la matrice de rotation est de créer 3 matrices de rotation séparées, une pour chaque axe de rotation. Ensuite, concaténer les matrices de gauche à droite Y, X et Z.

	vR = vYXZ

Une autre méthode consiste à calculer directement la matrice de rotation.

L'ajout des informations de décalage est simple, il suffit de placer les données de translation X, Y et Z dans les emplacements appropriés de la matrice. Une fois que la transformation locale est créée, il faut la concaténer avec la transformation locale de son parent, puis de son grand-parent, et ainsi de suite.

	vM = vMenfantMparentMgrand-parent...
