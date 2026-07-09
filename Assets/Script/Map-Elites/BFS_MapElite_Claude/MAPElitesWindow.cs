using UnityEditor;
using UnityEngine;

public class MAPElitesWindow : EditorWindow
{
    private int iterations = 5000;

    private MAPElitesGenerator generator;

    [MenuItem("FeedTheCat/MAP-Elites Generator BFS Claude")]
    public static void Open()
    {
        GetWindow<MAPElitesWindow>(
            "MAP-Elites");
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Feed The Cat Generator",
            EditorStyles.boldLabel);

        iterations =
            EditorGUILayout.IntField(
                "Iterations",
                iterations);

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Generate"))
        {
            Generate();
        }

        GUILayout.Space(20);

        if (generator != null)
        {
            DrawArchive();
        }
        GUILayout.Space(20);

        if (generator != null)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Export Easy"))
            {
                ExportDifficulty(
                    LevelDifficulty.Easy);
            }

            if (GUILayout.Button("Export Medium"))
            {
                ExportDifficulty(
                    LevelDifficulty.Medium);
            }

            if (GUILayout.Button("Export Hard"))
            {
                ExportDifficulty(
                    LevelDifficulty.Hard);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        // Reclassify Difficulties button is always visible (no generator check needed)
        // Phan loai lai Easy/Medium/Hard theo tam phan vi (khong can chay
        // lai BFS) - dung khi ban muon xem lai phan bo hoac archive duoc
        // giu tu truoc do.
        if (GUILayout.Button("Reclassify Difficulties"))
        {
            if (generator != null && generator.archive != null)
            {
                generator.archive.RecalculateDifficulties();
                Debug.Log("Da phan loai lai do kho cho toan bo archive.");
            }
            else
            {
                Debug.LogWarning("MAPElitesWindow: No archive loaded. Run 'Generate' first or load an archive.");
            }
        }
    }

    private void Generate()
    {
        generator =
            new MAPElitesGenerator();

        generator.Run(
            iterations);

        Debug.Log(
            "Generation Complete");
    }

    private void DrawArchive()
    {
        GUILayout.Label(
            "Archive");

        for (
            int y = generator.archive.height - 1;
            y >= 0;
            y--)
        {
            GUILayout.BeginHorizontal();

            for (
                int x = 0;
                x < generator.archive.width;
                x++)
            {
                EliteCellBFS cell =
                    generator.archive.Get(x, y);
                if (!cell.occupied)
                {
                    GUI.backgroundColor =
                        Color.gray;
                }
                else
                {
                    switch (cell.difficulty)
                    {
                        case LevelDifficulty.Easy:
                            GUI.backgroundColor =
                                Color.green;
                            break;

                        case LevelDifficulty.Medium:
                            GUI.backgroundColor =
                                Color.yellow;
                            break;

                        case LevelDifficulty.Hard:
                            GUI.backgroundColor =
                                Color.red;
                            break;
                    }
                }

                if (
                    GUILayout.Button(
                        "",
                        GUILayout.Width(20),
                        GUILayout.Height(20)))
                {
                    if (cell.occupied)
                    {
                        SaveLevel(cell);
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor =
            Color.white;
    }

    private void SaveLevel(
        EliteCellBFS cell)
    {
        // levelName da duoc MAPElitesArchive.TryInsert dat theo dinh dang
        // {DoKho}_{TenLevel}, dung lam ten file mac dinh cho dong bo.
        string defaultName =
            string.IsNullOrEmpty(cell.level.levelName)
                ? "GeneratedLevel"
                : cell.level.levelName;

        string path =
            EditorUtility.SaveFilePanelInProject(
                "Save Level",
                defaultName,
                "asset",
                "");

        if (string.IsNullOrEmpty(path))
            return;

        LevelData copy =
            Object.Instantiate(
                cell.level);

        // Nguoi dung co the doi ten file trong hop thoai luu -> luon dong bo
        // lai field levelName ben trong LevelData theo dung ten file that su,
        // de asset va du lieu ben trong khong bi lech nhau (ItemCollector.cs
        // doc do kho tu levelName, khong doc tu ten file).
        copy.levelName =
            System.IO.Path.GetFileNameWithoutExtension(path);

        AssetDatabase.CreateAsset(
            copy,
            path);

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Saved: " + path);
    }

    private void ExportDifficulty(
    LevelDifficulty difficulty)
    {
        string folder =
            EditorUtility.OpenFolderPanel(
                "Save Levels",
                "Assets",
                "");

        if (string.IsNullOrEmpty(folder))
            return;

        folder =
            FileUtil.GetProjectRelativePath(
                folder);

        // Tao 1 thu muc con rieng theo ten do kho (vi du "Assets/Levels/Easy")
        // de cac lan export khac nhau (Easy/Medium/Hard) khong bao gio bi lan
        // lon file voi nhau khi nhin trong Project window, ke ca khi ban
        // export nhieu lan vao chung 1 thu muc goc.
        string difficultyFolder =
            $"{folder}/{difficulty}";

        if (!AssetDatabase.IsValidFolder(difficultyFolder))
        {
            AssetDatabase.CreateFolder(
                folder,
                difficulty.ToString());
        }

        int count = 0;

        for (int x = 0; x < generator.archive.width; x++)
        {
            for (int y = 0; y < generator.archive.height; y++)
            {
                EliteCellBFS cell =
                    generator.archive.Get(x, y);

                if (!cell.occupied)
                    continue;

                if (cell.difficulty != difficulty)
                    continue;

                LevelData copy =
                    Object.Instantiate(
                        cell.level);

                string baseName =
                    string.IsNullOrEmpty(cell.level.levelName)
                        ? "Level"
                        : cell.level.levelName;

                string finalName =
                    $"{baseName}_{count}";

                // Dong bo field levelName voi ten file that su duoc dung ben
                // duoi, de 2 level co ten goc trung nhau (vi du 2 ban dot bien
                // deu ten "Hard_Mutant") van phan biet duoc qua levelName,
                // khong chi qua ten file tren o dia.
                copy.levelName = finalName;

                AssetDatabase.CreateAsset(
                    copy,
                    $"{difficultyFolder}/{finalName}.asset");

                count++;
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Exported {count} {difficulty} levels vao {difficultyFolder}");
    }
}