using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    // Pure, deterministic layout algorithm. Never instantiates a scene GameObject —
    // it only reads RoomDefinition/RoomSocket data straight off the prefab assets — so it
    // is cheap enough to run thousands of times for validation.
    public static class DungeonGenerator
    {
        static readonly RoomType[] MainPathTypes = { RoomType.Combat, RoomType.Corridor };
        static readonly RoomType[] BranchTypes = { RoomType.DeadEnd, RoomType.Resource, RoomType.Treasure, RoomType.Event };

        public static DungeonLayoutResult Generate(DungeonDefinition definition, int seed)
        {
            var result = new DungeonLayoutResult { Seed = seed };

            string validationError = ValidateDefinition(definition);
            if (validationError != null)
            {
                result.Success = false;
                result.FailureReason = validationError;
                return result;
            }

            System.Random rng = new System.Random(seed);
            HashSet<GameObject> nonRepeatableUsed = new HashSet<GameObject>();

            RoomDefinition entranceDef = definition.EntrancePrefab.GetComponent<RoomDefinition>();
            RoomSocket[] entranceSockets = entranceDef.GetSockets();
            if (entranceSockets.Length == 0)
            {
                result.Success = false;
                result.FailureReason = "Entrance prefab has no sockets.";
                return result;
            }

            PlacedRoom entrance = new PlacedRoom
            {
                Prefab = definition.EntrancePrefab,
                Definition = entranceDef,
                Position = Vector3.zero,
                YawDegrees = 0f,
                Depth = 0,
                IsMainPath = true
            };
            result.Rooms.Add(entrance);
            result.MainPath.Add(entrance);
            result.Entrance = entrance;
            if (!entranceDef.CanRepeat) nonRepeatableUsed.Add(definition.EntrancePrefab);

            int targetTotalRooms = rng.Next(definition.MinRooms, definition.MaxRooms + 1);
            targetTotalRooms = Math.Max(targetTotalRooms, 2);
            int mainPathBodyCount = Math.Max(0, targetTotalRooms - 2);

            PlacedRoom current = entrance;
            int currentSocketIndex = 0; // entrance's only socket
            List<(PlacedRoom room, int socketIndex)> branchFrontier = new List<(PlacedRoom, int)>();

            for (int step = 0; step < mainPathBodyCount; step++)
            {
                RoomSocket[] currentSockets = current.Definition.GetSockets();
                if (currentSocketIndex < 0 || currentSocketIndex >= currentSockets.Length)
                {
                    result.FailureReason = $"Main path stuck at depth {current.Depth}: no open socket to continue.";
                    result.Success = false;
                    return result;
                }

                RoomSocket parentSocket = currentSockets[currentSocketIndex];
                Vector3 parentSocketWorld = current.SocketWorldPosition(parentSocket);
                SocketDirection parentWorldDir = current.SocketWorldDirection(parentSocket);
                int nextDepth = current.Depth + 1;

                List<GameObject> candidates = definition.RoomPool
                    .Where(p => p != null)
                    .Where(p => MainPathTypes.Contains(p.GetComponent<RoomDefinition>().RoomType))
                    .Where(p => !nonRepeatableUsed.Contains(p))
                    .Where(p =>
                    {
                        RoomDefinition d = p.GetComponent<RoomDefinition>();
                        return nextDepth >= d.MinDepth && nextDepth <= d.MaxDepth;
                    })
                    .ToList();

                if (!TryPlaceFromCandidates(candidates, current, parentSocket, parentSocketWorld, parentWorldDir,
                        nextDepth, result.Rooms, rng, nonRepeatableUsed, out PlacedRoom placed, out int usedLocalSocketIndex))
                {
                    result.FailureReason = $"No room from the pool fit at main-path depth {nextDepth}.";
                    result.Success = false;
                    return result;
                }

                current.ConnectedSocketIndices.Add(currentSocketIndex);
                RoomSocket[] placedSockets = placed.Definition.GetSockets();
                placed.ConnectedSocketIndices.Add(usedLocalSocketIndex);
                current.Connections.Add((currentSocketIndex, placed, usedLocalSocketIndex));
                placed.Connections.Add((usedLocalSocketIndex, current, currentSocketIndex));
                placed.Depth = nextDepth;
                placed.IsMainPath = true;

                result.Rooms.Add(placed);
                result.MainPath.Add(placed);

                // Pick the continuation socket: prefer the one facing straight opposite the
                // socket we just entered through; leftover sockets become branch points.
                SocketDirection usedLocalDir = placedSockets[usedLocalSocketIndex].Direction;
                int continueIndex = -1;
                for (int i = 0; i < placedSockets.Length; i++)
                {
                    if (i == usedLocalSocketIndex) continue;
                    if (placedSockets[i].Direction == usedLocalDir.Opposite()) { continueIndex = i; break; }
                }
                if (continueIndex == -1)
                {
                    for (int i = 0; i < placedSockets.Length; i++)
                    {
                        if (i != usedLocalSocketIndex) { continueIndex = i; break; }
                    }
                }

                for (int i = 0; i < placedSockets.Length; i++)
                {
                    if (i == usedLocalSocketIndex || i == continueIndex) continue;
                    branchFrontier.Add((placed, i));
                }

                if (continueIndex == -1)
                {
                    // Single-socket room ended up on the main path (shouldn't happen given
                    // MainPathTypes always have >=2 sockets) — stop the main path here.
                    current = placed;
                    currentSocketIndex = -1;
                    break;
                }

                current = placed;
                currentSocketIndex = continueIndex;
            }

            // --- Boss ---
            RoomDefinition bossDef = definition.BossPrefab.GetComponent<RoomDefinition>();
            RoomSocket[] bossSockets = bossDef.GetSockets();
            if (bossSockets.Length == 0)
            {
                result.Success = false;
                result.FailureReason = "Boss prefab has no sockets.";
                return result;
            }

            RoomSocket[] currentSocketsForBoss = current.Definition.GetSockets();
            if (currentSocketIndex < 0 || currentSocketIndex >= currentSocketsForBoss.Length)
            {
                result.Success = false;
                result.FailureReason = "No open socket available to attach the Boss room.";
                return result;
            }

            RoomSocket bossParentSocket = currentSocketsForBoss[currentSocketIndex];
            Vector3 bossParentWorld = current.SocketWorldPosition(bossParentSocket);
            SocketDirection bossParentDir = current.SocketWorldDirection(bossParentSocket);
            int bossDepth = current.Depth + 1;

            if (!TryPlaceFromCandidates(new List<GameObject> { definition.BossPrefab }, current, bossParentSocket,
                    bossParentWorld, bossParentDir, bossDepth, result.Rooms, rng, nonRepeatableUsed,
                    out PlacedRoom bossPlaced, out int bossLocalSocketIndex))
            {
                result.Success = false;
                result.FailureReason = "Boss room does not fit at the end of the main path (overlap).";
                return result;
            }

            current.ConnectedSocketIndices.Add(currentSocketIndex);
            bossPlaced.ConnectedSocketIndices.Add(bossLocalSocketIndex);
            current.Connections.Add((currentSocketIndex, bossPlaced, bossLocalSocketIndex));
            bossPlaced.Connections.Add((bossLocalSocketIndex, current, currentSocketIndex));
            bossPlaced.Depth = bossDepth;
            bossPlaced.IsMainPath = true;
            result.Rooms.Add(bossPlaced);
            result.MainPath.Add(bossPlaced);
            result.Boss = bossPlaced;

            // --- Branches ---
            Shuffle(branchFrontier, rng);
            int branchesPlaced = 0;
            foreach ((PlacedRoom room, int socketIndex) frontierSlot in branchFrontier)
            {
                if (branchesPlaced >= definition.MaxBranches) break;
                if (result.Rooms.Count >= definition.MaxRooms) break;
                if (frontierSlot.room.ConnectedSocketIndices.Contains(frontierSlot.socketIndex)) continue;

                RoomSocket[] roomSockets = frontierSlot.room.Definition.GetSockets();
                RoomSocket parentSocket = roomSockets[frontierSlot.socketIndex];
                Vector3 parentWorld = frontierSlot.room.SocketWorldPosition(parentSocket);
                SocketDirection parentDir = frontierSlot.room.SocketWorldDirection(parentSocket);
                int branchDepth = frontierSlot.room.Depth + 1;

                List<GameObject> candidates = definition.RoomPool
                    .Where(p => p != null)
                    .Where(p => BranchTypes.Contains(p.GetComponent<RoomDefinition>().RoomType))
                    .Where(p => !nonRepeatableUsed.Contains(p))
                    .Where(p =>
                    {
                        RoomDefinition d = p.GetComponent<RoomDefinition>();
                        return branchDepth >= d.MinDepth && branchDepth <= d.MaxDepth;
                    })
                    .ToList();

                if (!TryPlaceFromCandidates(candidates, frontierSlot.room, parentSocket, parentWorld, parentDir,
                        branchDepth, result.Rooms, rng, nonRepeatableUsed, out PlacedRoom branchRoom, out int branchLocalSocketIndex))
                {
                    continue; // best-effort: skip this branch slot
                }

                frontierSlot.room.ConnectedSocketIndices.Add(frontierSlot.socketIndex);
                branchRoom.ConnectedSocketIndices.Add(branchLocalSocketIndex);
                frontierSlot.room.Connections.Add((frontierSlot.socketIndex, branchRoom, branchLocalSocketIndex));
                branchRoom.Connections.Add((branchLocalSocketIndex, frontierSlot.room, frontierSlot.socketIndex));
                branchRoom.Depth = branchDepth;
                branchRoom.IsMainPath = false;

                result.Rooms.Add(branchRoom);
                branchesPlaced++;
            }

            result.BranchCount = branchesPlaced;

            if (result.Rooms.Count < definition.MinRooms)
            {
                result.Success = false;
                result.FailureReason = $"Generated only {result.Rooms.Count} rooms, below MinRooms ({definition.MinRooms}).";
                return result;
            }

            if (!IsBossReachable(result))
            {
                result.Success = false;
                result.FailureReason = "Boss room is not reachable from the Entrance (graph check failed).";
                return result;
            }

            if (HasAnyOverlap(result.Rooms, out string overlapReason))
            {
                result.Success = false;
                result.FailureReason = overlapReason;
                return result;
            }

            result.Success = true;
            return result;
        }

        static string ValidateDefinition(DungeonDefinition definition)
        {
            if (definition == null) return "DungeonDefinition is null.";
            if (definition.EntrancePrefab == null) return "DungeonDefinition has no EntrancePrefab.";
            if (definition.BossPrefab == null) return "DungeonDefinition has no BossPrefab.";
            if (definition.EntrancePrefab.GetComponent<RoomDefinition>() == null) return "EntrancePrefab is missing a RoomDefinition component.";
            if (definition.BossPrefab.GetComponent<RoomDefinition>() == null) return "BossPrefab is missing a RoomDefinition component.";
            if (definition.RoomPool == null || definition.RoomPool.Count == 0) return "DungeonDefinition has an empty RoomPool.";
            if (definition.MinRooms < 2) return "MinRooms must be at least 2 (Entrance + Boss).";
            if (definition.MaxRooms < definition.MinRooms) return "MaxRooms must be >= MinRooms.";
            foreach (GameObject prefab in definition.RoomPool)
            {
                if (prefab == null) return "RoomPool contains a null entry.";
                if (prefab.GetComponent<RoomDefinition>() == null) return $"Room prefab '{prefab.name}' is missing a RoomDefinition component.";
            }
            return null;
        }

        static bool TryPlaceFromCandidates(
            List<GameObject> candidates,
            PlacedRoom parentRoom,
            RoomSocket parentSocket,
            Vector3 parentSocketWorld,
            SocketDirection parentWorldDir,
            int depth,
            List<PlacedRoom> existingRooms,
            System.Random rng,
            HashSet<GameObject> nonRepeatableUsed,
            out PlacedRoom placed,
            out int usedLocalSocketIndex)
        {
            List<GameObject> shuffledCandidates = new List<GameObject>(candidates);
            Shuffle(shuffledCandidates, rng);

            // Weighted priority: sort by weight but keep some randomness via shuffle above.
            shuffledCandidates = shuffledCandidates.OrderByDescending(c => c.GetComponent<RoomDefinition>().Weight * (0.5 + rng.NextDouble())).ToList();

            foreach (GameObject candidatePrefab in shuffledCandidates)
            {
                RoomDefinition candidateDef = candidatePrefab.GetComponent<RoomDefinition>();
                RoomSocket[] candidateSockets = candidateDef.GetSockets();
                List<int> socketOrder = Enumerable.Range(0, candidateSockets.Length).ToList();
                ShuffleInts(socketOrder, rng);

                foreach (int socketIndex in socketOrder)
                {
                    RoomSocket candidateSocket = candidateSockets[socketIndex];
                    SocketDirection requiredWorldDir = parentWorldDir.Opposite();
                    float yaw = Mathf.Repeat(requiredWorldDir.ToYawDegrees() - candidateSocket.Direction.ToYawDegrees(), 360f);
                    Vector3 rotatedLocal = Quaternion.AngleAxis(yaw, Vector3.up) * candidateSocket.transform.localPosition;
                    Vector3 position = parentSocketWorld - rotatedLocal;

                    PlacedRoom tentative = new PlacedRoom
                    {
                        Prefab = candidatePrefab,
                        Definition = candidateDef,
                        Position = position,
                        YawDegrees = yaw,
                        Depth = depth
                    };

                    if (OverlapsAny(tentative, existingRooms)) continue;

                    if (!candidateDef.CanRepeat) nonRepeatableUsed.Add(candidatePrefab);
                    placed = tentative;
                    usedLocalSocketIndex = socketIndex;
                    return true;
                }
            }

            placed = null;
            usedLocalSocketIndex = -1;
            return false;
        }

        public static bool OverlapsAny(PlacedRoom candidate, List<PlacedRoom> existing)
        {
            foreach (PlacedRoom other in existing)
            {
                if (RoomsOverlap(candidate, other)) return true;
            }
            return false;
        }

        public static bool RoomsOverlap(PlacedRoom a, PlacedRoom b, float epsilon = 0.1f)
        {
            Rect ra = a.WorldRect();
            Rect rb = b.WorldRect();
            ra.xMin += epsilon; ra.xMax -= epsilon; ra.yMin += epsilon; ra.yMax -= epsilon;
            rb.xMin += epsilon; rb.xMax -= epsilon; rb.yMin += epsilon; rb.yMax -= epsilon;
            return ra.Overlaps(rb);
        }

        public static bool HasAnyOverlap(List<PlacedRoom> rooms, out string reason)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (RoomsOverlap(rooms[i], rooms[j]))
                    {
                        reason = $"Rooms overlap: {rooms[i].Definition.name} at {rooms[i].Position} and {rooms[j].Definition.name} at {rooms[j].Position}.";
                        return true;
                    }
                }
            }
            reason = null;
            return false;
        }

        public static bool IsBossReachable(DungeonLayoutResult result)
        {
            if (result.Entrance == null || result.Boss == null) return false;

            HashSet<PlacedRoom> visited = new HashSet<PlacedRoom>();
            Queue<PlacedRoom> queue = new Queue<PlacedRoom>();
            queue.Enqueue(result.Entrance);
            visited.Add(result.Entrance);

            while (queue.Count > 0)
            {
                PlacedRoom room = queue.Dequeue();
                if (room == result.Boss) return true;

                foreach (var connection in room.Connections)
                {
                    if (visited.Add(connection.other)) queue.Enqueue(connection.other);
                }
            }

            return visited.Contains(result.Boss);
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static void ShuffleInts(List<int> list, System.Random rng) => Shuffle(list, rng);
    }
}
