namespace SchoolHorror
{
    public enum TileType
    {
        Wall,
        CorridorFloor,
        RoomFloor,
        Podium,
        Desk,
        TeacherDesk,
        Blackboard,
        Door,
        Gate,
        Window,       // chặn đường đi nhưng nhìn/chiếu sáng xuyên qua được
        BrokenWall,   // mảng tường sập: đi qua được
        CorridorProp  // tủ locker, máy bán nước hành lang: vật cản cố định
    }
}
