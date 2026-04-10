using System.Collections.Generic;

public static class PosePresetLibrary
{
    public static List<CustomPoseDetector.CustomPoseConfig> CreateTayPresets(
        InteractionType interactionType,
        int tPoseOptionIndex,
        int aPoseOptionIndex,
        int yPoseOptionIndex,
        float holdTime,
        float marginDegrees)
    {
        var list = new List<CustomPoseDetector.CustomPoseConfig>
        {
            CreatePreset("T Pose", CustomPoseDetector.PosePresetType.TPose, interactionType, tPoseOptionIndex, holdTime, marginDegrees),
            CreatePreset("A Pose", CustomPoseDetector.PosePresetType.APose, interactionType, aPoseOptionIndex, holdTime, marginDegrees),
            CreatePreset("Y Pose", CustomPoseDetector.PosePresetType.YPose, interactionType, yPoseOptionIndex, holdTime, marginDegrees)
        };

        return list;
    }

    private static CustomPoseDetector.CustomPoseConfig CreatePreset(
        string poseName,
        CustomPoseDetector.PosePresetType presetType,
        InteractionType interactionType,
        int optionIndex,
        float holdTime,
        float marginDegrees)
    {
        return new CustomPoseDetector.CustomPoseConfig
        {
            poseName = poseName,
            interactionType = interactionType,
            selectedOptionIndex = optionIndex,
            presetType = presetType,
            holdTime = holdTime,
            marginDegrees = marginDegrees,
            conditions = BuildConditionsForPreset(presetType)
        };
    }

    private static List<CustomPoseDetector.JointAngleCondition> BuildConditionsForPreset(CustomPoseDetector.PosePresetType presetType)
    {
        var conditions = new List<CustomPoseDetector.JointAngleCondition>
        {
            new CustomPoseDetector.JointAngleCondition
            {
                jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.LeftArm,
                targetAngle = 172f
            },
            new CustomPoseDetector.JointAngleCondition
            {
                jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.RightArm,
                targetAngle = 172f
            },
        };

        switch (presetType)
        {
            case CustomPoseDetector.PosePresetType.TPose:
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 92f
                });
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 92f
                });
                break;

            case CustomPoseDetector.PosePresetType.APose:
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 48f
                });
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 48f
                });
                break;

            case CustomPoseDetector.PosePresetType.YPose:
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.LeftShoulder,
                    targetAngle = 136f
                });
                conditions.Add(new CustomPoseDetector.JointAngleCondition
                {
                    jointType = CustomPoseDetector.JointAngleCondition.PresetJoint.RightShoulder,
                    targetAngle = 136f
                });
                break;
        }

        return conditions;
    }
}
