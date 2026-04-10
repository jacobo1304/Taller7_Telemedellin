using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class PosePresetLibrary
{
    public static void CreateTayPresets(
        InteractionType interactionType,
        int tPoseOptionIndex,
        int aPoseOptionIndex,
        int yPoseOptionIndex,
        float holdTime,
        float marginDegrees)
    {
        CreatePreset("T Pose", CustomPoseDetector.PosePresetType.TPose, "TPose");
        CreatePreset("A Pose", CustomPoseDetector.PosePresetType.APose, "APose");
        CreatePreset("Y Pose", CustomPoseDetector.PosePresetType.YPose, "YPose");
    }

    private static void CreatePreset(
        string poseName,
        CustomPoseDetector.PosePresetType presetType,
        string assetName)
    {
        PoseData poseData = ScriptableObject.CreateInstance<PoseData>();
        poseData.poseName = poseName;
        poseData.poseImage = null; // Assign sprite in editor
        poseData.conditions = BuildConditionsForPreset(presetType);

        string path = $"Assets/Jacobo/Code/ScriptableO/PoseData/{assetName}.asset";
        AssetDatabase.CreateAsset(poseData, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Pose {assetName} created at {path}");
    }

    private static List<PoseData.JointAngleCondition> BuildConditionsForPreset(CustomPoseDetector.PosePresetType presetType)
    {
        var conditions = new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition
            {
                jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm,
                targetAngle = 172f
            },
            new PoseData.JointAngleCondition
            {
                jointType = PoseData.JointAngleCondition.PresetJoint.RightArm,
                targetAngle = 172f
            },
        };

        switch (presetType)
        {
            case CustomPoseDetector.PosePresetType.TPose:
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 92f
                });
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 92f
                });
                break;

            case CustomPoseDetector.PosePresetType.APose:
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 48f
                });
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 48f
                });
                break;

            case CustomPoseDetector.PosePresetType.YPose:
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 136f
                });
                conditions.Add(new PoseData.JointAngleCondition
                {
                    jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 136f
                });
                break;
        }

        return conditions;
    }
}
