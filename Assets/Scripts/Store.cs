using UniHumanoid;
using UnityEngine;

public class Store : MonoBehaviour
{
    private bool m_usingArm = false;
    private string m_path = null;
    private Bvh m_bvh = null;
      
    public bool UsingArm
    {
        get { return m_usingArm; }
        set
        {
            if (m_usingArm == value) return;
            m_usingArm = value;
        }
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
    public Bvh Bvh
    {
        get { return m_bvh; }
        set
        {
            if (m_bvh == value) return;
            m_bvh = value;
        }
    }
    
    public void toggleUsingArm()
    {
        UsingArm = !UsingArm;
    }

    private void Update()
    {
        // TODO: Update
        if (Path != null && Bvh == null)
        {
            Bvh = new Bvh().GetBvhFromPath(Path);
        }
    }
}
