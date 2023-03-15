using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class RoomListHolder
{
    public List<(string name, ulong id)> RoomsData;

    public RoomListHolder(List<RoomData> roomsData)
    {
        RoomsData = roomsData.Select(x => (x.HostName, x.RoomID)).ToList();
    }
}
