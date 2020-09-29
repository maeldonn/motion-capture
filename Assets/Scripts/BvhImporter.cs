using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniHumanoid
{    public class BvhImporter : MonoBehaviour
    {
        public string m_path;
        string Source; // WARNING: Do not set public !!!!
        public Bvh Bvh;

        private void Start()
        {
            Parse();
        }

        public string Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
            }
        }

        public void Parse()
        {
            // TODO: Import path in Unity
            Parse(Application.dataPath+"/BVH/DaiJumpAroundChar00.bvh");
        }

        public void Parse(string path)
        {
            Path = path;
            Source = File.ReadAllText(Path, Encoding.UTF8);
            Bvh = Bvh.Parse(Source);
        }

        public Bvh GetBvh()
        {
            Parse();
            return Bvh;
        }
    }
}