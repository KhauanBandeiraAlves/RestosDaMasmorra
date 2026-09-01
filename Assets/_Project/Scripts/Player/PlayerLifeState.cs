namespace RestosDaMasmorra.Player
{
    // Centralized life/defeat state. Only Alive/Downed/Returned are used in solo play
    // (Phase D); Reviving is reserved for the Phase E multiplayer revive flow so nothing
    // downstream has to special-case "if (multiplayer)".
    public enum PlayerLifeState
    {
        Alive,
        Downed,
        Reviving,
        Returned
    }
}
