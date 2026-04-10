using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PoseGenerator : MonoBehaviour
{
    [ContextMenu("Create T-Pose")]
    public void CreateTPose()
    {
        PoseData tPose = ScriptableObject.CreateInstance<PoseData>();
        tPose.poseName = "T-Pose";
        tPose.poseImage = null; // Assign a default sprite in the editor
        tPose.conditions = new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 180f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 180f }
        };

        SavePoseAsset(tPose, "TPose");
    }

    [ContextMenu("Create A-Pose")]
    public void CreateAPose()
    {
        PoseData aPose = ScriptableObject.CreateInstance<PoseData>();
        aPose.poseName = "A-Pose";
        aPose.poseImage = null;
        aPose.conditions = new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 60f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 60f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f }
        };

        SavePoseAsset(aPose, "APose");
    }

    [ContextMenu("Create Y-Pose")]
    public void CreateYPose()
    {
        PoseData yPose = ScriptableObject.CreateInstance<PoseData>();
        yPose.poseName = "Y-Pose";
        yPose.poseImage = null;
        yPose.conditions = new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 130f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 130f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f }
        };

        SavePoseAsset(yPose, "YPose");
    }

    private void SavePoseAsset(PoseData pose, string name)
    {
        string path = $"Assets/Jacobo/Code/ScriptableO/PoseData/{name}.asset";
        AssetDatabase.CreateAsset(pose, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Pose {name} created at {path}");
    }
}