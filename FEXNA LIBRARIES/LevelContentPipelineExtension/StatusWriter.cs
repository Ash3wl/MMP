using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using ListExtension;

using TWrite = FEXNA_Library.Data_Status;

namespace LevelContentPipelineExtension
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class StatusWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {

            // Id
            output.Write(value.Id);
            // Name
            output.Write(value.Name);
            // Description
            output.Write(value.Description);
            // Turns
            output.Write(value.Turns);
            // Negative
            output.Write(value.Negative);
            // Damage per Turn
            output.Write((double)value.Damage_Per_Turn);
            // Unselectable
            output.Write(value.Unselectable);
            // AI Controlled
            output.Write(value.Ai_Controlled);
            // Attacks Allies
            output.Write(value.Attacks_Allies);
            // No Magic
            output.Write(value.No_Magic);
            // Skills
            value.Skills.write(output);
            // Image Index
            output.Write(value.Image_Index);
            // Map Animation
            output.Write(value.Map_Anim_Id);
            // Battle Color
            output.Write(value.Battle_Color);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FEXNA_Library.StatusReader).AssemblyQualifiedName;
        }
    }
}
