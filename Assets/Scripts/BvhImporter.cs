using UnityEngine;
using System.IO;
using System.Text;


namespace UniHumanoid
{    public class BvhImporter : MonoBehaviour
    {
        public string m_path;
        private Bvh m_bvh;

        public BvhImporter(string path)
        {
            m_path = path;
        }

        public string BvhPath
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
            }
        }

        public Bvh Bvh
        {
            get { return m_bvh; }
            set
            {
                if (m_bvh == value) return;
                m_bvh = value;
            }
        }

        public void Parse()
        {
            Parse(Application.dataPath + "/BVH/" + BvhPath);
        }

        public void Parse(string path)
        {
            //BvhPath = path;
            Bvh = Bvh.Parse(File.ReadAllText(path, Encoding.UTF8));
        }

        public Bvh GetBvh()
        {
            Parse();
            return Bvh;
        }
    }
}