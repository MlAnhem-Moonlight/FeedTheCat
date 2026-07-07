using UnityEngine;

public class LevelMutator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void MutateNPC(
    NPCDef npc)
    {
        npc.row =
            Random.Range(0, 8);

        npc.column =
            Random.Range(0, 6);
    }

    private void MutateGoal(
    LevelData level)
    {
        level.destinations[0] =
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6));
    }

    private void MutatePlayer(
    LevelData level)
    {
        level.playerStart =
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6));
    }

    private NPCDef CreateNPC()
    {
        NPCDef npc =
            new NPCDef();

        npc.prefabIndex =
            Random.Range(0, 9);

        npc.row =
            Random.Range(0, 8);

        npc.column =
            Random.Range(0, 6);

        npc.seed =
            Random.Range(
                int.MinValue,
                int.MaxValue);

        npc.fixedSteps = 3;

        npc.minRandomSteps = 1;
        npc.maxRandomSteps = 4;

        return npc;
    }
}
