using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using FEXNA.Map;
using FEXNA_Library;
#if DEBUG
using System.Diagnostics;
#endif

namespace FEXNA
{
    enum Unit_Passable { Clear, Blocked, PassableEventedEnemy, PassableEnemy, PassableAlly }
    enum Range_Context { Restrict_To_Playable, Restrict_to_Map, Map_Over_Edge}
    class Pathfinding
    {
        const int OFF_MAP_PENALTY_MULT = 1; // Multiplied by the cost of tiles offmap; if greater than 1, encourages units to enter the map faster

        public static int unit_distance = 0;
        static bool Ignore_Units = false;
        static bool Restrict_To_Map = true;
        static int last_id = -1;
        static Dictionary<Vector2, Unit_Passable> Unit_Locs = new Dictionary<Vector2, Unit_Passable>(); // should maybe be an array?
        static HashSet<Vector2> Doors = new HashSet<Vector2>();
        static bool Units_Ignored_Last_Time, Evented_Move_Last_Time;
        static int[,] Move_Costs = new int[0,0];
        static Random rand = new Random();

        #region Accessors
        public static bool ignore_units
        {
            set
            {
                Ignore_Units = value;
                last_id = -1;
            }
        }
        #endregion

        #region Pathfinding
        public static void reset() { last_id = -1; }

        static bool map_data_needs_updated(Game_Unit unit)
        {
            return last_id != unit.id || Ignore_Units != Units_Ignored_Last_Time || Evented_Move_Last_Time != unit.is_evented_move;
        }

        static void update_map_data(Game_Unit unit, bool through_doors = false, bool ignore_doors = false)
        {
            Unit_Locs.Clear();
            Doors.Clear();
            // Goes through all units and sets a passable tag for the tile that unit is on
            // This should probably refer to the map's unit location data instead of figuring things out itself //Yeti
            foreach (Game_Unit test_unit in Global.game_map.units.Values.Where(x => !x.is_rescued && !x.is_dead))
            {
                Vector2 loc = test_unit.pathfinding_loc;
                if (test_unit != unit && !Global.game_map.is_off_map(loc))
                {
#if DEBUG
                    if (!Global.game_system.is_interpreter_running)
                        // if the location is already occupied, problems
                        Debug.Assert(!Unit_Locs.ContainsKey(loc), "Two units share a location when trying to start pathfinding");
                    Unit_Locs[loc] = tile_unit_passability(unit, loc, test_unit);
#else
                    if (!Unit_Locs.ContainsKey(loc))
                        Unit_Locs.Add(loc, tile_unit_passability(unit, loc, test_unit));
#endif
                }
            }
            Units_Ignored_Last_Time = Ignore_Units;
            Evented_Move_Last_Time = unit.is_evented_move;
            Ignore_Units = false;
            // Doors
            if (through_doors)
                if ((unit.can_open_door() || ignore_doors) && !unit.is_player_team)
                    foreach(var door in Global.game_map.door_locations)
                        Doors.Add(door.Key);
        }

        static Unit_Passable tile_unit_passability(Game_Unit unit, Vector2 loc, Game_Unit unit_here)
        {
            if (unit.is_passable_team(unit_here))
                return Unit_Passable.PassableAlly;
            else
            {
                if (unit.is_evented_move)
                    return Unit_Passable.PassableEventedEnemy;
                else if (unit.can_pass_enemies())
                    return Unit_Passable.PassableAlly;
                if (Ignore_Units)
                    return Unit_Passable.PassableEnemy;
                return Unit_Passable.Blocked;
            }
        }

        static void reset_move_costs()
        {
            //int length = Global.game_map.width() * Global.game_map.height(); // this line does nothing? //Debug
            if (Move_Costs.GetLength(0) != Global.game_map.width ||
                    Move_Costs.GetLength(1) != Global.game_map.height)
                Move_Costs = new int[Global.game_map.width, Global.game_map.height];
            for (int y = 0; y < Move_Costs.GetLength(1); y++)
                for (int x = 0; x < Move_Costs.GetLength(0); x++)
                    Move_Costs[x, y] = -2;
        }

        public static List<Vector2> get_route(Vector2 target_loc, int mov, int id)
        {
            return get_route(target_loc, mov, id, Global.game_map.units[id].loc);
        }
        public static List<Vector2> get_route(Vector2 target_loc, int mov, int id, Vector2 loc, bool through_doors = false, bool ignore_doors = false)
        {
            reset_move_costs();
            // Prepare outside variables for pathfinding
            Game_Unit unit = Global.game_map.units[id];
            if (map_data_needs_updated(unit))
            {
                update_map_data(unit, through_doors, ignore_doors);
                last_id = id;
            }
            //Restrict_To_Map = !Global.game_map.is_off_map(loc, false) && !Global.game_map.is_off_map(target_loc, false); //Debug
            Restrict_To_Map = !Global.game_map.is_off_map(loc) && !Global.game_map.is_off_map(target_loc);
            //Prepare pathfinding variables
            Open_List open_list = new Open_List();
            Closed_List_Route closed_list = new Closed_List_Route();

            int temp_parent = -1;

            Vector2 temp_loc = Vector2.Zero;
            int temp_f = 0;
            int temp_g = 0;
            int temp_h = 0;
            bool temp_accessible = true;

            Vector2[] check_loc = new Vector2[] {
                new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };
            Vector2 test_loc;
            bool route_found = false;

            bool use_euclidean_distance = mov == -1;
            // Start pathfinding
            temp_g = 0;
            temp_h = distance(loc, target_loc, use_euclidean_distance);
            temp_f = temp_g + temp_h;
            open_list.add_item(loc, temp_parent, temp_f, temp_g, temp_accessible);
            for (; ; )
            {
                if (open_list.size <= 0)
                    break;
                Open_Item lowest_f_item = open_list.get_lowest_f_item();
                temp_loc = lowest_f_item.Loc;
                temp_parent = lowest_f_item.Parent;
                temp_f = lowest_f_item.Fcost;
                temp_g = lowest_f_item.Gcost;
                temp_accessible = lowest_f_item.Accessible;

                temp_parent = closed_list.add_item(temp_loc, temp_parent, temp_f, temp_g, temp_accessible);
                open_list.remove_open_item();
                if (temp_loc == target_loc)
                {
                    route_found = true;
                    break;
                }
                else
                {
                    for (int i = 0; i < check_loc.Length; i++)
                    {
                        test_loc = temp_loc + check_loc[i];
#if DEBUG
                        if (Global.game_map.is_off_map(test_loc, Restrict_To_Map))
                        {
                            int test = 0;
                        }
#endif
                        // If the checked location isn't the target but is off the map, and off the map is not allowed
                        if (test_loc != target_loc && Global.game_map.is_off_map(test_loc, Restrict_To_Map))
                            continue;
                        // If the location is already on the closed list
                        if (closed_list.search(test_loc) > -1)
                            continue;
                        check_tile(unit, test_loc, temp_parent, mov, target_loc, open_list, closed_list, use_euclidean_distance: use_euclidean_distance);
                    }
                }
            }
            unit_distance = 0;
            if (route_found)
            {
                unit_distance = closed_list.get_g(temp_parent) / 10;
                return closed_list.get_route(temp_parent);
            }
            return null;
        }

        public static List<Vector2> get_reverse_route(Vector2 loc, int mov, int id)
        {
            return get_reverse_route(loc, mov, id, Global.game_map.units[id].loc);
        }
        public static List<Vector2> get_reverse_route(Vector2 loc, int mov, int id, Vector2 target_loc)
        {
            reset_move_costs();
            // Prepare outside variables for pathfinding
            Game_Unit unit = Global.game_map.units[id];
            if (map_data_needs_updated(unit))
            {
                update_map_data(unit);
                last_id = id;
            }
            //Restrict_To_Map = !Global.game_map.is_off_map(loc, false) && !Global.game_map.is_off_map(target_loc, false); //Debug
            Restrict_To_Map = !Global.game_map.is_off_map(loc) && !Global.game_map.is_off_map(target_loc);
            //Prepare pathfinding variables
            Open_List open_list = new Open_List();
            Closed_List_Route closed_list = new Closed_List_Route();

            int temp_parent = -1;

            Vector2 temp_loc = Vector2.Zero;
            int temp_f = 0;
            int temp_g = 0;
            int temp_h = 0;
            bool temp_accessible = true;

            Vector2[] check_loc = new Vector2[] {
                new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };
            Vector2 test_loc;
            bool route_found = false;

            // Start pathfinding
            //temp_g = 0; //Debug
            temp_g = pathfinding_terrain_cost(unit, loc);
            temp_h = manhatten_dist(loc, target_loc);
            temp_f = temp_g + temp_h;
            open_list.add_item(loc, temp_parent, temp_f, temp_g, temp_accessible);
            for (; ; )
            {
                if (open_list.size <= 0)
                    break;
                if (Constants.Gameplay.MOVE_ARROW_WIGGLING)
                    open_list.rng_lowest_f_id();
                Open_Item lowest_f_item = open_list.get_lowest_f_item();
                temp_loc = lowest_f_item.Loc;
                temp_parent = lowest_f_item.Parent;
                temp_f = lowest_f_item.Fcost;
                temp_g = lowest_f_item.Gcost;
                temp_accessible = lowest_f_item.Accessible;

                temp_parent = closed_list.add_item(temp_loc, temp_parent, temp_f, temp_g, temp_accessible);
                open_list.remove_open_item();
#if DEBUG
                if (temp_loc == target_loc)
                {
                    throw new IndexOutOfRangeException("Target location somehow got added to closed list");
                }
#endif
                if ((temp_loc.X == target_loc.X && Math.Abs(temp_loc.Y - target_loc.Y) <= 1) ||
                    (temp_loc.Y == target_loc.Y && Math.Abs(temp_loc.X - target_loc.X) <= 1))
                {
                    route_found = true;
                    break;
                }
                else
                {
                    for (int i = 0; i < check_loc.Length; i++)
                    {
                        test_loc = temp_loc + check_loc[i];
#if DEBUG
                        if (Global.game_map.is_off_map(test_loc, Restrict_To_Map))
                        {
                            int test = 0;
                        }
#endif
                        // If the checked location isn't the target but is off the map, and off the map is not allowed
                        if (test_loc != target_loc && Global.game_map.is_off_map(test_loc, Restrict_To_Map))
                            continue;
                        // If the location is already on the closed list
                        if (closed_list.search(test_loc) > -1)
                            continue;
                        check_tile(unit, test_loc, temp_parent, mov, target_loc, open_list, closed_list);
                    }
                }
            }
            unit_distance = 0;
            if (route_found)
            {
                unit_distance = closed_list.get_g(temp_parent) / 10;
                return closed_list.get_reverse_route(temp_parent, target_loc);
            }
            return null;
        }

        public static HashSet<Vector2> get_range(Vector2 target_loc, int mov, int id)
        {
            return get_range(target_loc, mov, id, Global.game_map.units[id].loc);
        }
        public static HashSet<Vector2> get_range(Vector2 target_loc, int mov, int id, Vector2 loc)
        {
            reset_move_costs();
            // Prepare outside variables for pathfinding
            Game_Unit unit = Global.game_map.units[id];
            if (map_data_needs_updated(unit))
            {
                update_map_data(unit);
                last_id = id;
            }
            if (Global.game_map.width == 0)
                return new HashSet<Vector2>();
            //Prepare pathfinding variables
            Open_List open_list = new Open_List();
            Closed_List_Route closed_list = new Closed_List_Route();

            int temp_parent = -1;

            Vector2 temp_loc = Vector2.Zero;
            int temp_f = 0;
            int temp_g = 0;
            int temp_h = 0;
            bool temp_accessible = true;

            Vector2[] check_loc = new Vector2[] {
                new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };
            Vector2 test_loc;

            // Start pathfinding
            temp_g = 0;
            temp_h = manhatten_dist(loc, target_loc);
            temp_f = temp_g + temp_h;
            open_list.add_item(loc, temp_parent, temp_f, temp_g, temp_accessible);
            for (; ; )
            {
                if (open_list.size <= 0)
                    break;
                Open_Item lowest_f_item = open_list.get_lowest_f_item();
                temp_loc = lowest_f_item.Loc;
                temp_parent = lowest_f_item.Parent;
                temp_f = lowest_f_item.Fcost;
                temp_g = lowest_f_item.Gcost;
                temp_accessible = lowest_f_item.Accessible;

                temp_parent = closed_list.add_item(temp_loc, temp_parent, temp_f, temp_g, temp_accessible);
                open_list.remove_open_item();

                bool reverse = (rand.Next(2) == 0);
                reverse = false;
                for (int i = 0; i < check_loc.Length; i++)
                {
                    test_loc = temp_loc + check_loc[reverse ? 3 - i : i];
                    if (Global.game_map.is_off_map(test_loc))
                        continue;
                    if (closed_list.search(test_loc) > -1)
                        continue;
                    check_tile(unit, test_loc, temp_parent, mov, target_loc, open_list, closed_list);
                }
            }

            return closed_list.get_range();
        }

        public static Maybe<int> get_distance(Vector2 target_loc, int id, int mov, bool through_doors, bool ignore_doors = false)
        {
            return get_distance(target_loc, id, mov, through_doors, Global.game_map.units[id].loc, ignore_doors);
        }
        public static Maybe<int> get_distance(Vector2 target_loc, int id, int mov, bool through_doors, Vector2 loc, bool ignore_doors = false)
        {
            reset_move_costs();
            // Prepare outside variables for pathfinding
            Game_Unit unit = Global.game_map.units[id];
            if (map_data_needs_updated(unit))
            {
                update_map_data(unit, through_doors, ignore_doors);
                last_id = id;
            }
            Restrict_To_Map = !Global.game_map.is_off_map(loc, false) && !Global.game_map.is_off_map(target_loc, false);
            //Prepare pathfinding variables
            Open_List open_list = new Open_List();
            Closed_List_Route closed_list = new Closed_List_Route();

            int temp_parent = -1;

            Vector2 temp_loc = Vector2.Zero;
            int temp_f = 0;
            int temp_g = 0;
            int temp_h = 0;
            bool temp_accessible = true;

            Vector2[] check_loc = new Vector2[] {
                new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };
            Vector2 test_loc;
            bool route_found = false;

            // Start pathfinding
            temp_g = 0;
            temp_h = manhatten_dist(loc, target_loc);
            temp_f = temp_g + temp_h;
            open_list.add_item(loc, temp_parent, temp_f, temp_g, temp_accessible);
            for (; ; )
            {
                if (open_list.size <= 0)
                    break;
                Open_Item lowest_f_item = open_list.get_lowest_f_item();
                temp_loc = lowest_f_item.Loc;
                temp_parent = lowest_f_item.Parent;
                temp_f = lowest_f_item.Fcost;
                temp_g = lowest_f_item.Gcost;
                temp_accessible = lowest_f_item.Accessible;

                temp_parent = closed_list.add_item(temp_loc, temp_parent, temp_f, temp_g, temp_accessible);
                open_list.remove_open_item();
                if (temp_loc == target_loc)
                {
                    route_found = true;
                    break;
                }
                else
                {
                    bool reverse = (rand.Next(2) == 0);
                    reverse = false;
                    for (int i = 0; i < check_loc.Length; i++)
                    {
                        test_loc = temp_loc + check_loc[reverse ? 3 - i : i];
                        if (Global.game_map.is_off_map(test_loc, Restrict_To_Map))
                            continue;
                        if (closed_list.search(test_loc) > -1)
                            continue;
                        check_tile(unit, test_loc, temp_parent, mov, target_loc, open_list, closed_list);
                    }
                }
            }
            unit_distance = 0;
            if (route_found)
            {
                return closed_list.get_g(temp_parent) / 10;
            }
            return new Maybe<int>();
        }

        public static Vector2 find_open_tile(Vector2 target_loc, int id)
        {
            return find_open_tile(target_loc, id, target_loc);
        }
        public static Vector2 find_open_tile(Vector2 target_loc, int id, Vector2 loc)
        {
            reset_move_costs();
            // Prepare outside variables for pathfinding
            Game_Unit unit = Global.game_map.units[id];
            if (map_data_needs_updated(unit))
            {
                update_map_data(unit);
                last_id = id;
            }
            if (Global.game_map.width == 0)
                return Config.OFF_MAP;
            //Prepare pathfinding variables
            Open_List open_list = new Open_List();
            Closed_List_Route closed_list = new Closed_List_Route();

            int temp_parent = -1;

            Vector2 temp_loc = Vector2.Zero;
            int temp_f = 0;
            int temp_g = 0;
            int temp_h = 0;
            bool temp_accessible = true;

            Vector2[] check_loc = new Vector2[] {
                new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };
            Vector2 test_loc;
            bool route_found = false;

            // Start pathfinding
            temp_g = 0;
            temp_h = manhatten_dist(loc, target_loc);
            temp_f = temp_g + temp_h;
            open_list.add_item(loc, temp_parent, temp_f, temp_g, temp_accessible);
            for (; ; )
            {
                if (open_list.size <= 0)
                    break;
                Open_Item lowest_f_item = open_list.get_lowest_f_item();
                temp_loc = lowest_f_item.Loc;
                temp_parent = lowest_f_item.Parent;
                temp_f = lowest_f_item.Fcost;
                temp_g = lowest_f_item.Gcost;
                temp_accessible = lowest_f_item.Accessible;

                temp_parent = closed_list.add_item(temp_loc, temp_parent, temp_f, temp_g, temp_accessible);
                open_list.remove_open_item();
                if (Global.game_map.get_unit(temp_loc) == null)
                {
                    route_found = true;
                    break;
                }
                else
                {
                    bool reverse = (rand.Next(2) == 0);
                    reverse = false;
                    for (int i = 0; i < check_loc.Length; i++)
                    {
                        test_loc = temp_loc + check_loc[reverse ? 3 - i : i];
                        if (Global.game_map.is_off_map(test_loc))
                            continue;
                        if (closed_list.search(test_loc) > -1)
                            continue;
                        check_tile(unit, test_loc, temp_parent, -1, target_loc, open_list, closed_list, true);
                    }
                }
            }
            unit_distance = 0;
            if (route_found)
            {
                return temp_loc;
            }
            return Config.OFF_MAP;
        }

        private static void check_tile(
            Game_Unit unit, Vector2 loc, int parent, int mov, Vector2 target_loc,
            Open_List open_list, Closed_List_Route closed_list, bool dijkstras = false, bool use_euclidean_distance = false)
        {
            // return if terrain type of this tile doesn't have stats //Yeti
            int move_cost = pathfinding_terrain_cost(unit, loc);
            bool pass;
            int h = 0;
            if (Doors.Contains(loc) && move_cost < 0)
            {
                // I don't quite understand why I left the move cost negative for doors //Debug
                move_cost = -10;// (unit.mov + 1) * 10; //Debug
                pass = true;
                h = (unit.mov + 1) * 10;
            }
            else
                pass = passable(unit, loc, loc != target_loc) || (loc == target_loc && Global.game_map.is_off_map(target_loc, false));

            if (pass)
            {
                int g = move_cost + closed_list.get_g(parent);
                if (mov < 0 || g <= mov * 10)
                {
                    h += (dijkstras ? 0 : distance(loc, target_loc, use_euclidean_distance));
                    // If fog and AI controlled and the unit can't see this tile
                    //if (Global.game_map.fow && !unit.is_ally && !Global.game_map.fow_visibility[unit.team].Contains(loc)) //Debug
                    
                    if (Global.game_map.fow && Global.game_state.ai_active && !Global.game_map.fow_visibility[unit.team].Contains(loc))
                        // Make the tile less desirable for the unit to cross
                        h += unit.mov * 10;
                    int f = g + h;
                    int on_list = open_list.search(loc);
                    if (on_list > -1)
                    {
                        open_list.repoint(on_list, parent, f, g);
                    }
                    else
                    {
                        open_list.add_item(loc, parent, f, g, pass);
                    }
                }
            }
        }

        public static bool passable(Game_Unit unit, Vector2 loc)
        {
            return terrain_cost(unit, loc) >= 0;
        }
        protected static bool passable(Game_Unit unit, Vector2 loc, bool team)
        {
            bool pass = true;
            int move_cost = pathfinding_terrain_cost(unit, loc);
            pass &= move_cost >= 0;
            // If team matters for this test, make sure no enemy is blocking
            if (team) pass &= team_passable(unit, loc);
            return pass;
        }

        protected static bool team_passable(Game_Unit unit, Vector2 loc)
        {
            if (!Unit_Locs.Keys.Contains(loc))
                return true;
            return Unit_Locs[loc] != Unit_Passable.Blocked;
        }

        protected static int pathfinding_terrain_cost(Game_Unit unit, Vector2 loc)
        {
            // If the location is completely off the map, return the move cost
            if ((int)loc.X < 0 || (int)loc.X >= Move_Costs.GetLength(0) || (int)loc.Y < 0 || (int)loc.Y >= Move_Costs.GetLength(1))
                return terrain_cost(unit, loc);
            // If the terrain cost for this tile hasn't been cached yet
            if (Move_Costs[(int)loc.X, (int)loc.Y] == -2)
                Move_Costs[(int)loc.X, (int)loc.Y] = terrain_cost(unit, loc);
            // If unit is off map and the tile is impassable, make it passable but costly so they can always get onto the map
            if (Move_Costs[(int)loc.X, (int)loc.Y] < 0 && Global.game_map.is_off_map(unit.loc) && Global.game_map.is_off_map(loc))
            {
                return (unit.mov * 2) * 10 * OFF_MAP_PENALTY_MULT;
            }
            return Move_Costs[(int)loc.X, (int)loc.Y];
        }
        protected static int terrain_cost(Game_Unit unit, Vector2 loc)
        {
            int terr_cost = unit.move_cost(loc);
            // If this is an event move and a unit is blocking but they can be passed through, prefer to move around them if possible
            if (Unit_Locs.ContainsKey(loc) && Unit_Locs[loc] == Unit_Passable.PassableEventedEnemy)
                terr_cost += Math.Max(1, unit.mov);
            return terr_cost * 10 * (Global.game_map.is_off_map(loc) ? OFF_MAP_PENALTY_MULT : 1);
        }

        public static HashSet<Vector2> get_range_around(HashSet<Vector2> move_range, int max_range)
        {
            return get_range_around(move_range, max_range, 1);
        }
        public static HashSet<Vector2> get_range_around(HashSet<Vector2> move_range, int max_range, bool walls)
        {
            return get_range_around(move_range, max_range, 1, walls);
        }
        public static HashSet<Vector2> get_range_around(HashSet<Vector2> move_range, int max_range, int min_range)
        {
            // Where is Config.BLOCK_FIRE_THROUGH_WALLS_DEFAULT even used
            return get_range_around(move_range, max_range, min_range,
                Constants.Gameplay.BLOCK_FIRE_THROUGH_WALLS_DEFAULT);
        }
        public static HashSet<Vector2> get_range_around(HashSet<Vector2> move_range, int max_range, int min_range, bool walls)
        {
            return get_range_around(move_range, max_range, min_range, walls, Range_Context.Restrict_To_Playable);
        }
        public static HashSet<Vector2> get_range_around(HashSet<Vector2> move_range, int max_range, int min_range, bool walls, Range_Context restrict_to_playable)
        {
            // This method could use some memory and performance optimization,
            // but I don't really know where to start//Yeti

            // Also make a version of this method that accepts a single location to
            // search around, instead of a move range //Yeti

            //List<Vector2> list = new List<Vector2>(
            //    Global.game_map.width * Global.game_map.height);
            HashSet<Vector2> list = new HashSet<Vector2>();

            Vector2 target_loc = Vector2.Zero;
            if (!walls)
            {
                foreach (Vector2 loc in move_range)
                {
                    for (int i = -max_range; i <= max_range; i++)
                    {
                        target_loc.X = loc.X + i;
                        // If off map (horizontal)
                        if (restrict_to_playable == Range_Context.Map_Over_Edge)
                        {
                            if (Global.game_map.is_off_map_edge_x(target_loc.X))
                                continue;
                        }
                        else
                        {
                            if (Global.game_map.is_off_map_x(target_loc.X, restrict_to_playable == Range_Context.Restrict_To_Playable))
                                continue;
                        }
                        for (int j = -(max_range - Math.Abs(i)); j <= (max_range - Math.Abs(i)); j++)
                        {
                            if (Math.Abs(i) + Math.Abs(j) < min_range)
                                continue;
                            target_loc.Y = loc.Y + j;
                            // If off map (vertical)
                            if (restrict_to_playable == Range_Context.Map_Over_Edge)
                            {
                                if (Global.game_map.is_off_map_edge_y(target_loc.Y))
                                    continue;
                            }
                            else
                            {
                                if (Global.game_map.is_off_map_y(target_loc.Y, restrict_to_playable == Range_Context.Restrict_To_Playable))
                                    continue;
                            }
                            list.Add(target_loc);
                        }
                    }
                }
            }
            else
            {
                // Check if hitting a tile would shoot through a wall and except those tiles
                // If the shot wouldn't go through a wall but the tile is a wall, still allow it
                foreach (Vector2 loc in move_range)
                {
                    for (int i = -max_range; i <= max_range; i++)
                    {
                        target_loc.X = loc.X + i;
                        // If off map (horizontal)
                        if (restrict_to_playable == Range_Context.Map_Over_Edge)
                        {
                            if (Global.game_map.is_off_map_edge_x(target_loc.X))
                                continue;
                        }
                        else
                        {
                            if (Global.game_map.is_off_map_x(target_loc.X, restrict_to_playable == Range_Context.Restrict_To_Playable))
                                continue;
                        }
                        for (int j = -(max_range - Math.Abs(i)); j <= (max_range - Math.Abs(i)); j++)
                        {
                            if (Math.Abs(i) + Math.Abs(j) < min_range)
                                continue;
                            target_loc.Y = loc.Y + j;

                            // If off map (vertical)
                            if (restrict_to_playable == Range_Context.Map_Over_Edge)
                            {
                                if (Global.game_map.is_off_map_edge_y(target_loc.Y))
                                    continue;
                            }
                            else
                            {
                                if (Global.game_map.is_off_map_y(target_loc.Y, restrict_to_playable == Range_Context.Restrict_To_Playable))
                                    continue;
                            }
                            bool valid = clear_firing_line(loc, target_loc);
                            if (!valid)
                                continue;
                            list.Add(target_loc);
                        }
                    }
                }
            }

            return list;
            //return new HashSet<Vector2>(list); //Debug
        }

        public static bool clear_firing_line(Vector2 loc, Vector2 target_loc)
        {
            List<Vector2> line = bresenham_supercover(loc, target_loc);
            List<Vector2[]> corners = new List<Vector2[]>();
            for (int k = 0; k < line.Count - 1; k++)
            {
                // If crossing from this point to the next point goes exactly across a corner
                if ((target_loc.X > loc.X ^ target_loc.Y > loc.Y) ?
                    (line[k] + new Vector2(1, 1) == line[k + 1]) || (line[k] + new Vector2(-1, -1) == line[k + 1]) :
                    (line[k] + new Vector2(1, -1) == line[k + 1]) || (line[k] + new Vector2(-1, 1) == line[k + 1]))
                {
                    corners.Add(new Vector2[] { line[k], line[k + 1] });
                    line.RemoveAt(k);
                    line.RemoveAt(k);
                    k--;
                }
            }
            // Checks corners
            foreach (Vector2[] temp_loc in corners)
            {
                if (!Global.game_map.terrain_fire_through(temp_loc[0]) && !Global.game_map.terrain_fire_through(temp_loc[1]))
                {
                    return false;
                }
            }

            bool steep = Math.Abs(target_loc.Y - loc.Y) > Math.Abs(target_loc.X - loc.X);
            for (int k = 0; k < line.Count; k++)
            {
                // If not checking the final two points and parallel to the next point (same Y for steep vertical, same x for horizontal)
                if (k < line.Count - 1 && (steep ?
                    (line[k].Y == line[k + 1].Y) :
                    (line[k].X == line[k + 1].X)))
                {

                    // If the first point of this pair, and either the last point checked or the other point of the pair are blocked
                    if (!Global.game_map.terrain_fire_through(line[k]) &&
                        ((k > 0 && !Global.game_map.terrain_fire_through(line[k - 1])) || (!Global.game_map.terrain_fire_through(line[k + 1]))))
                    {
                        return false;
                    }
                    k++;
                }
                else
                {
                    if (line[k] != loc && line[k] != target_loc && !Global.game_map.terrain_fire_through(line[k]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public static List<Vector2> bresenham(Vector2 loc, Vector2 target_loc)
        {
            return bresenham((int)loc.X, (int)loc.Y, (int)target_loc.X, (int)target_loc.Y);
        }
        public static List<Vector2> bresenham(int x0, int y0, int x1, int y1)
        {
            List<Vector2> result = new List<Vector2>();
            if (y0 == y1)
            {
                if (x0 > x1)
                {
                    Additional_Math.swap(ref x0, ref x1);
                }
                for (int x = x0; x < x1; x++)
                    result.Add(new Vector2(x, y0));
                result.Add(new Vector2(x1, y1));
            }
            else if (x0 == x1)
            {
                if (y0 > y1)
                {
                    Additional_Math.swap(ref y0, ref y1);
                }
                for (int y = y0; y < y1; y++)
                    result.Add(new Vector2(x0, y));
                result.Add(new Vector2(x1, y1));
            }
            else
            {
                bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
                if (steep)
                {
                    Additional_Math.swap(ref x0, ref y0);
                    Additional_Math.swap(ref x1, ref y1);
                }
                if (x0 > x1)
                {
                    Additional_Math.swap(ref x0, ref x1);
                    Additional_Math.swap(ref y0, ref y1);
                }
                int delta_x = x1 - x0;
                int delta_y = (int)Math.Abs(y1 - y0);
                int error = delta_x / 2;
                int ystep = y0 < y1 ? 1 : -1;
                int y = y0;
                for (int x = x0; x <= x1; x++)
                {
                    result.Add(steep ? new Vector2(y, x) : new Vector2(x, y));
                    error -= delta_y;
                    if (error < 0)
                    {
                        y += ystep;
                        error += delta_x;
                    }
                }
            }
            return result;
        }

        public static List<Vector2> bresenham_supercover(Vector2 loc, Vector2 target_loc)
        {
            return bresenham_supercover((int)loc.X, (int)loc.Y, (int)target_loc.X, (int)target_loc.Y);
        }
        public static List<Vector2> bresenham_supercover(int x0, int y0, int x1, int y1)
        {
            Vector2 base_loc = new Vector2(x0, y0);
            List<Vector2> result = new List<Vector2>();
            if (y0 == y1)
            {
                if (x0 > x1)
                {
                    Additional_Math.swap(ref x0, ref x1);
                }
                for (int x = x0; x < x1; x++)
                    result.Add(new Vector2(x, y0));
                result.Add(new Vector2(x1, y1));
            }
            else if (x0 == x1)
            {
                if (y0 > y1)
                {
                    Additional_Math.swap(ref y0, ref y1);
                }
                for (int y = y0; y < y1; y++)
                    result.Add(new Vector2(x0, y));
                result.Add(new Vector2(x1, y1));
            }
            else
            {
                bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
                if (steep)
                {
                    Additional_Math.swap(ref x0, ref y0);
                    Additional_Math.swap(ref x1, ref y1);
                }
                if (x0 > x1)
                {
                    Additional_Math.swap(ref x0, ref x1);
                    Additional_Math.swap(ref y0, ref y1);
                }
                int delta_x = (x1 - x0), delta_x2 = delta_x * 2;
                int delta_y = (int)Math.Abs(y1 - y0), delta_y2 = delta_y * 2;
                int error = delta_x;
                int error_prev = error;
                int ystep = y0 < y1 ? 1 : -1;
                int y = y0;
                result.Add(steep ? new Vector2(y, x0) : new Vector2(x0, y));
                for (int x = x0 + 1; x <= x1; x++)
                {
                    error += delta_y2;
                    if (error > delta_x2)
                    {
                        y += ystep;
                        error -= delta_x2;
                        if (error + error_prev < delta_x2)
                            result.Add(steep ? new Vector2(y - ystep, x) : new Vector2(x, y - ystep));
                        else if (error + error_prev > delta_x2)
                            result.Add(steep ? new Vector2(y, x - 1) : new Vector2(x - 1, y));
                        else
                        {
                            result.Add(steep ? new Vector2(y, x - 1) : new Vector2(x - 1, y));
                            result.Add(steep ? new Vector2(y - ystep, x) : new Vector2(x, y - ystep));
                        }

                    }
                    result.Add(steep ? new Vector2(y, x) : new Vector2(x, y));
                    error_prev = error;
                }
                //result.Add(steep ? new Vector2(y, x1) : new Vector2(x1, y));
            }
            if (result[0] != base_loc)
                result.Reverse();
            return result;
        }

        /// <summary>
        /// Returns which locations among a move range are within range of a target location.
        /// </summary>
        /// <param name="target_loc">Target location to attempt to hit</param>
        /// <param name="move_range">Possible locations to hit from</param>
        /// <returns></returns>
        public static HashSet<Vector2> hit_from_loc(Vector2 target_loc, HashSet<Vector2> move_range, int min_range, int max_range)
        {
            return new HashSet<Vector2>(move_range.Where(loc =>
            {
                int distance = (int)(Math.Abs(loc.X - target_loc.X) + Math.Abs(loc.Y - target_loc.Y));
                return distance >= min_range && distance <= max_range;
            }));
            /*HashSet<Vector2> result = new HashSet<Vector2>(); //Debug
            int distance;
            foreach (Vector2 loc in move_range)
            {
                distance = (int)(Math.Abs(loc.X - target_loc.X) + Math.Abs(loc.Y - target_loc.Y));
                if (distance >= min_range && distance <= max_range)
                    result.Add(loc);
            }
            return result;*/
        }
        #endregion

        #region Attack Range
        public static List<int> targets_in_range(int min_range, int max_range, HashSet<Vector2> move_range, List<int> units)
        {
            // Where is Constants.Gameplay.BLOCK_FIRE_THROUGH_WALLS_DEFAULT even used
            return targets_in_range(min_range, max_range, move_range, units,
                Constants.Gameplay.BLOCK_FIRE_THROUGH_WALLS_DEFAULT);
        }
        public static List<int> targets_in_range(int min_range, int max_range, HashSet<Vector2> move_range, List<int> units, bool walls)
        {
            List<int> result = new List<int>();
            foreach (Vector2 loc in move_range)
            {
                // If all units have been checked, break
                if (units.Count == 0) break;
                // Loop through units
                int i = 0;
                while (i < units.Count)
                {
                    int id = units[i];
                    Combat_Map_Object target = Global.game_map.attackable_map_object(id);
                    int dist = manhatten_dist(loc, target.loc) / 10;
                    if (dist <= max_range && dist >= min_range && (walls ? clear_firing_line(loc, target.loc) : true))
                    {
                        result.Add(id);
                        units.Remove(id);
                    }
                    else
                        i++;
                }
            }
            return result;
        }
        #endregion

        #region Fow
        public static HashSet<Vector2> fow_sight_area(List<Fow_View_Object> viewers)
        {
            HashSet<Vector2> result = new HashSet<Vector2>();
	        //for (int i = 0; i < team.Count; i++)
            for (int i = 0; i < viewers.Count; i++)
            {
                result.UnionWith(get_range_around(new HashSet<Vector2> { viewers[i].loc },
                    viewers[i].vision(), 0, Constants.Gameplay.BLOCK_VISION_THROUGH_WALLS,
                    Range_Context.Map_Over_Edge));
            }
            //result = result.Distinct().ToList(); //ListOrEquals //HashSet
            return result;
        }
        #endregion

        protected static int distance(Vector2 loc, Vector2 target_loc, bool use_euclidean_distance = false)
        {
            if (!use_euclidean_distance)
                return manhatten_dist(loc, target_loc);
            else
                return euclidean_dist(loc, target_loc);
        }
        protected static int euclidean_dist(Vector2 loc, Vector2 target_loc)
        {
            return (int)(Math.Sqrt(Math.Pow(loc.X - target_loc.X, 2) + Math.Pow(loc.Y - target_loc.Y, 2)) * 10);
        }
        protected static int manhatten_dist(Vector2 loc, Vector2 target_loc)
        {
            return (int)(Math.Abs(loc.X - target_loc.X) + Math.Abs(loc.Y - target_loc.Y)) * 10;
        }
    }
    
    class Open_List
    {
        int Current_Id = 0;
        List<Open_Item> Items = new List<Open_Item>();
        int Size = 0;
        int Lowest_F_Id = -1;

        public int size { get { return Size; } }

        public void add_item(Vector2 loc, int parent, int fcost, int gcost, bool accessible)
        {
            Current_Id += 1;
            Size++;
            if ((size) > Items.Count)
                Items.Add(new Open_Item { Id = Current_Id, Loc = loc, Parent = parent, Fcost = fcost, Gcost = gcost, Accessible = accessible });
            else
                Items[size - 1] = new Open_Item { Id = Current_Id, Loc = loc, Parent = parent, Fcost = fcost, Gcost = gcost, Accessible = accessible };
            int i = size;
            while (i != 1)
            {
                if (fcost <= Items[i / 2 - 1].Fcost)
                {
                    Open_Item temp = Items[i / 2 - 1];
                    Items[i / 2 - 1] = Items[i - 1];
                    Items[i - 1] = temp;
                    i /= 2;
                }
                else
                    break;
            }
        }

        public void repoint(int index, int parent, int f, int g)
        {
            if (g < Items[index].Gcost)
            {
                Items[index].Parent = parent;
                Items[index].Fcost = f;
                Items[index].Gcost = g;

                int i = index + 1;
                while (i != 1)
                {
                    if (Items[i - 1].Fcost <= Items[i / 2 - 1].Fcost)
                    {
                        Open_Item temp = Items[i / 2 - 1];
                        Items[i / 2 - 1] = Items[i - 1];
                        Items[i - 1] = temp;
                        i /= 2;
                    }
                    else
                        break;
                }
            }
        }

        public void remove_open_item()
        {
            int remove_id = lowest_f_id();
            Items[remove_id] = Items[size - 1];
            Size--;
            Lowest_F_Id = -1;
            //Items.RemoveAt(Items.Count - 1);
            resort();
        }

        private void resort()
        {
            int i1 = 1;
            for (; ; )
            {
                int i2 = i1;
                if (i2 * 2 + 1 <= size)
                {
                    if (Items[i2 - 1].Fcost >= Items[i2 * 2 - 1].Fcost)
                        i1 = i2 * 2;
                    if (Items[i1 - 1].Fcost >= Items[i2 * 2].Fcost)
                        i1 = i2 * 2 + 1;
                }
                else if (i2 * 2 <= size)
                {
                    if (Items[i2 - 1].Fcost >= Items[i2 * 2 - 1].Fcost)
                        i1 = i2 * 2;
                }

                if (i1 != i2)
                {
                    Open_Item temp = Items[i2 - 1];
                    Items[i2 - 1] = Items[i1 - 1];
                    Items[i1 - 1] = temp;
                }
                else
                    break;
            }
        }

        public Open_Item get_lowest_f_item()
        {
            int id = lowest_f_id();
            return Items[id];
        }

        public void rng_lowest_f_id()
        {
            //return 0; // Hopefully the binary heap works correctly...! //Debug
            int index1 = 0, index2 = -1;
            //int min_f1 = Items[index1].Fcost, min_f2 = -1;
            for (int i = 1; i < size; i++)
            {
                //if (min_f1 > Items[i].Fcost)
                if (Items[index1].Fcost > Items[i].Fcost)
                {
                    index2 = index1;
                    //min_f2 = min_f1;
                    index1 = i;
                    //min_f1 = Items[index1].Fcost;
                }
                else if (Items[index1].Fcost == Items[i].Fcost && Items[index1].Gcost == Items[i].Gcost)
                {
                    index2 = i;
                    //min_f2 = Items[index1].Fcost;
                }
            }
            if (index1 != 0)
            {
                int test = 0;
                test++;
            }
            if (index2 != -1 && Items[index1].Gcost == Items[index2].Gcost)
            {
                if (!Global.game_system.roll_rng(50))
                    index1 = index2;
                Lowest_F_Id = index1;
            }
            else
                Lowest_F_Id = index1;
        }

        protected int lowest_f_id()
        {
            if (Lowest_F_Id != -1)
                return Lowest_F_Id;
            //return 0; // Hopefully the binary heap works correctly...! //Debug
            int index = 0;
            //int min_f = Items[index].Fcost;
            for (int i = 1; i < size; i++)
            {
                if (Items[index].Fcost > Items[i].Fcost)
                {
                    index = i;
                    //min_f = Items[index].Fcost;
                }
            }
            if (index != 0)
            {
                int test = 0;
                test++;
            }
            return index;
        }

        public int search(Vector2 loc)
        {
            for (int i = 0; i < size; i++)
            {
                if (Items[i].Loc == loc)
                    return i;
            }
            return -1;
        }
    }

    class Open_Item
    {
        public int Id;
        public Vector2 Loc;
        public int Parent;
        public int Fcost;
        public int Gcost;
        public bool Accessible;

        public override string ToString()
        {
            return "loc:(" + ((int)Loc.X) + ", " + ((int)Loc.Y) + "), order: " + Fcost / 10 + ", moved: " + Gcost / 10 + ", remaining: " + (Fcost - Gcost) / 10;
        }
    }

    class Closed_List
    {
        protected IEnumerable<Vector2> Loc;
        protected List<int> Parent = new List<int>();
        List<int> Fcost = new List<int>();
        List<int> Gcost = new List<int>();
        List<bool> Accessible = new List<bool>();
        Dictionary<Vector2, int> Locations = new Dictionary<Vector2, int>();

        #region Accessors
        protected virtual int loc_count { get { return (Loc as HashSet<Vector2>).Count; } }
        #endregion

        public Closed_List()
        {
            initialize_loc();
            //Locations = new int[Global.game_map.width() * Global.game_map.height()];
        }

        protected virtual void initialize_loc()
        {
            Loc = new HashSet<Vector2>();
        }

        protected virtual void add_loc(Vector2 loc)
        {
            (Loc as HashSet<Vector2>).Add(loc);
        }

        public int add_item(Vector2 loc, int parent, int fcost, int gcost, bool accessible)
        {
            add_loc(loc);
            Parent.Add(parent);
            Fcost.Add(fcost);
            Gcost.Add(gcost);
            Accessible.Add(accessible);
            Locations[loc] = loc_count;
            //Locations[(int)loc.X + (int)loc.Y * Global.game_map.width()] = Loc.Count;

            return loc_count - 1;
        }

        public int search(Vector2 loc)
        {
            if (!Locations.ContainsKey(loc))
                return -1;
            return Locations[loc] - 1;
            //return Locations[(int)loc.X + (int)loc.Y * Global.game_map.width()] - 1;

            //for (int i = 0; i < Loc.Count; i++)
            //{
            //    if (Loc[i] == loc) return i;
            //}
            return -1;
        }

        public virtual HashSet<Vector2> get_range()
        {
            return Loc as HashSet<Vector2>;
        }

        public int get_g(int index)
        {
            return Gcost[index];
        }
    }

    class Closed_List_Route : Closed_List
    {
        #region Accessors
        protected override int loc_count { get { return (Loc as List<Vector2>).Count; } }
        #endregion

        protected override void initialize_loc()
        {
            Loc = new List<Vector2>();
        }

        protected override void add_loc(Vector2 loc)
        {
            (Loc as List<Vector2>).Add(loc);
        }

        private Vector2 loc(int index)
        {
            return (Loc as List<Vector2>)[index];
        }

        public List<Vector2> get_route(int temp_id)
        {
            List<Vector2> list = new List<Vector2>();
            int temp_parent = Parent[temp_id];
            while (temp_parent != -1)
            {
                list.Add(loc(temp_id) - loc(temp_parent));
                temp_id = temp_parent;
                temp_parent = Parent[temp_id];
            }
            return list;
        }
        public List<Vector2> get_reverse_route(int temp_id, Vector2 starting_loc)
        {
            List<Vector2> list = new List<Vector2>();
            list.Add(this.loc(temp_id) - starting_loc);
            int temp_parent = Parent[temp_id];
            while (temp_parent != -1)
            {
                list.Insert(0, this.loc(temp_parent) - this.loc(temp_id));
                temp_id = temp_parent;
                temp_parent = Parent[temp_id];
            }
            return list;
        }

        public override HashSet<Vector2> get_range()
        {
            return new HashSet<Vector2>(Loc);
        }
    }
}