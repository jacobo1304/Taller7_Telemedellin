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

    [ContextMenu("Create brazoLevantado")]
    public void CreateBrazoLevantado()
    {
        PoseData brazoLevantado = ScriptableObject.CreateInstance<PoseData>();
        brazoLevantado.poseName = "brazoLevantado";
        brazoLevantado.poseImage = null;
        brazoLevantado.marginDegrees = 25f;
        brazoLevantado.conditions = new List<PoseData.JointAngleCondition>
        {
            // Brazo derecho arriba y extendido
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f },

            // Brazo izquierdo abajo y extendido
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 15f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 170f }
        };

        SavePoseAsset(brazoLevantado, "brazoLevantado");
    }

    [ContextMenu("Create portandoPizarra")]
    public void CreatePortandoPizarra()
    {
        PoseData portandoPizarra = ScriptableObject.CreateInstance<PoseData>();
        portandoPizarra.poseName = "portandoPizarra";
        portandoPizarra.poseImage = null;
        portandoPizarra.marginDegrees = 28f;
        portandoPizarra.conditions = new List<PoseData.JointAngleCondition>
        {
            // Brazos en postura de sostener objeto al frente
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 45f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 95f },

            // Piernas / pies en zancada (una más extendida, otra más flexionada)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 130f }
        };

        SavePoseAsset(portandoPizarra, "portandoPizarra");
    }

    [ContextMenu("Create Presets 01-11")]
    public void CreatePresets01To11()
    {
        CreatePose01();
        CreatePose02();
        CreatePose03();
        CreatePose04();
        CreatePose05();
        CreatePose06();
        CreatePose07();
        CreatePose08();
        CreatePose09();
        CreatePose10();
        CreatePose11();
    }

    [ContextMenu("Create Pose 01")]
    public void CreatePose01()
    {
        CreateAndSaveNumberedPose("01", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 120f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 45f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 150f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 150f }
        });
    }

    [ContextMenu("Create Pose 02")]
    public void CreatePose02()
    {
        CreateAndSaveNumberedPose("02", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 140f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 140f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 155f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 155f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 95f }
        });
    }

    [ContextMenu("Create Pose 03")]
    public void CreatePose03()
    {
        CreateAndSaveNumberedPose("03", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 20f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 120f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 95f }
        });
    }

    [ContextMenu("Create Pose 04")]
    public void CreatePose04()
    {
        CreateAndSaveNumberedPose("04", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 150f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 165f }
        });
    }

    [ContextMenu("Create Pose 05")]
    public void CreatePose05()
    {
        CreateAndSaveNumberedPose("05", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 170f }
        });
    }

    [ContextMenu("Create Pose 06")]
    public void CreatePose06()
    {
        CreateAndSaveNumberedPose("06", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 45f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 45f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 115f }
        });
    }

    [ContextMenu("Create Pose 07")]
    public void CreatePose07()
    {
        CreateAndSaveNumberedPose("07", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 160f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 120f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 95f }
        });
    }

    [ContextMenu("Create Pose 08")]
    public void CreatePose08()
    {
        CreateAndSaveNumberedPose("08", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 80f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 170f }
        });
    }

    [ContextMenu("Create Pose 09")]
    public void CreatePose09()
    {
        CreateAndSaveNumberedPose("09", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 170f }
        });
    }

    [ContextMenu("Create Pose 10")]
    public void CreatePose10()
    {
        CreateAndSaveNumberedPose("10", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 50f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 50f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 105f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 105f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 165f }
        });
    }

    [ContextMenu("Create Pose 11")]
    public void CreatePose11()
    {
        CreateAndSaveNumberedPose("11", 28f, new List<PoseData.JointAngleCondition>
        {
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 155f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 30f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 165f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftLeg, targetAngle = 135f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightLeg, targetAngle = 135f }
        });
    }

    private void CreateAndSaveNumberedPose(string numberName, float marginDegrees, List<PoseData.JointAngleCondition> conditions)
    {
        PoseData pose = ScriptableObject.CreateInstance<PoseData>();
        pose.poseName = numberName;
        pose.poseImage = null;
        pose.marginDegrees = marginDegrees;
        pose.conditions = conditions;

        SavePoseAsset(pose, numberName);
    }

    private void SavePoseAsset(PoseData pose, string name)
    {
        string path = $"Assets/Jacobo/Code/ScriptableO/PoseData/{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<PoseData>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(pose, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Pose {name} created at {path}");
    }
}