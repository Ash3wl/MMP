using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;

namespace FEXNA
{
    class Window_Home_Base : Window_Setup
    {
        public bool talk_events_exist { get { return Global.game_state.has_ready_base_events(); } }

        public Window_Home_Base() : base(false)
        {
            Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Panoramas/" + Global.game_system.home_base_background);
            (Background as Menu_Background).vel = new Vector2(0, 0);
            (Background as Menu_Background).tile = new Vector2(0, 0);
        }

        protected override void initialize_sprites()
        {
            base.initialize_sprites();
            refresh_talk_ready();
            CommandWindow.set_text_color(4, "Grey");
            Banner.src_rect = new Rectangle(0, 216, 320, 32);
            InfoWindow.loc = new Vector2(8, 128 + 32);
            InfoWindow.src_rect = new Rectangle(0, 176, 112, 32);
        }

        protected override void create_start_button()
        {
            StartButton = Button_Description.button(Inputs.Start,
                Config.WINDOW_WIDTH - 64);
            StartButton.description = "Leave";
            StartButton.stereoscopic = Config.PREPMAIN_INFO_DEPTH;
        }

        public void refresh_talk_ready()
        {
            if (!talk_events_exist)
                CommandWindow.set_text_color(3, "Grey");
            else
                CommandWindow.set_text_color(3, "White");
            if ((Home_Base_Choices)CommandWindow.index == Home_Base_Choices.Talk)
                refresh_text();
        }

        protected override List<string> command_window_string()
        {
            return new List<string> { "Items", "Support", "Talk", "Codex", "Options", "Save" };
        }

        public override void refresh()
        {
            Goal.text = Global.game_system.Objective_Text;
            Goal.offset = new Vector2(Font_Data.text_width(Goal.text) / 2, 0);
            AvgLvl.loc = new Vector2(108, 136);
            AvgLvl.text = (Global.battalion.deployed_average_level / Constants.Actor.EXP_TO_LVL).ToString();
            Funds.text = Global.battalion.gold.ToString();
        }

        protected override void refresh_text()
        {
            switch ((Home_Base_Choices)CommandWindow.index)
            {
                // Items
                case Home_Base_Choices.Trade:
                    HelpText.text = Global.system_text["Prep Items"];
                    break;
                // Support
                case Home_Base_Choices.Support:
                    HelpText.text = Global.system_text["Prep Support"];
                    break;
                // Talk
                case Home_Base_Choices.Talk:
                    if (talk_events_exist)
                        HelpText.text = Global.system_text["Prep Talk"];
                    else
                        HelpText.text = Global.system_text["Prep Disabled"];
                    break;
                // Codex
                case Home_Base_Choices.Codex:
                    HelpText.text = Global.system_text["Prep Disabled"];
                    break;
                // Options
                case Home_Base_Choices.Options:
                    HelpText.text = Global.system_text["Prep Options"];
                    break;
                // Save
                case Home_Base_Choices.Save:
                    HelpText.text = Global.system_text["Prep Save"];
                    break;
            }
        }

        protected override void draw_background(SpriteBatch sprite_batch)
        {
            if (Background != null)
            {
                Effect effect = Global.effect_shader();
                if (effect != null)
                {
                    effect.CurrentTechnique = effect.Techniques["Tone"];
                    effect.Parameters["tone"].SetValue(Global.game_state.screen_tone.to_vector_4(1.0f));
                }
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, effect);
                Background.draw(sprite_batch);
                sprite_batch.End();
            }
        }

        protected override void draw_info(SpriteBatch sprite_batch)
        {
            Counter.draw(sprite_batch);
        }
    }
}
