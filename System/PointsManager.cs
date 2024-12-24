using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class PointsManager : ModSystem
{
    private readonly Dictionary<Team, int> _points = new();
    private readonly Dictionary<Team, ISet<short>> _downedNpcs = new();

    public IReadOnlyDictionary<Team, int> Points => _points;
    public IReadOnlyDictionary<Team, ISet<short>> DownedNpcs => _downedNpcs;

    public override void ClearWorld()
    {
        foreach (var team in Enum.GetValues<Team>())
        {
            _points[team] = 0;
            _downedNpcs[team] = new HashSet<short>();
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        var points = new int[_points.Count];

        foreach (var (team, teamPoints) in _points)
            points[(int)team] = teamPoints;

        var downedNpcs = new int[_downedNpcs.Count][];

        foreach (var (team, downedNpc) in _downedNpcs)
            downedNpcs[(int)team] = downedNpc.Select(id => (int)id).ToArray();

        tag["points"] = points;
        tag["downedNpcs"] = downedNpcs.ToList();
    }

    public override void LoadWorldData(TagCompound tag)
    {
        var points = (int[])tag["points"];
        for (var i = 0; i < points.Length; i++)
            _points[(Team)i] = points[i];

        var downedNpcs = (List<int[]>)tag["downedNpcs"];
        for (var i = 0; i < downedNpcs.Count; i++)
            _downedNpcs[(Team)i] = downedNpcs[i].Select(id => (short)id).ToHashSet();
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(_points.Count);
        foreach (var (team, teamPoints) in _points)
        {
            writer.Write((int)team);
            writer.Write(teamPoints);
        }

        writer.Write(_downedNpcs.Count);
        foreach (var (team, downedNpcs) in _downedNpcs)
        {
            writer.Write((int)team);
            writer.Write(downedNpcs.Count);
            foreach (var id in downedNpcs)
                writer.Write(id);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        _points.Clear();
        _downedNpcs.Clear();

        var numberOfPointEntries = reader.ReadInt32();
        for (var i = 0; i < numberOfPointEntries; i++)
        {
            var team = (Team)reader.ReadInt32();
            var points = reader.ReadInt32();
            _points[team] = points;
        }

        var numberOfDownedNpcsEntries = reader.ReadInt32();
        for (var i = 0; i < numberOfDownedNpcsEntries; i++)
        {
            var team = (Team)reader.ReadInt32();
            _downedNpcs[team] = new HashSet<short>();

            var numberOfIdEntries = reader.ReadInt32();
            for (var j = 0; j < numberOfIdEntries; j++)
                _downedNpcs[team].Add(reader.ReadInt16());
        }
    }

    public void AwardNpcKillToTeam(Team team, NPC npc)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        // Is this NPC assigned custom point values?
        if (!config.Points.Npc.TryGetValue(new NPCDefinition(npc.type), out var points))
        {
            // No, but they might be a boss.
            if (!npc.boss)
                return;

            points = config.Points.Boss;
        }

        var hasBeenDownedByThisTeam = _downedNpcs[team].Contains((short)npc.type);

        // This team has already downed this NPC, and it is not repeatable, don't award any points.
        if (hasBeenDownedByThisTeam && !points.Repeatable)
            return;

        var hasBeenDownedByAnyTeam = hasBeenDownedByThisTeam ||
                                     _downedNpcs.Values.Any(downedNpcs => downedNpcs.Contains((short)npc.type));

        var pointsToAward = hasBeenDownedByAnyTeam ? points.Additional : points.First;
        _points[team] += pointsToAward;

        _downedNpcs[team].Add((short)npc.type);

        NetMessage.SendData(MessageID.WorldData);

        // FIXME: Better message and dedicated interface
        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"Team {team} killed {npc.FullName} for {pointsToAward} point(s)"), Color.White);
    }

    public void AwardPlayerKillToTeam(Team team, Player player)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        // Even if certain oddities allowed this to happen, no point exchanging would actually occur.
        if (team == (Team)player.team)
            return;

        var victimTeamPoints = _points[team];
        // Find the lowest denomination of points we can take (can't take more than the other team has!)
        var pointsToTrade = Math.Min(victimTeamPoints, config.Points.PlayerKill);

        // If they had no points, then there isn't any work to do.
        if (pointsToTrade <= 0)
            return;

        _points[team] -= pointsToTrade;
        _points[(Team)player.team] += pointsToTrade;

        NetMessage.SendData(MessageID.WorldData);

        // FIXME: Better message and dedicated interface
        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"{team} awarded {pointsToTrade} for killing {player.name}"), Color.White);
    }
}