using UnityEngine;

public class enemy_network: MonoBehaviour
{
    public EC_dash[] dashes;
    public EC_shoot[] shoots;
    public EC_beh_jelly jelly;
    public EC_jump[] jumps;
    public EC_teleport tp;
    public void SetFx(Vector3Int id) // id, trigId, fxId
    {
        switch (id.x)
        {
            case 0: // dashes
                for (int i = 0; i < dashes.Length; i++)
                    if (dashes[i].trigId == id.y)
                        if (id.z == 0)
                            dashes[i].PrepareFx(false);
                        else
                            dashes[i].AttackFx(false);
                break;
            case 1: // shoots
                for (int i = 0; i < shoots.Length; i++)
                    if (shoots[i].trigId == id.y)
                        if (id.z == 0)
                            shoots[i].PrepareFx(false);
                        else
                            shoots[i].AttackFx(false);
                break;
            case 2: // jelly
                jelly.DashFx(false);
                break;
            case 3: // jump
                for (int i = 0; i < jumps.Length; i++)
                    if (jumps[i].trigId == id.y)
                        if (id.z == 0)
                            jumps[i].PrepareFx(false);
                        else
                            jumps[i].AttackFx(false);
                break;
            case 4: // tp
                tp.PrepareFx(false);
                break;
        }
    }

    public void SetTp(Vector2 from, Vector2 to)
    {
        if (tp != null)
        {
            if (from != Vector2.zero)
                tp.TeleportStartFx(false, from, to);
            else
                tp.TeleportEndFx(false, to);
        }
    }
}
