using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neuron
{
    public static class AliceMotionBindHelper 
    {

        public static int Bind(Transform root, Transform[] bones, string prefix = "")
        {
            int foundCount = 0;
            bool shouldExactlyMatch = !string.IsNullOrEmpty(prefix);
            for(int i = 0; i < (int)Neuron.TranceTransformsInstance.InertialBones.NumOfBones; i++)
            {
                string enumName = Enum.GetName(typeof(Neuron.TranceTransformsInstance.InertialBones), (Neuron.TranceTransformsInstance.InertialBones)i);
                if (shouldExactlyMatch)
                    enumName = prefix + enumName;
                Transform foundTrans = FindChild(root, enumName, shouldExactlyMatch);
                if(foundTrans == null)
                {
                    if(enumName.StartsWith("RightInHand"))
                    {
                        enumName = enumName.Replace("RightInHand", "RightHand");
                    }
                    if ( enumName.StartsWith("LeftInHand"))
                    {
                        enumName = enumName.Replace("LeftInHand", "LeftHand");
                    }
                    foundTrans = FindChild(root, enumName, shouldExactlyMatch);
                }
                if (foundTrans != null)
                {
                    foundCount++;
                    if (bones[i] == null)
                        bones[i] = foundTrans;
                }
                else
                {
                    if (bones[i] != null)
                        foundCount++;
                    else
                        Debug.LogWarningFormat("can't find {0} bone under {1}", enumName, root.name);
                }
            }
            return foundCount;
        }


        static Transform FindChild(Transform father, string name, bool shouldExactlyMatch)
        {
            Transform trans = null;
            int childCount = father.childCount;

            if (shouldExactlyMatch)
            {
                if (father.name == name)
                {
                    trans = father;
                    return father;
                } 

            }
            else
            {
                if (father.name.EndsWith(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    trans = father;
                    return father;
                }
            }


            for (int i = 0; i < childCount; i++)
            {
                trans = FindChild(father.GetChild(i), name, shouldExactlyMatch);
                if (trans != null)
                    break;
            }
            return trans;
        }
    }
}
