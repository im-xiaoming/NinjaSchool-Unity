// Offline presentation policy shared by GameScr's item panels. No input or gameplay dependencies.
// -1 means return to the owning title/tab; horizontal navigation does not wrap into another cell.
public static class TabGridNavigation
{
    public enum Direction { Left, Right, Up, Down }
    public const int Title = -1;

    public static int Move(int index, int count, int columns, Direction direction)
    {
        if (count <= 0 || columns <= 0) return Title;
        if (index < 0 || index >= count) index = 0;
        switch (direction)
        {
            case Direction.Left: return index == 0 ? Title : index - 1;
            case Direction.Right: return index == count - 1 ? Title : index + 1;
            case Direction.Up: return index < columns ? Title : index - columns;
            case Direction.Down: return index + columns < count ? index + columns : index;
            default: return index;
        }
    }

    // Preserve the character equipment panel's irregular slot arrangement (GameScr.updateKeyUIInforMe).
    private static readonly int[] EquipmentUp = { Title, Title, 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 9 };
    private static readonly int[] EquipmentDown = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 11, 12, 13, 14, 15, 15 };

    public static int MoveEquipment(int index, Direction direction)
    {
        if (index < 0 || index >= EquipmentUp.Length) index = 0;
        if (direction == Direction.Up) return EquipmentUp[index];
        if (direction == Direction.Down) return EquipmentDown[index];
        return Move(index, EquipmentUp.Length, 2, direction);
    }
}
