using System.Collections.Generic;
using UnityEngine;

public class MAPElitesGenerator
{
    // Ti le sinh HOAN TOAN ngau nhien (exploration) so voi dot bien tu 1
    // elite co san trong archive (exploitation). Ap dung tu vong lap dau
    // tien archive co it nhat 1 elite. Tang gia tri nay neu muon archive
    // da dang hon; giam neu muon hoi tu nhanh quanh cac elite tot.
    //
    // Tang tu 0.2 -> 0.4: o muc 0.2, ~80% level la dot bien tu elite co san,
    // khien cac level trong cung 1 do kho de hoi tu ve vai "khuon mau" giong
    // nhau (cung tuyen duong, cung so buoc) do fitness landscape co gradient
    // ro rang keo dot bien ve cung 1 huong toi uu. Nang ti le random-init len
    // giup archive giu duoc nhieu bo cuc (vi tri player/dich/item/NPC) khac
    // nhau hon cho cung 1 khoang do kho.
    private const float RandomInitChance = 0.4f;

    // Ti le muc tieu Easy:Medium:Hard khi chon "do kho ky vong" (aspirational
    // difficulty) cho 1 candidate MOI - dung de quyet dinh so luong NPC va
    // loai NPC (qua LevelGenUtil.GetWeightedRandomPrefabIndex) ngay tu luc
    // sinh/dot bien, thay vi sinh hoan toan random roi "hy vong" BFS cham
    // diem ra dung do kho mong muon. Day la nguyen nhan chinh khien level
    // Easy truoc day van co the co toi 6-9 NPC (CreateRandomLevel cu random
    // deu, khong quan tam do kho muc tieu; con Mutate() thi luon duoc goi
    // voi target mac dinh la Medium, nen trong so Easy/Hard trong LevelGenUtil
    // gan nhu khong bao gio thuc su duoc dung).
    private const int EasyRatio = 4;
    private const int MediumRatio = 2;
    private const int HardRatio = 1;

    public MAPElitesArchive archive;

    private BFSSolver solver =
        new BFSSolver();

    private LevelMutator mutator =
        new LevelMutator();

    public MAPElitesGenerator()
    {
        // Chieu thu 3 (depth) = so luong NPC (0..MaxNpcCountDimension-1). Xem
        // giai thich chi tiet trong MAPElitesArchive.cs - day la fix chinh
        // cho van de "moi do kho deu ~9 NPC" (level it NPC va nhieu NPC gio
        // KHONG con canh tranh chung 1 o archive nua).
        archive =
            new MAPElitesArchive(
                20,
                20,
                MAPElitesArchive.MaxNpcCountDimension);
    }

    public void Run(int iterations)
    {
        int validLevels = 0;

        // Dem ly do bi loai o TUNG buoc, giup nhin ra ngan sach iterations
        // dang "mat" vao dau (vi du: da so bi loai vi khong the vua an item
        // vua thang -> co the can giam so NPC/do kho Hard, hoac tang
        // iterations; da so bi loai vi archive da co elite tot hon o cung
        // o -> archive dang bao hoa, tang iterations se it hieu qua hon la
        // tang do phan giai archive).
        int rejectedOverlap = 0;
        int rejectedUnsolvable = 0;
        int rejectedNotFullClear = 0;
        int rejectedNotBetterThanElite = 0;

        for (int i = 0; i < iterations; i++)
        {
            LevelData level =
                CreateValidCandidateLevel(i);

            if (level == null)
            {
                // Khong tao duoc candidate khong-chong-o sau nhieu lan thu -> bo qua vong lap nay
                rejectedOverlap++;
                continue;
            }

            DifficultyResultBFS d =
                solver.Evaluate(level);

            // Level chi duoc chap nhan neu: (1) toi duoc dich, VA (2) co it
            // nhat 1 duong vua nhat duoc item vua toi duoc dich. Truoc day
            // chi kiem tra (1), nen co truong hop NPC chan kin item khien
            // reward khong bao gio nhat duoc du level van "solvable" (thang
            // duoc theo duong khac, khong di qua item).
            if (!d.solvable)
            {
                rejectedUnsolvable++;
                Object.DestroyImmediate(level);
                continue;
            }

            if (!d.fullClearSolvable)
            {
                rejectedNotFullClear++;
                Object.DestroyImmediate(level);
                continue;
            }

            bool kept =
                archive.TryInsert(
                    level,
                    d);

            if (!kept)
            {
                // Level giai duoc nhung khong du tot de vao archive
                // (o cua no da co elite tot hon) -> huy de tranh ro ri bo nho.
                rejectedNotBetterThanElite++;
                Object.DestroyImmediate(level);
            }
            else
            {
                validLevels++;
            }

            if (i % 100 == 0 && i > 0)
            {
                System.GC.Collect();
            }
        }

        Debug.Log(
            $"[MAPElites] Ly do loai candidate - " +
            $"Overlap: {rejectedOverlap} | " +
            $"Unsolvable (khong toi duoc dich): {rejectedUnsolvable} | " +
            $"Khong the vua an item vua thang: {rejectedNotFullClear} | " +
            $"Da co elite tot hon o cung o: {rejectedNotBetterThanElite}");

        // Sau khi da sinh xong toan bo iterations, phan loai lai do kho theo
        // TAM PHAN VI cua diem (fullClearShortestPath + npcScore) trong chinh
        // tap level vua sinh ra. Cach nay dam bao ty le Easy/Medium/Hard luon
        // can doi (~1/3 moi loai) thay vi phu thuoc vao nguong co dinh doan
        // truoc (LevelDifficultyClassifier.EasyMaxScore/MediumMaxScore) -
        // von de bi lech neu cau hinh NPC (so luong/loai) thay doi.
        archive.RecalculateDifficulties();

        Debug.Log($"Generated {validLevels} valid levels out of {iterations} iterations");
    }

    // Luoi an toan cuoi cung: tao 1 candidate roi kiem tra CHAC CHAN khong co
    // o nao bi chong (item bi de boi o thu 2 cua NPC Idle, v.v.). Neu phat
    // hien chong o thi huy va thu lai (toi da 5 lan) thay vi de lot 1 level loi.
    private LevelData CreateValidCandidateLevel(int index)
    {
        const int maxAttempts = 5;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            LevelData level =
                CreateCandidateLevel(index);

            if (!LevelGenUtil.HasOverlap(level))
            {
                return level;
            }

            Object.DestroyImmediate(level);
        }

        return null;
    }

    // Quyet dinh: sinh moi hoan toan ngau nhien, hay lay 1 elite ngau nhien
    // tu archive roi dot bien no (mutation-based MAP-Elites).
    private LevelData CreateCandidateLevel(int index)
    {
        LevelData parent =
            archive.GetRandomElite();

        bool shouldExplore =
            parent == null ||
            Random.value < RandomInitChance;

        // Chon truoc 1 do kho ky vong (tỉ le EasyRatio:MediumRatio:HardRatio)
        // cho CA 2 nhanh - de nhanh nao cung chu dong huong toi cac muc do
        // kho khac nhau, thay vi luon ngam dinh Medium (truong hop mutation
        // truoc day) hoac hoan toan khong quan tam do kho (truong hop random
        // truoc day).
        LevelDifficulty aspirational =
            PickAspirationalDifficulty();

        if (shouldExplore)
        {
            return CreateRandomLevel(index, aspirational);
        }

        return mutator.Mutate(parent, aspirational);
    }

    private LevelDifficulty PickAspirationalDifficulty()
    {
        int total =
            EasyRatio + MediumRatio + HardRatio;

        int roll =
            Random.Range(0, total);

        if (roll < EasyRatio)
            return LevelDifficulty.Easy;

        if (roll < EasyRatio + MediumRatio)
            return LevelDifficulty.Medium;

        return LevelDifficulty.Hard;
    }

    private LevelData CreateRandomLevel(int index, LevelDifficulty aspirational)
    {
        LevelData level =
            ScriptableObject.CreateInstance<LevelData>();

        // Chi la ten tam thoi, MAPElitesArchive.TryInsert se gan tien to
        // {Easy|Medium|Hard}_ khi biet do kho thuc te cua level.
        level.levelName =
            $"Generated_{index}";

        // Tap hop tat ca cac o da bi chiem (destination, player, item, NPC)
        // de dam bao khong co gi chong len nhau khi sinh level.
        HashSet<Vector2Int> occupied =
            new HashSet<Vector2Int>();

        Vector2Int destination =
            LevelGenUtil.GetRandomCell();

        occupied.Add(destination);
        level.destinations.Add(destination);

        Vector2Int playerStart =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                destination,
                LevelGenUtil.MinPlayerGoalDistance);

        occupied.Add(playerStart);
        level.playerStart = playerStart;

        Vector2Int itemPos =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                destination,
                LevelGenUtil.MinItemGoalDistance);

        occupied.Add(itemPos);
        level.items.Add(new ItemDef
        {
            row = itemPos.x,
            column = itemPos.y,
            prefabIndex = 0
        });

        int npcCount =
            GetNpcCountForDifficulty(aspirational);

        for (int i = 0; i < npcCount; i++)
        {
            int prefabIndex =
                LevelGenUtil.GetWeightedRandomPrefabIndex(aspirational);

            if (LevelGenUtil.TryPlaceNPC(occupied, out NPCDef npc, prefabIndex))
            {
                level.npcs.Add(npc);
            }
            // Neu khong tim duoc cho trong sau nhieu lan thu, bo qua NPC nay
            // thay vi lam level bi loi (chong o).
        }

        return level;
    }

    // So luong NPC theo do kho ky vong. Giam THEM 1 lan nua so voi ban truoc
    // (Easy 2-4 / Medium 4-7 / Hard 6-9), ĐẶC BIET la Easy - vi truoc day du
    // da co trong so muc tieu, MAP-Elites van co xu huong "keo" moi o ve
    // gan sat NPC toi da (~9) BAT KE do kho du dinh, do fitness thuong rat
    // manh cho deadEndRatio (cang nhieu NPC → cang nhieu ngo cut → fitness
    // cang cao), khien cac candidate it NPC bi candidate nhieu NPC "de bep"
    // moi khi canh tranh chung 1 o archive.
    //
    // Fix goc: MAPElitesArchive gio dung THEM so luong NPC lam 1 chieu MAP-
    // Elites rieng (xem MAPElitesArchive.cs), nen level it NPC va nhieu NPC
    // KHONG CON canh tranh truc tiep voi nhau nua - moi muc NPC co "cho
    // dung" rieng trong archive. Giam khoang gia tri o day chi con dong vai
    // tro dinh huong ban dau cho generation, khong con la tuyen phong thu
    // duy nhat nhu truoc.
    private int GetNpcCountForDifficulty(LevelDifficulty target)
    {
        switch (target)
        {
            case LevelDifficulty.Easy:
                return Random.Range(1, 4); // 1-3 NPC, uu tien Idle

            case LevelDifficulty.Hard:
                return Random.Range(5, 9); // 5-8 NPC, uu tien Random Patrol/Way

            default:
                return Random.Range(3, 6); // Medium - 3-5 NPC
        }
    }
}