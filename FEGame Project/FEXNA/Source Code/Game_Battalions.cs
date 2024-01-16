using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FEXNA.Map;
using FEXNA_Library;
using ListExtension;
using FEXNADictionaryExtension;
using FEXNAVersionExtension;

namespace FEXNA
{
    public class Game_Battalions
    {
        protected int Current_Battalion = -1;
        Dictionary<int, Battalion> Data = new Dictionary<int, Battalion>();
        List<int> Individual_Animations = new List<int>();
        Dictionary<int, Game_Convoy> Convoys = new Dictionary<int, Game_Convoy>();

        #region Serialization
        public void write(BinaryWriter writer)
        {
            writer.Write(Current_Battalion);
            writer.Write(Data.Count);
            Data.write(writer);
            //for (int i = 0; i < Data.Count; i++) //Debug
            //{
            //    Data[i].write(writer);
            //}
            Individual_Animations.write(writer);
            Convoys.write(writer);
        }

        public void read(BinaryReader reader)
        {
            Current_Battalion = reader.ReadInt32();
            int count = reader.ReadInt32();
            if (Global.LOADED_VERSION.older_than(0, 4, 4, 1))
            {
                List<Battalion> data = new List<Battalion>();
                for (int i = 0; i < count; i++)
                    data.Add(Battalion.read(reader));
                Data = Enumerable.Range(0, data.Count).Select(x => new KeyValuePair<int, Battalion>(x, data[x]))
                    .ToDictionary(p => p.Key, p => p.Value);
            }
            else
                Data.read(reader);
            Individual_Animations.read(reader);
            if (Global.LOADED_VERSION.older_than(0, 4, 6, 0))
            {
                Dictionary<int, List<Item_Data>> convoys = new Dictionary<int, List<Item_Data>>();
                convoys.read(reader);
                Convoys = new Dictionary<int, Game_Convoy>(convoys.Select(p =>
                {
                    Game_Convoy convoy = new Game_Convoy();
                    convoy.Data = p.Value;
                    return new KeyValuePair<int, Game_Convoy>(p.Key, convoy);
                }).ToDictionary(p => p.Key, p => p.Value));
            }
            else
                Convoys.read(reader);
        }
        #endregion

        #region Accessors
        internal Battalion this[int index]
        {
            get
            {
                return Data[index];
            }
        }

        public int current_battalion
        {
            get { return Current_Battalion; }
            set
            {
                if (Data.Any())
                    Current_Battalion = Math.Max(Data.Keys.Min(),
                        Math.Min(value, Data.Keys.Max()));
            }
        }

        public Battalion battalion
        {
            get
            {
                if (!Data.ContainsKey(Current_Battalion))
                    return null;
                return Data[Current_Battalion];
            }
        }

        public List<Item_Data> active_convoy_data { get { return Convoys[battalion.convoy_id].Data; } }
        internal Shop_Data active_convoy_shop { get { return contains_convoy(battalion.convoy_id) ? Convoys[battalion.convoy_id].Shop : null; } }

        public List<int> individual_animations { get { return Individual_Animations; } }

        //public Dictionary<int, List<Item_Data>> convoys { get { return Convoys; } } //Debug
        internal HashSet<int> all_actor_ids { get { return new HashSet<int>(Data.SelectMany(x => x.Value.actors)); } }
        #endregion

        public Game_Battalions()
        {
            //add_battalion(0);
            Current_Battalion = -1;
            int actors = Global.data_actors.Keys.Max();
            for (int i = 0; i <= actors; i++)
                Individual_Animations.Add((int)Animation_Modes.Full);
        }
        public Game_Battalions(Game_Battalions battalions)
        {
            Current_Battalion = battalions.Current_Battalion;
            Data = battalions.Data
                .Select(pair => new KeyValuePair<int, Battalion>(pair.Key, new Battalion(pair.Value)))
                .ToDictionary(p => p.Key, p => p.Value);
            Individual_Animations = new List<int>(battalions.Individual_Animations);
            Convoys = battalions.Convoys
                .Select(pair => new KeyValuePair<int, Game_Convoy>(pair.Key, new Game_Convoy(pair.Value)))
                .ToDictionary(p => p.Key, p => p.Value);
            //Convoys = battalions.Convoys.Select(pair => new KeyValuePair<int, List<Item_Data>>(pair.Key, //Debug
            //    new List<Item_Data>(pair.Value.Select(x => new Item_Data(x)))))
            //    .ToDictionary(p => p.Key, p => p.Value);
        }

        internal void add_battalion(int index)
        {
            if (Data.ContainsKey(index))
            {
#if DEBUG
                throw new ArgumentException(string.Format(
                    "Tried to add battalion index {0}, but it already exists!",
                    index));
#endif
                return;
            }
            Data.Add(index, new Battalion());
        }

        internal bool ContainsKey(int key)
        {
            return Data.ContainsKey(key);
        }

        internal void correct_battalion_id(Data_Chapter chapter)
        {
            // If there's no data for the chapter's correct battalion,
            // but there is battalion 0
            if (!ContainsKey(chapter.Battalion) && ContainsKey(0))
            {
                Data[chapter.Battalion] = Data[0];
                Data.Remove(0);
                if (Current_Battalion == 0)
                    Current_Battalion = chapter.Battalion;
            }
        }

        public void set_individual_animation(int index, int mode)
        {
            Individual_Animations[index] = mode;
        }

        /*public void fix_battalion(int index, Battalion battalion) //Debug
        {
            while (Data.Count < index + 1)
                add_battalion();
            Data[index] = battalion;
        }*/

        #region Convoy
        public void add_convoy(int id)
        {
            Convoys.Add(id, new Game_Convoy());
        }

        public bool contains_convoy(int id)
        {
            return Convoys.ContainsKey(id);
        }

        public void add_item_to_convoy(Item_Data item, int id = -1)
        {
            if (id == -1)
                id = Global.battalion.convoy_id;
            if (Convoys[id].Data.Count < Constants.Gameplay.CONVOY_SIZE)
                Convoys[id].Data.Add(item);
        }

        public List<Item_Data> convoy(int id)
        {
            return Convoys[id].Data;
        }

        public Item_Data remove_item_from_convoy(int id, int index)
        {
            Item_Data item_data = Convoys[id].Data[index];
            Convoys[id].Data.RemoveAt(index);
            return item_data;
        }

        public void adjust_convoy_item_uses(int id, int index, int count)
        {
            Convoys[id].Data[index].add_uses(count);
        }

        public void sort_convoy(int id)
        {
            Convoys[id].sort();
        }
        #endregion

        #region Base Shop
        internal void set_convoy_shop(Shop_Data shop)
        {
            Convoys[battalion.convoy_id].Shop = shop;
        }

        public void clear_convoy_shop()
        {
            Convoys[battalion.convoy_id].Shop = null;
        }

        public void add_sold_home_base_item(Item_Data item_data)
        {
            Convoys[battalion.convoy_id].Sold_Items.Add(new Item_Data(item_data));
        }

        public List<Item_Data> convoy_sold_items()
        {
            return Convoys[battalion.convoy_id].valid_sold_items();
        }

        public void clear_convoy_sold_items()
        {
            Convoys[battalion.convoy_id].Sold_Items.Clear();
        }
        #endregion

        public void copy_battalion_to(Game_Battalions target, int battalion_id)
        {
            target.Data[battalion_id] = Data[battalion_id];
            if (Convoys.Count > 1 || target.Convoys.Count > 1) // I suspect this might break with multiple convoys, setting a break here to test it //Yeti
            {
                int x = 0;
                x++;
            }
            foreach (var pair in Convoys)
                target.Convoys[pair.Key] = pair.Value;
            //if (Data[battalion_id].convoy_id != -1) //Debug
            //    target.Convoys[Data[battalion_id].convoy_id] = Convoys[Data[battalion_id].convoy_id];
        }
    }

    public class Battalion
    {
        private List<int> Actors = new List<int>(), Deployed_Actors = new List<int>();
        private int Convoy_Id = -1;
        private int Gold = 0;

        #region Serialization
        public void write(BinaryWriter writer)
        {
            Actors.write(writer);
            Deployed_Actors.write(writer);
            writer.Write(Convoy_Id);
            writer.Write(Gold);
        }

        public static Battalion read(BinaryReader reader)
        {
            Battalion result = new Battalion();
            result.Actors.read(reader);
            if (!Global.LOADED_VERSION.older_than(0, 4, 6, 6))
                result.Deployed_Actors.read(reader);
            result.Convoy_Id = reader.ReadInt32();
            result.Gold = reader.ReadInt32();

            return result;
        }
        #endregion

        #region Accessors
        public List<int> actors { get { return Actors; } }
        public List<int> deployed { get { return Deployed_Actors.Where(x => Actors.Contains(x) && Global.game_map.get_unit_id_from_actor(x) == -1).ToList(); } }

        public int convoy_id
        {
            get { return Convoy_Id; }
            set
            {
                Convoy_Id = Math.Max(value, -1);
                if (!Global.game_battalions.contains_convoy(Convoy_Id))
                    Global.game_battalions.add_convoy(Convoy_Id);
            }
        }

        public bool is_convoy_full
        {
            get
            {
                return Global.game_battalions.convoy(Convoy_Id).Count >=
                    Constants.Gameplay.CONVOY_SIZE;
            }
        }

        public bool convoy_ready_for_sending
        {
            get
            {
                // If the convoy exists for this battalion and it's not full
                if (Convoy_Id != -1 && !is_convoy_full)
                    // And the ability to send is forced,
                    // or it's deployed to the map, or this is preparations,
                    // or there is no map so the convoy can't be deployed anyway
                    if (Global.game_temp.force_send_to_convoy ||
                            Global.game_map.get_unit_id_from_actor(Convoy_Id) != -1 ||
                            Global.game_system.preparations ||
                            Global.game_map.width == 0)
                        return true;
                return false;
            }
        }

        public int gold
        {
            get { return Gold; }
            set { Gold = Math.Max(0, value); }
        }

        public int average_level { get { return avg_level(false); } }
        public int deployed_average_level { get { return avg_level(true); } }
        private int avg_level(bool deployed)
        {
            if (Actors.Count <= 0)
                return 0;

            int lvl = 0;
            int count = 0;
            for (int i = 0; i < Actors.Count; i++)
            {
                // Only get deployed units
                if (deployed && !is_actor_deployed(i))
                    continue;
                lvl += Global.game_actors[Actors[i]].full_level *
                    Constants.Actor.EXP_TO_LVL + Global.game_actors[Actors[i]].exp;
                count++;
            }
            if (count <= 0)
                return 0;
            return lvl / count;
        }

        public float deployed_average_rating
        {
            get
            {
                if (Actors.Count <= 0)
                {
                    var pcs = Global.game_map.units
                        .Where(x => x.Value.is_player_team);
                    if (pcs.Any())
                        return (int)pcs
                            .Average(x => x.Value.actor.rating());
                    return 0;
                }

                float rating = 0;
                int count = 0;
                for (int i = 0; i < Actors.Count; i++)
                {
                    // Only get deployed units
                    if (!is_actor_deployed(i))
                        continue;
                    rating += Global.game_actors[Actors[i]].rating();
                    count++;
                }
                if (count <= 0)
                    return 0;
                return rating / count;
            }
        }

        public float enemy_rating
        {
            get
            {
                var enemies = Global.game_map.units
                    .Where(x => x.Value.is_attackable_team(Constants.Team.PLAYER_TEAM));
                if (enemies.Any())
                    return (float)enemies
                        .Average(x => (float)x.Value.actor.rating());
                return 0;
            }
        }

        public float enemy_threat
        {
            get
            {
                if (deployed_average_rating == 0)
                    return 0;
                return enemy_rating / deployed_average_rating;
            }
        }
        #endregion

        public Battalion() { }
        public Battalion(Battalion source)
        {
            Actors = new List<int>(source.Actors);
            Convoy_Id = source.Convoy_Id;
            Gold = source.Gold;
        }

        public void add_actor(int id)
        {
            if (!Actors.Contains(id))
            {
                Actors.Add(id);
                Global.game_actors[id].reset_lives();
            }
            if (Constants.Gameplay.LOSS_ON_DEATH.Contains(id))
                Global.game_system.add_loss_on_death(id);
        }

        public void remove_actor(int id)
        {
            Actors.Remove(id);
            Global.game_system.remove_loss_on_ally_death(id);
        }

        public bool is_actor_deployed(int index)
        { 
            return Global.game_map.is_actor_deployed(Actors[index]);
        }

        public int undeployed_actor(int index, bool include_immobile = false)
        {
            if (index > Actors.Count)
                return -1;
            for (int i = 0; i < Actors.Count; i++)
                // Checks if the unit is not yet deployed, and if they are able to move
                // (There's not much reason to use this to find units that can't move on the map, unless counting PCs)
                if (!is_actor_deployed(i) &&  (include_immobile || Global.game_actors[Actors[i]].mov > 0))
                {
                    index--;
                    if (index < 0)
                    {
                        return Actors[i];
                    }
                }
            return -1;
        }

        public void sort_by_deployed()
        {
            List<int> old_team = new List<int>();
            old_team.AddRange(Actors);
            Actors.Sort(delegate(int a, int b)
            {
                int value;
                // If at least one of the actors is forced
                if (Global.game_map.forced_deployment.Contains(a) || Global.game_map.forced_deployment.Contains(b))
                {
                    value = (!Global.game_map.forced_deployment.Contains(a) ? 1 : (
                        !Global.game_map.forced_deployment.Contains(b) ? -1 :
                        Global.game_map.forced_deployment.IndexOf(a) - Global.game_map.forced_deployment.IndexOf(b)));
                }
                else if (Global.game_map.is_actor_deployed(a))
                {
                    value = Global.game_map.is_actor_deployed(b) ? 0 : -1;
                }
                else if (Global.game_map.is_actor_deployed(b))
                {
                    value = 1;
                }
                else
                    value = 0;
                if (value == 0)
                    return old_team.IndexOf(a) - old_team.IndexOf(b);
                else
                    return value;
            });
        }

        public int item_count(Item_Data item_data)
        {
            int count = 0;
            if (Convoy_Id > -1)
                for (int i = 0; i < Global.game_battalions.convoy(Convoy_Id).Count; i++)
                    if (item_data.same_item(Global.game_battalions.convoy(Convoy_Id)[i]))
                        count++;
            foreach(int actor_id in Actors)
                foreach(Item_Data actor_item in Global.game_actors[actor_id].items)
                    if (item_data.same_item(actor_item))
                        count++;
            return count;
        }

        public bool convoy_has_item(Item_Data item_data)
        {
            if (Convoy_Id == -1)
                return false;
            return Global.game_battalions.convoy(Convoy_Id)
                .Any(x => x.same_item(item_data));
        }

        public Item_Data convoy_item(int index)
        {
            if (Convoy_Id == -1)
                return null;
            return Global.game_battalions.convoy(Convoy_Id)[index];
        }

        public void refresh_deployed()
        {
            Deployed_Actors.Clear();
            // Gets the actor ids of units on the map that are on the player team
            if (Global.map_exists)
                Deployed_Actors.AddRange(Global.game_map.units
                    .Where(x => x.Value.is_player_team)
                    .Select(x => x.Value.actor.id));
        }

        internal void enter_home_base()
        {
            if (Global.game_battalions.active_convoy_shop != null)
            {
                if (Constants.Gameplay.CONVOY_SOLD_ITEMS_REPAIR)
                {
                    List<Item_Data> repaired_items = Global.game_battalions.convoy_sold_items();
                    foreach (Item_Data item_data in repaired_items)
                    {

                        // If the shop already has this item
                        if (Global.game_battalions.active_convoy_shop.items.Any(item => item.same_item(item_data)))
                        {
                            // If the shop item isn't infinite, add one use (stock) to it
                            if (Global.game_battalions.active_convoy_shop.items.First(item => item.same_item(item_data)).Uses >= 1)
                                Global.game_battalions.active_convoy_shop.items.First(item => item.same_item(item_data)).repair_one_use();
                        }
                        else
                        {
                            int index;
                            // Among weapons with ids below that of the item being added, get the index of the one with the highest
                            if (Global.game_battalions.active_convoy_shop.items.Any(item => item.is_weapon && item.Id < item_data.Id))
                            {
                                int weapon_id = Global.game_battalions.active_convoy_shop.items
                                    .Where(item => item.is_weapon && item.Id < item_data.Id).Max(item => item.Id);
                                index = 1 + Global.game_battalions.active_convoy_shop.items.IndexOf(
                                    Global.game_battalions.active_convoy_shop.items.Last(x => x.is_weapon && x.Id == weapon_id));
                            }
                            // If there aren't any lower, use index 0 instead
                            else
                                index = 0;
                            Global.game_battalions.active_convoy_shop.items.Insert(index, new Item_Data(item_data.Type, item_data.Id, 1));
                        }
                    }
                }
                Global.game_battalions.clear_convoy_sold_items();
            }
        }

        internal void leave_home_base()
        {
            if (Convoy_Id > -1)
                Global.game_battalions.clear_convoy_shop();
        }
    }

    public class Game_Convoy
    {
        public List<Item_Data> Data = new List<Item_Data>();
        internal Shop_Data Shop = null;
        public List<Item_Data> Sold_Items = new List<Item_Data>();

        #region Serialization
        public void write(BinaryWriter writer)
        {
            Data.write(writer);
            writer.Write(Shop != null);
            if (Shop != null)
                Shop.write(writer);
            Sold_Items.write(writer);
        }

        public static Game_Convoy read(BinaryReader reader)
        {
            Game_Convoy result = new Game_Convoy();

            result.Data.read(reader);
            bool shop_exists = reader.ReadBoolean();
            if (shop_exists)
                result.Shop = Shop_Data.read(reader);
            result.Sold_Items.read(reader);

            return result;
        }
        #endregion

        public Game_Convoy() { }
        public Game_Convoy(Game_Convoy convoy)
        {
            Data = new List<Item_Data>(convoy.Data.Select(x => new Item_Data(x)));
        }

        public void sort()
        {
            // Put weapons before items, low ids before high ids, high uses remaining before low uses remaining
            // Maybe adjust this later sort weapons by rank within type //Yeti
            Data.Sort(delegate(Item_Data a, Item_Data b)
            {
                return a.Type == b.Type ? (a.Id == b.Id ? b.Uses - a.Uses : a.Id - b.Id) : ((int)a.Type - (int)b.Type);
            });
        }

        public List<Item_Data> valid_sold_items()
        {
            Weapon_Ranks highest_weapon_rank = (Weapon_Ranks)(Enum_Values.GetEnumCount(typeof(Weapon_Ranks)) - 1);
            return Sold_Items.Where(item_data =>
            {
                // Only weapons are allowed
                if (!item_data.is_weapon)
                    return false;
                Data_Weapon weapon = item_data.to_weapon;
                // Weapons with infinite uses are not allowed, because they really can't be repaired
                if (weapon.infinite_uses)
                    return false;
                // S rank weapons and Prf weapons are not allowed
                if (weapon.is_prf || weapon.Rank == Weapon_Ranks.None || weapon.Rank == highest_weapon_rank)
                    return false;
                // No Hammerne
                if (weapon.is_staff() && weapon.Staff_Traits[(int)Stave_Traits.Repair])
                    return false;
                return true;
            }).ToList();
        }
    }
}
