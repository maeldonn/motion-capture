using UnityEngine;

public class Arm : MonoBehaviour
{
    [SerializeField]
    private bool m_usingArm = false;

    public bool UsingArm
    {
        get { return m_usingArm; }
        set
        {
            if (m_usingArm == value) return;
            m_usingArm = value;
        }
    }

    public void toggleUsingArm()
    {
        UsingArm = !UsingArm;
    }
}
