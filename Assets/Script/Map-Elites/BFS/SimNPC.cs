using System;
using UnityEngine;

[Serializable]
public class SimNPC
{
    public NPCDef source;

    public int row;
    public int col;

    public int dirRow;
    public int dirCol;

    public int stepsRemaining;

    public System.Random rng;

    public SimNPC(NPCDef npc)
    {
        source = npc;

        row = npc.row;
        col = npc.column;

        rng = new System.Random(npc.seed);

        InitializeDirection();
        InitializeStepCounter();
    }

    private void InitializeDirection()
    {
        switch (source.prefabIndex)
        {
            // Idle Up
            case 0:
                dirRow = -1;
                dirCol = 0;
                break;

            // Idle Down
            case 1:
                dirRow = 1;
                dirCol = 0;
                break;

            // Idle Left
            case 2:
                dirRow = 0;
                dirCol = -1;
                break;

            // Idle Right
            case 3:
                dirRow = 0;
                dirCol = 1;
                break;

            // Fixed Horizontal
            case 4:
                dirRow = 0;
                dirCol = 1;
                break;

            // Fixed Vertical
            case 5:
                dirRow = 1;
                dirCol = 0;
                break;

            // RandomStep Horizontal
            case 6:
                dirRow = 0;
                dirCol = 1;
                break;

            // RandomStep Vertical
            case 7:
                dirRow = 1;
                dirCol = 0;
                break;

            // RandomWay
            case 8:
                dirRow = 0;
                dirCol = 1;
                break;
        }
    }

    private void InitializeStepCounter()
    {
        switch (source.prefabIndex)
        {
            case 4:
            case 5:
                stepsRemaining = source.fixedSteps;
                break;

            case 6:
            case 7:
                stepsRemaining =
                    rng.Next(
                        source.minRandomSteps,
                        source.maxRandomSteps + 1);
                break;

            case 8:
                stepsRemaining =
                    rng.Next(
                        source.minRandomSteps,
                        source.maxRandomSteps + 1);
                break;

            default:
                stepsRemaining = 0;
                break;
        }
    }

    public bool IsIdle()
    {
        return source.prefabIndex <= 3;
    }

    public bool IsFixed()
    {
        return source.prefabIndex == 4 ||
               source.prefabIndex == 5;
    }

    public bool IsRandomStep()
    {
        return source.prefabIndex == 6 ||
               source.prefabIndex == 7;
    }

    public bool IsRandomWay()
    {
        return source.prefabIndex == 8;
    }

    public bool OccupiesTwoCells()
    {
        return source.prefabIndex <= 3;
    }

    public SimNPC Clone()
    {
        SimNPC copy =
            (SimNPC)MemberwiseClone();

        return copy;
    }
}