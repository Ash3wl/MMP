using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using ArrayExtension;
using ListExtension;
using Vector2Extension;

using TWrite = FEXNA_Library.Data_Chapter;

namespace LevelContentPipelineExtension
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class Chapter_Writer : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            // Id
            output.Write(value.Id);
            // Prior_Chapters
            value.Prior_Chapters.write(output);
            value.Prior_Ranking_Chapters.write(output);
            value.Completed_Chapters.write(output);
            // Standalone
            output.Write(value.Standalone);
            // Chapter Name
            output.Write(value.Chapter_Name);
            // World Map Name
            output.Write(value.World_Map_Name);
            // World Map Loc
            value.World_Map_Loc.write(output);
            // World Map Lord Id
            output.Write(value.World_Map_Lord_Id);
            // Turn Themes
            value.Turn_Themes.write(output);
            // Battle Themes
            value.Battle_Themes.write(output);

            // Battalion
            output.Write(value.Battalion);
            // Text Key
            output.Write(value.Text_Key);
            // Event Data
            output.Write(value.Event_Data_Id);

            // Ranking Turns
            output.Write(value.Ranking_Turns);
            // Ranking Combat
            output.Write(value.Ranking_Combat);
            // Ranking Exp
            output.Write(value.Ranking_Exp);
            // Ranking Completion
            output.Write(value.Ranking_Completion);

            // Preset Chapter Data
            output.Write(value.Preset_Data.Lord_Lvl);
            output.Write(value.Preset_Data.Units);
            output.Write(value.Preset_Data.Gold);
            output.Write(value.Preset_Data.Playtime);

            // Progression Ids
            value.Progression_Ids.write(output);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FEXNA_Library.ChapterReader).AssemblyQualifiedName;
        }
    }
}
