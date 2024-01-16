using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using FEXNA.Windows.Command;

namespace FEXNA
{
    class SceneWorldmapArcs : Scene_Worldmap
    {
        private List<string> Arcs = new List<string>();
        private bool NonArcChapters = false;
        private int ActiveArc = 0;
        private List<int>[] IndexRedirects;

        public override int redirect
        {
            get
            {
                return IndexRedirects[ActiveArc][Command_Window.index];
            }
        }

        protected override void set_chapters()
        {
            base.set_chapters();
            get_arcs();
        }

        private void get_arcs()
        {
            // Get the arcs that are available
            var available_chapters = new HashSet<string>(Index_Redirect
                .Select(x => Global.Chapter_List[x]));
            foreach (string arc in Constants.WorldMap.GAME_ARCS)
            {
                var arc_chapters = Constants.WorldMap.ARC_CHAPTERS[arc];
                if (available_chapters.Intersect(arc_chapters).Any())
                {
                    Arcs.Add(arc);
                    available_chapters.ExceptWith(arc_chapters);
                }
            }
            NonArcChapters = available_chapters.Count > 0;

            // Sort the chapters into arcs
            IndexRedirects = new List<int>[Arcs.Count + (NonArcChapters ? 1 : 0)];
            for (int i = 0; i < IndexRedirects.Length; i++)
            {
                string arc = i >= Arcs.Count ? "" : Arcs[i];
                var arc_chapters = string.IsNullOrEmpty(arc) ?
                    null : Constants.WorldMap.ARC_CHAPTERS[arc];

                List<int> chapter_list = Index_Redirect
                    .Where(x =>
                        {
                            string ch = Global.Chapter_List[x];
                            if (arc_chapters == null)
                                return !Constants.WorldMap.ARC_CHAPTERS
                                    .Any(y => y.Value.Contains(ch));
                            else
                                return arc_chapters.Contains(ch);
                        })
                    .ToList();
                IndexRedirects[i] = chapter_list;
            }
        }

        protected override void create_command_window()
        {
            List<string> strs = new List<string>();
            foreach (int i in IndexRedirects[ActiveArc])
                strs.Add(Global.chapter_by_index(i).World_Map_Name);
            //Command_Window = new Window_Command(new Vector2(8, 56), 80, strs);
            //Command_Window = new Window_Command_Scroll(new Vector2(8, 60), 96, 6, strs); //was 56 y pos, 
            Command_Window = new Window_Command_Worldmap(
                new Vector2(8, 60), WIDTH, 6, strs);
            refresh_rank_images();

            Command_Window.tint = new Color(224, 224, 224, 224);
            Command_Window.glow = true;
            Command_Window.immediate_index = Index;
            Command_Window.refresh_scroll();
        }

        protected override void refresh_rank_images()
        {
            List<string> ranks = new List<string>(), hard_ranks = new List<string>();
            foreach (int i in IndexRedirects[ActiveArc])
            {
                if (Global.save_file != null)
                {
                    if (Global.save_file.ContainsKey(Global.Chapter_List[i], Difficulty_Modes.Normal))
                        ranks.Add(Global.save_file.displayed_rank(Global.Chapter_List[i], Difficulty_Modes.Normal));
                    else
                        ranks.Add("");
                    if (Global.save_file.ContainsKey(Global.Chapter_List[i], Difficulty_Modes.Hard))
                        hard_ranks.Add(Global.save_file.displayed_rank(Global.Chapter_List[i], Difficulty_Modes.Hard));
                    else
                        hard_ranks.Add("");
                }
                else
                {
                    ranks.Add("");
                    hard_ranks.Add("");
                }
            }
            Command_Window.refresh_ranks(ranks, hard_ranks);
        }

        protected override void refresh_data_panel()
        {
            if (!hard_mode_enabled(redirect)) // if hard mode not available for this chapter //Yeti
            {
                Global.save_file.Difficulty = Global.game_system.Difficulty_Mode = Difficulty_Modes.Normal;
            }
            Data_Window.set_mode(Global.game_system.Difficulty_Mode, Arcs.Count > 1);

            refresh_data();
        }

        public override void set_chapter(string id)
        {
            if (Index_Redirect.Any(x => Global.Chapter_List[x] == id))
            {
                int i = Index_Redirect.First(x => Global.Chapter_List[x] == id);
                ActiveArc = Enumerable.Range(0, IndexRedirects.Length)
                    .First(x => IndexRedirects[x].Contains(i));
                create_command_window();
                i = IndexRedirects[ActiveArc].IndexOf(i);
                set_chapter(i);
                return;
            }
            refresh();
        }

        protected override void command_left()
        {
            change_arc(false);
        }

        protected override void command_right()
        {
            change_arc(true);
        }

        private void change_arc(bool increase)
        {
            if (Mode_Switch_Timer <= 0)
            {
                Global.game_system.play_se(System_Sounds.Status_Page_Change);
                if (increase)
                    ActiveArc = (ActiveArc + 1) % IndexRedirects.Length;
                else
                    ActiveArc = (ActiveArc + IndexRedirects.Length - 1) %
                        IndexRedirects.Length;

                Index = 0;
                create_command_window();
                refresh();
                Mode_Switch_Timer = Constants.WorldMap.WORLDMAP_MODE_SWITCH_DELAY;
            }
        }
    }
}
