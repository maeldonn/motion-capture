using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NeuronTransformsInstanceVR))]
public class NeuronTransformsInstanceVREditor : Editor 
{
	public override void OnInspectorGUI()
	{
		NeuronTransformsInstanceVR vrScript = (NeuronTransformsInstanceVR)target;


		DrawDefaultInspector ();


		if(GUILayout.Button("Load bone references"))
		{
			Debug.Log ("[NeuronTransformsInstanceVR] - LOAD all Transform references into the bones list!");

			vrScript.Bind( vrScript.root, vrScript.prefix );
			vrScript.editorScriptHasLoadedBones = true;

			EditorUtility.SetDirty (vrScript);
		}

		if(GUILayout.Button("Clear bone references"))
		{
			Debug.Log ("[NeuronTransformsInstanceVR] - CLEAR all Transform references in the bones list!");

			for (int i=0; i < vrScript.bones.Length; i++){
				vrScript.bones[i] = null;
			}

			vrScript.editorScriptHasLoadedBones = false;

			EditorUtility.SetDirty (vrScript);
		}



	}
}