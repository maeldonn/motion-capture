# Importer un fichier BVH

## Manipulation

Il suffit d'avoir un fichier au format bvh et de le déplacer dans le dossier BVH/ à la racine du projet.
Ensuite il faut attacher le script BvhImporter.cs à un gameobject et remplir le champ Path avec le nom du fichier ainsi que son extension.  

## Principe

Des scripts vont traiter le fichier et l'importer. Pas de soucis, le fichier sera bien traité, avec beaucoup de tendresse.

* **BvhImporter.cs**

	Ce script se charge de récupérer le fichier dans une string.

* **Bvh.cs**

	Ce script se charge de traiter des données BVH à partir d'un fichier et de retourner un objet BVH.
