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
            // Nota: En pantalla/cámara espejada (mirror horizontal) el "brazo derecho" se detecta como el lado izquierdo.
            // Además: solo importa el brazo levantado; el otro brazo se ignora completamente.
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f }
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
            // Matriz nueva (fila 1, col 1): brazos en "V" (tipo Y)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 130f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 130f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f }
        });
    }

    [ContextMenu("Create Pose 02")]
    public void CreatePose02()
    {
        CreateAndSaveNumberedPose("02", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 1, col 2): brazos arriba (más vertical)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f }
        });
    }

    [ContextMenu("Create Pose 03")]
    public void CreatePose03()
    {
        CreateAndSaveNumberedPose("03", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 1, col 3): manos en la cabeza (codos hacia afuera)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 75f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 75f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 55f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 55f }
        });
    }

    [ContextMenu("Create Pose 04")]
    public void CreatePose04()
    {
        CreateAndSaveNumberedPose("04", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 1, col 4): gesto de "no sé" (brazos abiertos + codos flexionados)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 95f }
        });
    }

    [ContextMenu("Create Pose 05")]
    public void CreatePose05()
    {
        CreateAndSaveNumberedPose("05", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva: "EsCine" (última pose de la imagen) — brazos arriba en gesto de celebración.
            // Solo brazos/hombros (sin piernas/pies). Pose simétrica (no requiere mirror).
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 150f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 150f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 135f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 135f }
        });
    }

    [ContextMenu("Create Pose 06")]
    public void CreatePose06()
    {
        CreateAndSaveNumberedPose("06", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 2, col 2): apuntando lateral (unilateral) + otro brazo flexionado
            // Nota mirror: si en el arte el brazo levantado/apuntando es "derecho", aquí se usa lado izquierdo.
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 90f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 180f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 30f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 90f }
        });
    }

    [ContextMenu("Create Pose 07")]
    public void CreatePose07()
    {
        CreateAndSaveNumberedPose("07", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 2, col 3): "enfoque preciso" (una mano al frente + otra recogida)
            // Se mantiene como pose levemente asimétrica, sin depender de piernas.
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 60f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 95f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 25f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 120f }
        });
    }

    [ContextMenu("Create Pose 08")]
    public void CreatePose08()
    {
        CreateAndSaveNumberedPose("08", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 2, col 4): manos en la cabeza (variante)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 85f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 85f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 45f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 45f }
        });
    }

    [ContextMenu("Create Pose 09")]
    public void CreatePose09()
    {
        CreateAndSaveNumberedPose("09", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 3, col 1): "gran escena" (brazos arriba pero flexionados)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 120f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 120f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 75f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 75f }
        });
    }

    [ContextMenu("Create Pose 10")]
    public void CreatePose10()
    {
        CreateAndSaveNumberedPose("10", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 3, col 2): "micrófono al suelo" (brazos hacia abajo, extendidos)
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 15f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 15f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 175f }
        });
    }

    [ContextMenu("Create Pose 11")]
    public void CreatePose11()
    {
        CreateAndSaveNumberedPose("11", 28f, new List<PoseData.JointAngleCondition>
        {
            // Matriz nueva (fila 3, col 3): "cantante" (unilateral: un brazo arriba + el otro flexionado)
            // Nota mirror: si el arte muestra el brazo derecho arriba, aquí se usa el izquierdo.
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftShoulder, targetAngle = 170f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.LeftArm, targetAngle = 175f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightShoulder, targetAngle = 70f },
            new PoseData.JointAngleCondition { jointType = PoseData.JointAngleCondition.PresetJoint.RightArm, targetAngle = 55f }
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