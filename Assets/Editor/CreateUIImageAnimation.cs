using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class UIAnimationCreator : EditorWindow
{
    private DefaultAsset spriteFolder;
    private Image targetImage;
    private int fps = 30;

    [MenuItem("Tools/UI Animation Creator")]
    static void Open()
    {
        GetWindow<UIAnimationCreator>("UI Animation Creator");
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        spriteFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Sprite Folder",
            spriteFolder,
            typeof(DefaultAsset),
            false);

        targetImage = (Image)EditorGUILayout.ObjectField(
            "Target Image",
            targetImage,
            typeof(Image),
            true);

        fps = EditorGUILayout.IntSlider("FPS", fps, 5, 60);

        GUILayout.Space(10);

        if (GUILayout.Button("CREATE ANIMATION", GUILayout.Height(40)))
        {
            CreateAnimation();
        }
    }

    void CreateAnimation()
    {
        if (spriteFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Chọn thư mục Sprite.", "OK");
            return;
        }

        if (targetImage == null)
        {
            EditorUtility.DisplayDialog("Error", "Chọn Image trong Hierarchy.", "OK");
            return;
        }

        string folder = AssetDatabase.GetAssetPath(spriteFolder);

        var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy Sprite.", "OK");
            return;
        }

        string clipPath = folder + "/" + targetImage.name + ".anim";
        string controllerPath = folder + "/" + targetImage.name + ".controller";

        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        var binding = new EditorCurveBinding
        {
            type = typeof(Image),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys =
            new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)fps,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            binding,
            keys);

        AssetDatabase.CreateAsset(clip, clipPath);

        SerializedObject so = new SerializedObject(clip);
        so.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = true;
        so.ApplyModifiedProperties();

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddMotion(clip);

        Animator animator = targetImage.GetComponent<Animator>();

        if (animator == null)
            animator = targetImage.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Done",
            "Animation đã được tạo!",
            "OK");
    }
}