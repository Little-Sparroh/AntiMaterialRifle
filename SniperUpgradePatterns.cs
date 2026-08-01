/// <summary>Hex footprints for Anti-Material Rifle upgrades.</summary>
public static class SniperUpgradePatterns
{
    public static HexMap Small()
    {
        HexMap map = new HexMap(2, 2);
        Set(map, 0, 0, true, 4);
        Set(map, 1, 0, true, 16);
        Set(map, 0, 1, true, 1);
        return map;
    }

    public static HexMap Line()
    {
        HexMap map = new HexMap(3, 1);
        Set(map, 0, 0, true, 4);
        Set(map, 1, 0, true, 20);
        Set(map, 2, 0, true, 16);
        return map;
    }

    public static HexMap Medium()
    {
        HexMap map = new HexMap(3, 2);
        Set(map, 0, 0, true, 8);
        Set(map, 1, 0, true, 16);
        Set(map, 2, 0, true, 0);
        Set(map, 0, 1, true, 4);
        Set(map, 1, 1, true, 2);
        return map;
    }

    public static HexMap Large()
    {
        HexMap map = new HexMap(3, 3);
        Set(map, 1, 0, true, 4);
        Set(map, 0, 1, true, 2);
        Set(map, 1, 1, true, 0);
        Set(map, 2, 1, true, 0);
        Set(map, 1, 2, true, 1);
        return map;
    }

    public static HexMap Wide()
    {
        HexMap map = new HexMap(4, 2);
        Set(map, 0, 0, true, 4);
        Set(map, 1, 0, true, 8);
        Set(map, 2, 0, true, 16);
        Set(map, 1, 1, true, 1);
        Set(map, 2, 1, true, 1);
        Set(map, 3, 1, true, 0);
        return map;
    }

    public static HexMap Exotic()
    {
        HexMap map = new HexMap(4, 3);
        Set(map, 1, 0, true, 4);
        Set(map, 2, 0, true, 16);
        Set(map, 0, 1, true, 2);
        Set(map, 1, 1, true, 0);
        Set(map, 2, 1, true, 0);
        Set(map, 3, 1, true, 0);
        Set(map, 1, 2, true, 1);
        Set(map, 2, 2, true, 1);
        return map;
    }

    /// <summary>Boundary Incursion / GridGrow — single cell.</summary>
    public static HexMap BoundaryIncursion()
    {
        HexMap map = new HexMap(1, 1);
        Set(map, 0, 0, true, 0);
        return map;
    }

    private static void Set(HexMap map, int x, int y, bool enabled, byte connections)

    {
        ref HexMap.Node n = ref map[x, y];
        n.enabled = enabled;
        n.connections = (HexMap.Direction)connections;
    }
}
