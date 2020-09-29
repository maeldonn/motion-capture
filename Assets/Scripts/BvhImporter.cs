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

        private void Start()
        {
            Parse();
        }

        public String m_path;
        public String Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
            }
        }
        /*public */String Source; // source
        public Bvh Bvh;

        public void Parse()
        {
            Parse(Application.dataPath+"/"+"BVH"+"/" + "DaiJumpAroundChar00.bvh");
        }

        public void Parse(string path)
        {
            Path = path;
            Source = File.ReadAllText(Path, Encoding.UTF8);
            Bvh = Bvh.Parse(Source);
        }
    }
}