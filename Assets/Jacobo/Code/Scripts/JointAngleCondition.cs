using UnityEngine;

[System.Serializable]
public class JointAngleCondition
{
    public enum PresetJoint
    {
        LeftArm,       // 11(Shoulder) - 13(Elbow) - 15(Wrist)
        RightArm,      // 12(Shoulder) - 14(Elbow) - 16(Wrist)
        LeftShoulder,  // 23(Hip) - 11(Shoulder) - 13(Elbow)
        RightShoulder, // 24(Hip) - 12(Shoulder) - 14(Elbow)
        LeftLeg,       // 23(Hip) - 25(Knee) - 27(Ankle)
        RightLeg,      // 24(Hip) - 26(Knee) - 28(Ankle)
        Custom
    }

    public PresetJoint jointType;
    [Tooltip("Only used if jointType is Custom")]
    public int customJointA = 11;
    [Tooltip("Middle/Vertex Joint (Only used if Custom)")]
    public int customJointB = 13;
    public int customJointC = 15;

    [Range(0f, 180f)]
    public float targetAngle = 180f;

    public void GetIndices(out int a, out int b, out int c)
    {
        switch (jointType)
        {
            case PresetJoint.LeftArm:       a = 11; b = 13; c = 15; break;
            case PresetJoint.RightArm:      a = 12; b = 14; c = 16; break;
            case PresetJoint.LeftShoulder:  a = 23; b = 11; c = 13; break;
            case PresetJoint.RightShoulder: a = 24; b = 12; c = 14; break;
            case PresetJoint.LeftLeg:       a = 23; b = 25; c = 27; break;
            case PresetJoint.RightLeg:      a = 24; b = 26; c = 28; break;
            default: a = customJointA; b = customJointB; c = customJointC; break;
        }
    }
}