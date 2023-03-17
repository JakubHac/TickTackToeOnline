using System;

[Serializable]
public class MoveHolder
{
    public readonly int x;
    public readonly int y;
    public readonly ulong RoomID;

    public MoveHolder(int x, int y, ulong roomID)
    {
        this.x = x;
        this.y = y;
        RoomID = roomID;
    }
}
