using System.Collections.Generic;
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

    // Tim 1 duong dan .asset CHUA TUNG TON TAI trong 'folder', bat dau tu
    // ten mong muon (desiredName). Neu "{folder}/{desiredName}.asset" da co
    // san (file cu tu lan export/save truoc, hoac trung voi 1 level khac
    // trong CHINH lan export nay), tu dong thu "{desiredName}_1", "_2", ...
    // cho toi khi tim duoc ten chua bi chiem - tranh ghi de am tham len file
    // cu (truoc day ExportDifficulty dung "count" rieng cho tung lan goi,
    // nen 2 lan export vao chung 1 folder rat de trung ten va mat file cu).
    //
    // usedInThisRun: tap hop cac ten DA duoc "cap phat" trong CHINH lan goi
    // export nay nhung co the CHUA kip ghi xuong dia (AssetDatabase co the
    // chua nhan dien ngay) - dam bao khong co 2 level trong cung 1 lan export
    // vo tinh nhan trung ten nhau.
    private string GetUniqueAssetPath(
        string folder,
        string desiredName,
        HashSet<string> usedInThisRun)
    {
        string candidateName = desiredName;
        string candidatePath = $"{folder}/{candidateName}.asset";

        int suffix = 1;

        while (
            usedInThisRun.Contains(candidateName) ||
            AssetDatabase.LoadAssetAtPath<LevelData>(candidatePath) != null ||
            System.IO.File.Exists(candidatePath))
        {
            candidateName = $"{desiredName}_{suffix}";
            candidatePath = $"{folder}/{candidateName}.asset";
            suffix++;
        }

        usedInThisRun.Add(candidateName);

        return candidatePath;
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

        // Neu file nguoi dung chon TRUNG voi 1 asset da co san (vi du ho bam
        // Save nhieu lan voi cung 1 ten mac dinh), tu dong tang hau to thay
        // vi de AssetDatabase.CreateAsset ghi de am tham len file cu.
        if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null)
        {
            string folder = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string desiredName = System.IO.Path.GetFileNameWithoutExtension(path);

            path = GetUniqueAssetPath(
                folder,
                desiredName,
                new HashSet<string>());
        }

        LevelData copy =
            Object.Instantiate(
                cell.level);

        // Nguoi dung co the doi ten file trong hop thoai luu -> luon dong bo
        // lai field levelName ben trong LevelData theo dung ten file THAT SU
        // se duoc dung (sau khi da tu dong tranh trung ten o tren), de asset
        // va du lieu ben trong khong bi lech nhau (ItemCollector.cs doc do
        // kho tu levelName, khong doc tu ten file).
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

        // Ghi nho cac ten DA dung trong CHINH lan export nay (xem giai thich
        // trong GetUniqueAssetPath) - tao moi 1 lan cho ca vong lap export,
        // KHONG tao lai trong moi lan lap, de cac level trong cung 1 lan
        // export cung tranh trung ten lan nhau.
        HashSet<string> usedInThisRun = new HashSet<string>();

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

                // Truoc day dung "{baseName}_{count}" voi count rieng cho
                // MOI lan goi ExportDifficulty (luon bat dau lai tu 0) - nen
                // 2 lan export vao chung 1 folder (vi du chay Generate roi
                // Export 2 lan) rat de tao ra dung 1 ten file, khien
                // AssetDatabase.CreateAsset GHI DE am tham len file cua lan
                // truoc ma khong bao loi gi ca. Gio tim ten DUY NHAT thuc su
                // (kiem tra ca file da co san tren dia lan cac ten da dung
                // trong chinh lan export nay) roi moi tao asset.
                string desiredName =
                    $"{baseName}_{count}";

                string assetPath =
                    GetUniqueAssetPath(
                        difficultyFolder,
                        desiredName,
                        usedInThisRun);

                string finalName =
                    System.IO.Path.GetFileNameWithoutExtension(assetPath);

                // Dong bo field levelName voi ten file THAT SU duoc dung ben
                // duoi (sau khi da tranh trung ten), de 2 level co ten goc
                // trung nhau van phan biet duoc qua levelName, khong chi qua
                // ten file tren o dia.
                copy.levelName = finalName;

                AssetDatabase.CreateAsset(
                    copy,
                    assetPath);

                count++;
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Exported {count} {difficulty} levels vao {difficultyFolder}");
    }
}