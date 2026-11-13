using System.Collections.Generic;
using NsoServer.World;
using NsoServer.Net;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>Headless client movement executor. A policy requests a destination, never sets player coordinates.
    /// The way there is planned over the platforms (BotNavigation: walk, jump, double jump, drop) and executed with the
    /// same client frames the plan simulated. Like the real client (Char.sendMove) the character moves locally every
    /// frame and only reports its position as an ordinary PLAYER_MOVE at the client cadence (MoveSendMilliseconds),
    /// so the other players' clients receive the same sparse move points a human produces and walk the bot smoothly
    /// (Char.moveTo / vMovePoints: 24 one-pixel points a second made the remote character turn back and forth).</summary>
    internal sealed class BotMovement
    {
        private object zone;
        private int targetX, targetY, velocityY;
        // simX/simY: where the client character is (moved every frame); sentX/sentY: the last position the server got
        private int simX, simY, sentX, sentY, lastX, lastY;
        private long requestedAt, sentAt;
        private bool active, airJumped, wasGrounded = true;
        // planned path (BotNavigation) and the edge being executed
        private List<BotNavigation.Edge> path;
        private int pathIndex, planX = int.MinValue, planY, edgeFrames;
        private bool onEdge, reachable = true;
        private long plannedAt;
        public void Release()
        {
            requestedAt = 0;
        }

        /// <summary>Reports the client position now if it moved since the last report: any other action (attack, pick
        /// up, change map, NPC) is judged by the server at the position the client really has.</summary>
        public void Flush(BotClient client, PlayerState me, long now)
        {
            if (me == null || me.IsDead || me.IsChangeMap || zone != me.Zone || me.X != sentX || me.Y != sentY)
                return;
            if (simX == sentX && simY == sentY)
                return;
            client.SendMovementFrame(simX, simY);
            sentX = simX;
            sentY = simY;
            sentAt = now;
        }

        /// <summary>True when the last plan found no way to (x, y) (the AI should choose another target).</summary>
        public bool CannotReach(int x, int y)
        {
            return !reachable && Math.Abs(planX - x) <= 24 && (y < 0 || planY < 0 || Math.Abs(planY - y) <= 30);
        }

        public void Request(PlayerState me, int x, int y, long now)
        {
            if (me == null || me.Zone == null || me.IsDead || me.IsChangeMap)
                return;
            if (zone != me.Zone || sentX != me.X || sentY != me.Y)
                Reset(me);
            targetX = x;
            targetY = y;
            requestedAt = now;
            active = true;
        }

        // the server position is the truth again (start, teleport, refused move, zone change)
        private void Reset(PlayerState me)
        {
            velocityY = 0;
            airJumped = false;
            onEdge = false;
            path = null;
            wasGrounded = true;
            simX = sentX = lastX = me.X;
            simY = sentY = lastY = me.Y;
            zone = me.Zone;
        }

        public void Step(BotClient client, PlayerState me, GameWorld world, long now)
        {
            if (!active)
                return;
            if (me == null || me.IsDead || me.IsChangeMap || zone != me.Zone || me.X != sentX || me.Y != sentY)
            {
                active = false;
                velocityY = 0;
                onEdge = false;
                path = null;
                return;
            } // teleport, correction, death or zone change cancels stale input

            var grid = world.GetGrid(me.Zone.Tile);
            bool held = now - requestedAt <= 650; // intent renewed by the unchanged 500 ms policy tick
            int speed = Math.Max(0, Math.Min(me.Speed, AgentConfig.MoveStepPx / 12));
            int x = simX, y = simY;
            bool grounded = TileMotion.Supported(grid, x, y) && velocityY == 0;
            if (onEdge)
                FollowEdge(grid, ref x, ref y, speed);
            else if (grounded && held && speed > 0 && FollowPath(grid, ref x, ref y, speed, now))
            {
            }
            else
                Approach(grid, ref x, ref y, held ? speed : 0, grounded);
            bool still = x == simX && y == simY;
            bool landed = !wasGrounded && TileMotion.Supported(grid, x, y) && velocityY == 0;
            wasGrounded = TileMotion.Supported(grid, x, y) && velocityY == 0;
            lastX = simX;
            lastY = simY;
            simX = x;
            simY = y;
            bool stopping = !held && !onEdge && wasGrounded;
            // Char.sendMove: at most every 250 ms while moving; also when the character stops or lands, so the server
            // (attack range, pickups, waypoints) and the other clients get the resting position without delay
            if ((simX != sentX || simY != sentY) && (now - sentAt >= AgentConfig.MoveSendMilliseconds || still || landed || stopping))
            {
                client.SendMovementFrame(simX, simY);
                sentX = simX;
                sentY = simY;
                sentAt = now;
            }

            if (stopping && simX == sentX && simY == sentY)
                active = false;
        }

        // Starts the next planned edge; plans first when the target moved. False = no edge to follow (arrived next to
        // the target, or no better position exists): the final approach takes over.
        private bool FollowPath(TileGrid grid, ref int x, ref int y, int speed, long now)
        {
            bool targetMoved = Math.Abs(targetX - planX) > 24 || (targetY >= 0) != (planY >= 0) || (targetY >= 0 && Math.Abs(targetY - planY) > 30);
            // flat walking edges already passed (walking straight on along the same floor)
            while (path != null && pathIndex < path.Count && Flat(path[pathIndex]) && path[pathIndex].FromY == y && (path[pathIndex].ToX - x) * path[pathIndex].Dir <= 0)
                pathIndex++;
            bool lost = path != null && pathIndex < path.Count && Lost(path[pathIndex], x, y);
            if (targetMoved || lost || path == null)
            {
                // a failed search is not repeated for the same target for a while (the AI picks another one meanwhile)
                if (!targetMoved && !lost && path == null && !reachable && now - plannedAt < 3000)
                    return false;
                if (!BotNavigation.Standable(grid, BotNavigation.Centre(x), y))
                    return false;
                var found = BotNavigation.Find(grid, x, y, targetX, targetY, speed, out reachable);
                if (found == null)
                {
                    reachable = true;
                    return false;
                } // the map graph is still being built: walk straight

                planX = targetX;
                planY = targetY;
                plannedAt = now;
                path = found;
                pathIndex = 0;
                while (pathIndex < path.Count && Flat(path[pathIndex]) && (path[pathIndex].ToX - x) * path[pathIndex].Dir <= 0)
                    pathIndex++;
            }

            if (path == null || pathIndex >= path.Count)
                return false;
            var e = path[pathIndex];
            if (Flat(e))
            {
                // Walking along a floor: straight on from where the character is, never back to the cell centre first
                // (the planned walk edges end 1 px past the next centre, so aligning stepped back every tile).
                int dx = Math.Max(-speed, Math.Min(speed, e.ToX - x));
                BotNavigation.Frame(grid, ref x, ref y, ref velocityY, ref airJumped, dx, false);
                if (x == simX && y == simY)
                    path = null; // blocked: plan again
                else if (x == e.ToX && y == e.ToY)
                    pathIndex++;
                return true;
            }

            if (x != e.FromX)
            {
                // The same jump/drop from here instead of from the centre, when it lands in the same cell: no step back.
                BotNavigation.Edge here;
                if (BotNavigation.Simulate(grid, x, y, e.Dir, e.Kind, speed, out here) && BotNavigation.Centre(here.ToX) == BotNavigation.Centre(e.ToX) && here.ToY == e.ToY)
                {
                    path[pathIndex] = here;
                    e = here;
                }
                else
                {
                    // walk to the centre of the cell, where the planned edge was simulated from
                    int dx = Math.Max(-speed, Math.Min(speed, e.FromX - x));
                    BotNavigation.Frame(grid, ref x, ref y, ref velocityY, ref airJumped, dx, false);
                    if (x == simX && y == simY)
                        path = null; // cannot reach the centre (a wall inside the cell)
                    return true;
                }
            }

            onEdge = true;
            edgeFrames = 0;
            airJumped = false;
            velocityY = e.Kind == BotNavigation.Walk ? 0 : TileMotion.JumpVelocity;
            FollowEdge(grid, ref x, ref y, speed);
            return true;
        }

        // a walk that stays on the same floor (walking off a ledge is executed exactly as planned instead)
        private static bool Flat(BotNavigation.Edge e)
        {
            return e.Kind == BotNavigation.Walk && e.ToY == e.FromY;
        }

        private static bool Lost(BotNavigation.Edge e, int x, int y)
        {
            if (Flat(e))
                return y != e.FromY || (x - e.FromX) * e.Dir < -24;
            return BotNavigation.Centre(e.FromX) != BotNavigation.Centre(x) || e.FromY != y;
        }

        // One frame of the current edge, exactly as BotNavigation.Simulate ran it.
        private void FollowEdge(TileGrid grid, ref int x, ref int y, int speed)
        {
            var e = path[pathIndex];
            int px = x;
            BotNavigation.Frame(grid, ref x, ref y, ref velocityY, ref airJumped, e.Dir * speed, e.Kind == BotNavigation.DoubleJump);
            edgeFrames++;
            bool grounded = TileMotion.Supported(grid, x, y) && velocityY == 0;
            bool finished = grounded && !(e.Kind == BotNavigation.Walk && Math.Abs(x - e.FromX) < 24 && y == e.FromY && x != px);
            if (!finished && edgeFrames <= BotNavigation.MaxFrames)
                return;
            onEdge = false;
            if (x == e.ToX && y == e.ToY)
                pathIndex++;
            else
                path = null; // the world disagreed (speed changed, another frame interfered): plan again
        }

        // Final approach on the same platform (and the movement when no plan applies): walk towards the target, jump
        // at a wall or towards a higher target, double jump while rising when that is still not enough.
        private void Approach(TileGrid grid, ref int x, ref int y, int speed, bool grounded)
        {
            int dx = Math.Max(-speed, Math.Min(speed, targetX - x));
            int ty = targetY >= 0 ? targetY : y;
            bool wallAhead = dx != 0 && TileMotion.Horizontal(grid, x, y, Math.Sign(dx) * (speed + TileMotion.HalfWidth)) != x + Math.Sign(dx) * (speed + TileMotion.HalfWidth);
            // With the map graph the planner decides every jump; here only the last few pixels are walked (jumping at an
            // unreachable higher target made the bots hop on the spot). Blind jumps only while the graph is being built.
            bool blind = speed > 0 && !BotNavigation.Ready(grid, speed);
            bool wantUp = blind && speed > 0 && (ty < y - 8 || wallAhead);
            if (grounded && wantUp)
            {
                velocityY = TileMotion.JumpVelocity;
                airJumped = false;
            }
            else if (grounded)
                airJumped = false;
            BotNavigation.Frame(grid, ref x, ref y, ref velocityY, ref airJumped, dx, wantUp);
        }
    }
}
