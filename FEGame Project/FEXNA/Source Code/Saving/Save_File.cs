using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if DEBUG
using System.Diagnostics;
#endif
using FEXNAVersionExtension;

namespace FEXNA.IO
{
    public class Save_File
    {
        public Mode_Styles Style = Mode_Styles.Standard;
        public Difficulty_Modes Difficulty = Difficulty_Modes.Normal;
        private Dictionary<string, Dictionary<Difficulty_Modes, Dictionary<string, Save_Data>>> Data =
            new Dictionary<string, Dictionary<Difficulty_Modes, Dictionary<string, Save_Data>>>();

        #region Serialization
        public void write(BinaryWriter writer)
        {
            writer.Write((int)Style);
            writer.Write((int)Difficulty);
            writer.Write(Data.Count);
            foreach(var pair in Data)
            {
                writer.Write(pair.Key);

                writer.Write(pair.Value.Count);
                foreach (KeyValuePair<Difficulty_Modes, Dictionary<string, Save_Data>> data in pair.Value)
                {
                    writer.Write((int)data.Key);
                    write_chapter(data.Value, writer);
                }
            }
        }
        private static void write_chapter(Dictionary<string, Save_Data> data, BinaryWriter writer)
        {
            writer.Write(data.Count);
            foreach (var pair in data)
            {
                writer.Write(pair.Key);
                pair.Value.write(writer);
            }
        }

        public static Save_File read(BinaryReader reader)
        {
            Save_File result = new Save_File();
            result.Style = (Mode_Styles)reader.ReadInt32();
            if (!Global.LOADED_VERSION.older_than(0, 4, 3, 4))
                 result.Difficulty = (Difficulty_Modes)reader.ReadInt32();

            int chapter_count = reader.ReadInt32();
            if (Global.LOADED_VERSION.older_than(0, 4, 4, 0))
            {
                for (int i = 0; i < chapter_count; i++)
                {
                    string key = reader.ReadString();
                    Save_Data value = Save_Data.read(reader);
                    result.Data.Add(key, new Dictionary<Difficulty_Modes,Dictionary<string,Save_Data>> {
                        { value.difficulty, new Dictionary<string, Save_Data> { { value.progression_id, value } } }
                    } );
                }
            }
            else
            {
                for (int i = 0; i < chapter_count; i++)
                {
                    string chapter_key = reader.ReadString();
                    Dictionary<Difficulty_Modes, Dictionary<string, Save_Data>> chapter = new Dictionary<Difficulty_Modes, Dictionary<string, Save_Data>>();

                    int count = reader.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        Difficulty_Modes key = (Difficulty_Modes)reader.ReadInt32();
                        Dictionary<string, Save_Data> value = read_chapter(reader);
                        chapter.Add(key, value);
                    }
                    result.Data.Add(chapter_key, chapter);
                }
            }
            return result;
        }
        private static Dictionary<string, Save_Data> read_chapter(BinaryReader reader)
        {
            Dictionary<string, Save_Data> result = new Dictionary<string, Save_Data>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                Save_Data value = Save_Data.read(reader);
                result.Add(key, value);
            }
            return result;
        }
        #endregion

        #region Accessors
        //public Dictionary<string, Dictionary<string, Save_Data>> data { get { return Data; } }
        public int Count { get { return Data.Count; } }
        internal Save_Data most_recent_save
        {
            get
            {
                return recent_save();
                /*if (Count == 0)
                    return null;

                DateTime time = new DateTime();
                int index = -1;
                List<string> indices = Data.Keys.ToList();
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    foreach (var pair1 in Data[indices[i]])
                        foreach (var pair2 in pair1.Value)
                        {
                            DateTime temp_time = pair2.Value.time;
                            if (index == -1 || temp_time > time)
                            {
                                index = i;
                                time = temp_time;
                            }
                        }
                }
                string chapter_id = indices[index];

                time = new DateTime();
                index = -1;
                List <Difficulty_Modes> difficulty_indices = Data[chapter_id].Keys.ToList();
                for (int i = difficulty_indices.Count - 1; i >= 0; i--)
                {
                    foreach (var pair in Data[chapter_id][difficulty_indices[i]])
                    {
                        DateTime temp_time = pair.Value.time;
                        if (index == -1 || temp_time > time)
                        {
                            index = i;
                            time = temp_time;
                        }
                    }
                }
                Difficulty_Modes difficulty = difficulty_indices[index];

                time = new DateTime();
                index = -1;
                indices = Data[chapter_id][difficulty].Keys.ToList();
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    DateTime temp_time = Data[chapter_id][difficulty][indices[i]].time;
                    if (index == -1 || temp_time > time)
                    {
                        index = i;
                        time = temp_time;
                    }
                }

                return Data[chapter_id][difficulty][indices[index]];*/
            }
        }

        internal Dictionary<string, int> acquired_supports
        {
            get
            {
                Dictionary<string, int> result = new Dictionary<string, int>();
                foreach (Save_Data save in Data
                    .SelectMany(p => p.Value)
                    .SelectMany(p => p.Value)
                    .Select(p => p.Value))
                {
                    // If the chapter is standalone, there needs to be at least one chapter following from it or it's a trial map/etc and shouldn't be counted
                    if (Global.data_chapters[save.chapter_id].Standalone)
                    {
                        // Compare the prior chapter list for each chapter against the progression ids for the current chapter
                        if (!Global.data_chapters
                                // If this other chapter is standalone it can't follow from this chapter
                                .Any(x => !x.Value.Standalone && x.Value.Prior_Chapters
                                    .Intersect(Global.data_chapters[save.chapter_id].Progression_Ids).Any()))
                            continue;
                    }
                    Dictionary<string, int> supports = save.acquired_supports;
                    foreach (var pair in supports)
                    {
                        if (!result.ContainsKey(pair.Key))
                            result.Add(pair.Key, 0);
                        result[pair.Key] = Math.Max(result[pair.Key], supports[pair.Key]);
                    }
                }
                return result;
            }
        }
        #endregion

        public void save_data(string chapter_id, Difficulty_Modes difficulty, string progression_id,
            string previous_chapter_id)
        {
            if (!Data.ContainsKey(chapter_id))
                Data[chapter_id] = new Dictionary<Difficulty_Modes, Dictionary<string, Save_Data>>();
            if (!Data[chapter_id].ContainsKey(difficulty))
                Data[chapter_id][difficulty] = new Dictionary<string, Save_Data>();

            //string chapter = chapter_id;
            //if (difficulty != Difficulty_Modes.Normal)
            //    chapter += Config.DIFFICULTY_SAVE_APPEND[difficulty];

            Data[chapter_id][difficulty][progression_id] = new Save_Data();
            Data[chapter_id][difficulty][progression_id].save_data(chapter_id, progression_id,
                past_rankings(chapter_id, difficulty, previous_chapter_id));
        }

        public Dictionary<string, Game_Ranking> all_rankings(
            string chapterId, Difficulty_Modes difficulty)
        {
            if (!ContainsKey(chapterId, difficulty))
                return null;

            var save = recent_save(chapterId, difficulty, "");
            if (save == null)
                return null;

            var rankings = Save_Data.process_past_ranking(
                new List<Save_Data> { save });

            return rankings;
        }

        public Dictionary<string, Game_Ranking> past_rankings(
            string chapter_id, Difficulty_Modes difficulty, string previous_chapter_id)
        {
            List<Save_Data> previous_chapters = new List<Save_Data>();
            bool any_prior_rankings = 
                Global.data_chapters[chapter_id].Prior_Ranking_Chapters.Count != 0;
            List<string> previous_ranking_progression_ids = new List<string>(
                !any_prior_rankings ?
                Global.data_chapters[chapter_id].Prior_Chapters :
                Global.data_chapters[chapter_id].Prior_Chapters.Intersect(
                    Global.data_chapters[chapter_id].Prior_Ranking_Chapters));
            if (previous_ranking_progression_ids.Count > 0)
            {
                for (int i = 0; i < previous_ranking_progression_ids.Count; i++)
                {
                    if (previous_ranking_progression_ids[i] == Global.data_chapters[chapter_id].Prior_Chapters[0])
                    {
                        if (Data.ContainsKey(previous_chapter_id) && Data[previous_chapter_id].ContainsKey(difficulty) &&
                                Data[previous_chapter_id][difficulty].ContainsKey(previous_ranking_progression_ids[i]))
                            previous_chapters.Add(Data[previous_chapter_id][difficulty][previous_ranking_progression_ids[i]]);
#if DEBUG
                        else
                            Debug.Assert(string.IsNullOrEmpty(previous_chapter_id));
#endif
                    }
                    else
                    {
                        Save_Data previous_data = recent_save(difficulty, previous_ranking_progression_ids[i]);
#if DEBUG
                        Debug.Assert(previous_data != null);
#endif
                        previous_chapters.Add(previous_data);
                    }
                }
            }

            return Save_Data.process_past_ranking(previous_chapters);
        }

        public void load_data(string chapter_id, Difficulty_Modes difficulty,
            string previous_chapter_id, string progression_id)
        {
#if DEBUG
            Debug.Assert(Data.ContainsKey(previous_chapter_id),
                string.Format(
                    "No save data for \"{0}\", the previous chapter progression id of {1}",
                    previous_chapter_id, chapter_id));
            Debug.Assert(Data[previous_chapter_id].ContainsKey(difficulty),
                string.Format(
                    "No save data for progression id \"{0}\" on {1} difficulty",
                    previous_chapter_id, difficulty));
            // This doesn't seem formed correctly, why is Prior_Chapters[0] required? //Yeti
            Debug.Assert(Data[previous_chapter_id][difficulty].ContainsKey(
                    Global.data_chapters[chapter_id].Prior_Chapters[0]),
                string.Format(
                    "Progression id \"{0}\" doesn't have save data for chapter {1}",
                    previous_chapter_id,
                    Global.data_chapters[chapter_id].Prior_Chapters[0]));
#endif
            // An list of the battalions of the chapters being loaded from, in order with the last being the most important
            List<int> previous_chapter_battalions = new List<int>(), completed_chapter_battalions = new List<int>();
            Dictionary<int, Save_Data> battalion_chapters = new Dictionary<int, Save_Data>();
            /*for (int i = Global.data_chapters[chapter_id].Prior_Chapters.Count - 1; i >= 0; i--)
            {
                Save_Data data = i == 0 ? Data[previous_chapter_id][difficulty][Global.data_chapters[chapter_id].Prior_Chapters[0]] :
                    recent_save(difficulty, Global.data_chapters[chapter_id].Prior_Chapters[i]);
                int battalion = Global.data_chapters.Values.Single(x => x.Id == data.chapter_id).Battalion;

                battalion_chapters[battalion] = data;
                if (previous_chapter_battalions.Contains(battalion))
                    previous_chapter_battalions.Remove(battalion);
                previous_chapter_battalions.Add(battalion);
            }*/

            for (int i = 0; i < Global.data_chapters[chapter_id].Prior_Chapters.Count; i++)
            {
                Save_Data data = i == 0 ? Data[previous_chapter_id][difficulty][Global.data_chapters[chapter_id].Prior_Chapters[0]] :
                    recent_save(difficulty, Global.data_chapters[chapter_id].Prior_Chapters[i]);
                int battalion = Global.data_chapters.Values.Single(x => x.Id == data.chapter_id).Battalion;

                if (!battalion_chapters.ContainsKey(battalion))// previous_chapter_battalions.Contains(battalion)) //Debug
                {
                    battalion_chapters[battalion] = data;
                    // Insert instead of add so the first added data will be at the end of the list, and iterated last below
                    previous_chapter_battalions.Insert(0, battalion);
                }
            }
            for (int i = 0; i < Global.data_chapters[chapter_id].Completed_Chapters.Count; i++)
            {
                Save_Data data = recent_save(difficulty, Global.data_chapters[chapter_id].Completed_Chapters[i]);
                int battalion = Global.data_chapters.Values.Single(x => x.Id == data.chapter_id).Battalion;

                if (!battalion_chapters.ContainsKey(battalion))// previous_chapter_battalions.Contains(battalion) && !completed_chapter_battalions.Contains(battalion)) //Debug
                {
                    battalion_chapters[battalion] = data;
                    completed_chapter_battalions.Insert(0, battalion);
                }
            }

            Save_Data.reset_old_data();
            // Load system and event data from each previous file
            foreach (int battalion in previous_chapter_battalions)
                battalion_chapters[battalion].load_data();
            // Load all actors from each save
            foreach (int battalion in completed_chapter_battalions)
                battalion_chapters[battalion].load_actors();
            foreach (int battalion in previous_chapter_battalions)
                battalion_chapters[battalion].load_actors();
            // Then load only battalion actors from every save, overwriting data actors might have on routes where they aren't PCs
            foreach (int battalion in completed_chapter_battalions)
                battalion_chapters[battalion].load_battalion(battalion);
            foreach (int battalion in previous_chapter_battalions)
                battalion_chapters[battalion].load_battalion(battalion);

            //Data[previous_chapter_id][difficulty][Global.data_chapters[chapter_id].Prior_Chapters[0]].load_data(); //Debug
        }
        /*public void load_data(string chapter_id, Difficulty_Modes difficulty, string progression_id) //Debug
        {
            if (Data.ContainsKey(chapter_id) && Data[chapter_id].ContainsKey(difficulty) && Data[chapter_id][difficulty].ContainsKey(progression_id))
                Data[chapter_id][difficulty][progression_id].load_data();
        }*/

        public string displayed_rank(string chapter_id, Difficulty_Modes difficulty)
        {
            DateTime time = new DateTime();
            int index = -1;
            List<string> indices = Data[chapter_id][difficulty].Keys.ToList();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                DateTime temp_time = Data[chapter_id][difficulty][indices[i]].time;
                if (index == -1 || temp_time > time)
                {
                    index = i;
                    time = temp_time;
                }
            }

            return Data[chapter_id][difficulty][indices[index]].ranking.rank;
        }

        /// <summary>
        /// Checks if a chapter is available to play. Returns true if the chapter is open.
        /// </summary>
        /// <param name="chapter_id">The key of the chapter to check</param>
        /// <param name="difficulty">What difficulty to use</param>
        public bool chapter_available(string chapter_id, Difficulty_Modes difficulty)
        {
            if (Global.data_chapters[chapter_id].Prior_Chapters.Count > 0)
                // If the chapter has prior chapters, to be viable it has to have save data to load from those chapters
                for (int i = 0; i < Global.data_chapters[chapter_id].Prior_Chapters.Count; i++)
                {
                    // The first entry must have the same battalion
                    if (i == 0)
                    {
                        // Why does this refer to 'Global.save_file' instead of itself, is it just out of date? //Debug
                        //if (!Global.save_file.has_progression_key(Global.data_chapters[chapter_id].Prior_Chapters[i], difficulty, //Debug
                        if (!has_progression_key(Global.data_chapters[chapter_id].Prior_Chapters[i], difficulty,
                                Global.data_chapters.Values.Single(x => x.Id == chapter_id).Battalion))
                            return false;
                    }
                    else
                        // Why does this refer to 'Global.save_file' instead of itself, is it just out of date? //Debug
                        //if (!Global.save_file.has_progression_key(Global.data_chapters[chapter_id].Prior_Chapters[i], difficulty)) //Debug
                        if (!has_progression_key(Global.data_chapters[chapter_id].Prior_Chapters[i], difficulty))
                            return false;
                }

            // Completed chapters require only that the chapter has been beaten, on any difficulty
            for (int i = 0; i < Global.data_chapters[chapter_id].Completed_Chapters.Count; i++)
            {
                // Why does this refer to 'Global.save_file' instead of itself, is it just out of date? //Debug
                //if (!Global.save_file.has_progression_key(Global.data_chapters[chapter_id].Completed_Chapters[i], Difficulty_Modes.Normal)) //Debug
                if (!has_progression_key(Global.data_chapters[chapter_id].Completed_Chapters[i], Difficulty_Modes.Normal))
                    return false;
            }

            return true;
        }

        private bool has_progression_key(string progression_id, Difficulty_Modes difficulty = Difficulty_Modes.Normal, int battalion = -1)
        {
            foreach (var chapter in Data)
                if (has_progression_key(chapter.Key, progression_id, difficulty, battalion))
                    return true;
            return false;
        }
        private bool has_progression_key(string chapter, string progression_id, Difficulty_Modes difficulty = Difficulty_Modes.Normal, int battalion = -1)
        {
            foreach (var chapter_difficulty in Data[chapter])
                if (chapter_difficulty.Key >= difficulty)
                    if (chapter_difficulty.Value.ContainsKey(progression_id))
                    {
                        if (battalion != -1)
                        {
                            if (Global.data_chapters.Values.Single(x => x.Id == chapter).Battalion == battalion)
                                return true;
                        }
                        else
                            return true;
                    }
                    //foreach (var progression in chapter_difficulty.Value) //Debug
                    //    if (progression.Value.progression_id == progression_id && progression.Value.difficulty >= difficulty)
                    //        return true;
            return false;
        }

        public List<string> previous_chapters(string chapter_id, Difficulty_Modes difficulty)
        {
            if (!Global.data_chapters[chapter_id].Prior_Chapters.Any())
                return new List<string>();
            string progression_id = Global.data_chapters[chapter_id].Prior_Chapters[0];
            int battalion = Global.data_chapters.Values.Single(x => x.Id == chapter_id).Battalion;

            List<string> result = Global.data_chapters.Values
                .Where(x => x.Progression_Ids.Contains(progression_id) && x.Battalion == battalion)
                .Select(x => x.Id).ToList();
            return result;
        }
        public List<string> valid_previous_chapters(string chapter_id, Difficulty_Modes difficulty)
        {
            if (Global.data_chapters[chapter_id].Prior_Chapters.Count == 0)
                return new List<string>();
            string progression_id = Global.data_chapters[chapter_id].Prior_Chapters[0];
            int battalion = Global.data_chapters.Values.Single(x => x.Id == chapter_id).Battalion;

            List<string> result = new List<string>(
                // Get keys from the data where the matching value has the given progression id at the given difficulty
                Data.Keys.Where(key => has_progression_key(key, progression_id, difficulty, battalion)));
            foreach (var chapter in Data)
                if (has_progression_key(chapter.Key, progression_id, difficulty, battalion))
                {}//result.Add(chapter.Key);

#if DEBUG
            if (result.Count == 0 && !Global.data_chapters[chapter_id].Standalone)
                throw new ArgumentException(string.Format("Trying to get previous chapters for \"{0}\"\nwhen no valid saves exist", chapter_id));
#endif

            result.Sort(delegate(string a, string b)
            {
                //int seconds = (int)(Data[a][difficulty][progression_id].time - Data[a][difficulty][progression_id].time).TotalSeconds;
                //if (seconds != 0)
                //    return seconds;

                //return Config.CHAPTERS.IndexOf(Config.CHAPTERS.Single(x => x.Id == a)) -
                //    Config.CHAPTERS.IndexOf(Config.CHAPTERS.Single(x => x.Id == b));
                return Global.Chapter_List.IndexOf(a) - Global.Chapter_List.IndexOf(b);
            });

            return result;
        }

        public bool ContainsKey(string key)
        {
            return Data.ContainsKey(key);
        }
        public bool ContainsKey(string key, Difficulty_Modes difficulty)
        {
            if (Data.ContainsKey(key))
                return Data[key].ContainsKey(difficulty);
            return false;
        }

        private Save_Data recent_save(string progression_id = "")
        {
            if (Count == 0)
                return null;

            DateTime time = new DateTime();
            int index = -1;
            List<string> indices = Data.Keys.ToList();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                foreach (var pair1 in Data[indices[i]])
                    foreach (var pair2 in pair1.Value)
                    {
                        if (string.IsNullOrEmpty(progression_id) || pair2.Key == progression_id)
                        {
                            DateTime temp_time = pair2.Value.time;
                            if (index == -1 || temp_time > time)
                            {
                                index = i;
                                time = temp_time;
                            }
                        }
                    }
            }
            if (index == -1)
                return null;
            return recent_save(indices[index], progression_id);
        }
        private Save_Data recent_save(Difficulty_Modes difficulty, string progression_id)
        {
            if (Count == 0)
                return null;

            DateTime time = new DateTime();
            int index = -1;
            List<string> indices = Data.Keys.ToList();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                foreach (var pair1 in Data[indices[i]])
                    if (pair1.Key == difficulty)
                    foreach (var pair2 in pair1.Value)
                    {
                        if (string.IsNullOrEmpty(progression_id) || pair2.Key == progression_id)
                        {
                            DateTime temp_time = pair2.Value.time;
                            if (index == -1 || temp_time > time)
                            {
                                index = i;
                                time = temp_time;
                            }
                        }
                    }
            }
            if (index == -1)
                return null;
            return recent_save(indices[index], difficulty, progression_id);
        }
        private Save_Data recent_save(string chapter_id, string progression_id)
        {
            if (Count == 0)
                return null;

            DateTime time = new DateTime();
            int index = -1;
            List<Difficulty_Modes> difficulty_indices = Data[chapter_id].Keys.ToList();
            for (int i = difficulty_indices.Count - 1; i >= 0; i--)
            {
                foreach (var pair in Data[chapter_id][difficulty_indices[i]])
                {
                    if (string.IsNullOrEmpty(progression_id) || pair.Key == progression_id)
                    {
                        DateTime temp_time = pair.Value.time;
                        if (index == -1 || temp_time > time)
                        {
                            index = i;
                            time = temp_time;
                        }
                    }
                }
            }
            if (index == -1)
                return null;
            return recent_save(chapter_id, difficulty_indices[index], progression_id);
        }
        private Save_Data recent_save(string chapter_id, Difficulty_Modes difficulty, string progression_id)
        {
            if (Count == 0)
                return null;

            DateTime time = new DateTime();
            int index = -1;
            List<string> indices = Data[chapter_id][difficulty].Keys.ToList();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(progression_id) || indices[i] == progression_id)
                {
                    DateTime temp_time = Data[chapter_id][difficulty][indices[i]].time;
                    if (index == -1 || temp_time > time)
                    {
                        index = i;
                        time = temp_time;
                    }
                }
            }
            if (index == -1)
                return null;

            return Data[chapter_id][difficulty][indices[index]];
        }
    }
}
