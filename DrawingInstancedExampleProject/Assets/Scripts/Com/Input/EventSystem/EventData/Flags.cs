namespace Com.Input.EventSystem
{
    [System.Flags]
    public enum MouseInputButton : byte
    {
        None = 0,
        Left = 0b001,
        Right = 0b010,
        Middle = 0b100
    }
}