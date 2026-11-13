using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    private ISession session = Session_ME.gI();
    protected static Service instance;
    private int xx;
    private int yy;
    public const sbyte KHAMNGOC = 0;
    public const sbyte LUYENNGOC = 1;
    public const sbyte GOTNGOC = 2;
    public const sbyte THAONGOC = 3;
}
