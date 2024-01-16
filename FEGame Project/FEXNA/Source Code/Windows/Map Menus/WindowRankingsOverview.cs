﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Graphics.Text;
using FEXNA.Windows.UserInterface;
using FEXNA.Windows.UserInterface.Command;
using FEXNA_Library;

namespace FEXNA.Windows.Map
{
    class WindowRankingsOverview : Map_Window_Base
    {
        const int ROWS = 8;
        const int COLUMN_WIDTH = 64;
        readonly static Vector2 DATA_OFFSET = new Vector2(32, 32);

        private string Chapter;
        private Difficulty_Modes Difficulty;
        private List<KeyValuePair<string, Game_Ranking>> Rankings;
        private Vector2 Scroll, ScrollSpeed;

        private Button_Description CancelButton;
        private Sprite RankingIcons;
        private UINodeSet<TextUINode> Nodes;
        new private UICursor<TextUINode> Cursor; //Yeti
        private Window_Ranking DetailedRanking;
        private Sprite DataBackground;
        private FE_Text StyleText, DifficultyText;

        private bool cancel_button_triggered
        {
            get
            {
                return CancelButton.consume_trigger(MouseButtons.Left) ||
                    CancelButton.consume_trigger(TouchGestures.Tap) ||
                    Global.Input.mouse_click(MouseButtons.Right);
            }
        }

        public WindowRankingsOverview(string chapter, Difficulty_Modes difficulty)
        {
            Chapter = chapter;
            Difficulty = difficulty;

            initialize_sprites();
            update_black_screen();
        }

        protected void initialize_sprites()
        {
            // Black Screen
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(
                @"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 255);
            // Background
            Background = new Menu_Background();
            Background.texture = Global.Content.Load<Texture2D>(
                @"Graphics/Pictures/Status_Background");
            (Background as Menu_Background).vel = new Vector2(-0.25f, 0);
            (Background as Menu_Background).tile = new Vector2(3, 2);
            Background.stereoscopic = Config.MAPMENU_BG_DEPTH;
            // Cancel Button
            CancelButton = Button_Description.button(Inputs.B, 32);
            CancelButton.loc = new Vector2(32, Config.WINDOW_HEIGHT - 24);
            CancelButton.description = "Cancel";
            CancelButton.stereoscopic = Config.MAPCOMMAND_HELP_DEPTH;

            RankingIcons = new Sprite();
            RankingIcons.texture = Global.Content.Load<Texture2D>(
                @"Graphics/Pictures/RankingIcons");
            RankingIcons.loc = DATA_OFFSET + new Vector2(48, -16);

            DataBackground = new Sprite();
            DataBackground.texture = Global.Content.Load<Texture2D>(
                @"Graphics/White_Square");
            DataBackground.dest_rect = new Rectangle(
                0, (int)DATA_OFFSET.Y,
                Config.WINDOW_WIDTH, ROWS * 16);
            DataBackground.tint = new Color(0, 0, 0, 128);

            // Style
            StyleText = new FE_Text();
            StyleText.loc = new Vector2(
                Config.WINDOW_WIDTH - 112, Config.WINDOW_HEIGHT - 24);
            StyleText.Font = "FE7_Text";
            StyleText.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            StyleText.text = Global.game_system.Style.ToString();
            StyleText.stereoscopic = Config.DATA_LEADER_DEPTH;
            // Difficulty
            DifficultyText = new FE_Text();
            DifficultyText.loc = new Vector2(
                Config.WINDOW_WIDTH - 56, Config.WINDOW_HEIGHT - 24);
            DifficultyText.Font = "FE7_Text";
            DifficultyText.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            DifficultyText.text = Difficulty.ToString();
            DifficultyText.stereoscopic = Config.DATA_LEADER_DEPTH;

            refresh_rankings(Chapter, Difficulty);
        }

        private void refresh_rankings(string chapter, Difficulty_Modes difficulty)
        {
            var current_rankings = Global.save_file.all_rankings(chapter, difficulty);
            Rankings = current_rankings.ToList();

            int i = 0;
            List<TextUINode> ranks = new List<TextUINode>();
            foreach (string ch in Global.Chapter_List)
            {
                if (current_rankings.ContainsKey(ch))
                {
                    var chapter_data = Global.data_chapters[ch];
                    var ranking = current_rankings[ch];

                    var text = new FE_Text();
                    text.Font = "FE7_Text";
                    text.texture = Global.Content.Load<Texture2D>(
                        @"Graphics/Fonts/FE7_Text_Yellow");
                    string id = chapter_data.Id;
                    if (id.StartsWith("Ch") && id.Length > 2)
                        id = id.Substring(2);
                    text.text = id;
                    text.stereoscopic = Config.OPTIONS_OPTIONS_DEPTH; //Yeti

                    var node = new RankingUINode("", text, COLUMN_WIDTH, ranking);
                    node.loc = DATA_OFFSET +
                        new Vector2((i / ROWS * COLUMN_WIDTH), (i % ROWS) * 16);

                    ranks.Add(node);
                    i++;
                }
            }

            Nodes = new UINodeSet<TextUINode>(ranks);
            Nodes.AngleMultiplier = 4f;
            Nodes.TangentDirections = new List<CardinalDirections>
                { CardinalDirections.Left, CardinalDirections.Right };
            Nodes.refresh_destinations();

            Cursor = new UICursor<TextUINode>(Nodes);
            Cursor.draw_offset = new Vector2(-16, 0);
        }

        public override void update()
        {
            base.update();
            bool input = !Closing && Black_Screen_Timer <= 0 && DetailedRanking == null;

            CancelButton.Update(input);
            Nodes.Update(input, new Vector2((int)Scroll.X, (int)Scroll.Y));
            Cursor.update();

            update_scroll(input);

            if (DetailedRanking != null)
            {
                DetailedRanking.update();
                if (DetailedRanking.is_ready)
                {
                    DetailedRanking = null;
                }
            }
            else
            {
                var selected = Nodes.consume_triggered(
                    Inputs.A, MouseButtons.Left, TouchGestures.Tap);

                if (selected.IsSomething)
                {
                    DetailedRanking = new Window_Ranking(Rankings[selected].Value);
                }
                else if (Global.Input.triggered(Inputs.B) || cancel_button_triggered)
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    close();
                }
            }

            StyleText.update();
            DifficultyText.update();
        }

        private void update_scroll(bool input)
        {
            int columns = (int)Math.Ceiling(Nodes.Count / (float)ROWS);
            int columns_onscreen = (Config.WINDOW_WIDTH - 48) / COLUMN_WIDTH;
            int max = (columns - columns_onscreen) * COLUMN_WIDTH;

            if (Input.ControlScheme == ControlSchemes.Buttons)
            {
                ScrollSpeed = Vector2.Zero;

                int right_edge_offset = columns_onscreen * COLUMN_WIDTH -
                    (int)DATA_OFFSET.X;
                float x = Cursor.loc.X - right_edge_offset;
                x = Math.Max(Math.Min(x, max), 0);
                Scroll = new Vector2(x, 0);
            }
            else if (Input.ControlScheme == ControlSchemes.Mouse)
            {
                ScrollSpeed = Vector2.Zero;
                if (Input.IsControllingOnscreenMouse)
                {
                    const int MOUSE_EDGE = 48;

                    float offset = 0;
                    if (Global.Input.mousePosition.Y >= DATA_OFFSET.Y &&
                        Global.Input.mousePosition.Y < DATA_OFFSET.Y + ROWS * 16)
                    {
                        // Right edge
                        if (Global.Input.mousePosition.X >
                            Config.WINDOW_WIDTH - MOUSE_EDGE)
                        {
                            offset = -(float)Math.Pow(
                                Global.Input.mousePosition.X -
                                    (Config.WINDOW_WIDTH - MOUSE_EDGE),
                                0.6f);
                        }
                        // Left edge
                        else if (Global.Input.mousePosition.X < MOUSE_EDGE)
                        {
                            offset = (float)Math.Pow(
                                MOUSE_EDGE - Global.Input.mousePosition.X,
                                0.6f);
                        }
                    }

                    offset = MathHelper.Clamp(offset, -16, 16);
                    float x = Scroll.X - offset;
                    x = Math.Max(Math.Min(x, max), 0);
                    Scroll = new Vector2(x, 0);
                }
            }
            else if (Input.ControlScheme == ControlSchemes.Touch)
            {
                float x;
                if (Global.Input.gesture_triggered(TouchGestures.FreeDrag))
                {
                    ScrollSpeed = Global.Input.freeDragVector;
                }
                x = Scroll.X - ScrollSpeed.X;
                x = Math.Max(Math.Min(x, max), 0);
                Scroll = new Vector2(x, 0);
            }
            ScrollSpeed *= 0.9f;
        }

        protected override void draw_window(SpriteBatch sprite_batch)
        {
            Vector2 scroll = new Vector2((int)Scroll.X, (int)Scroll.Y);

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DataBackground.draw(sprite_batch);
            //for (int i = 0; i < Math.Ceiling(Nodes.Count / (float)ROWS); i++) //Debug
            //    RankingIcons.draw(sprite_batch, -new Vector2(i * COLUMN_WIDTH, 0));
            Nodes.Draw(sprite_batch, scroll);
            Cursor.draw(sprite_batch, scroll);

            CancelButton.Draw(sprite_batch);
            StyleText.draw(sprite_batch);
            DifficultyText.draw(sprite_batch);
            sprite_batch.End();

            if (DetailedRanking != null)
                DetailedRanking.draw(sprite_batch);
        }
    }
}