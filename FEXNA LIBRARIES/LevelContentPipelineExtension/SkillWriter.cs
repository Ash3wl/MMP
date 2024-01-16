using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

using TWrite = FEXNA_Library.Data_Skill;

namespace LevelContentPipelineExtension
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class SkillWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {

            // Id
            output.Write(value.Id);
            // Name
            output.Write(value.Name);
            // Description
            output.Write(value.Description);
            // Abstract
            output.Write(value.Abstract);
            // Image Name
            output.Write(value.Image_Name);
            // Image Index
            output.Write(value.Image_Index);
            // Animation Id
            output.Write(value.Animation_Id);
            // Map Anim Id
            output.Write(value.Map_Anim_Id);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FEXNA_Library.SkillReader).AssemblyQualifiedName;
        }
    }
}
