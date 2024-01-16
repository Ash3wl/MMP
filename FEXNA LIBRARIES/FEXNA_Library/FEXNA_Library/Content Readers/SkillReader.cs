using Microsoft.Xna.Framework.Content;

using TRead = FEXNA_Library.Data_Skill;

namespace FEXNA_Library
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class SkillReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            existingInstance = new TRead();

            // Id
            existingInstance.Id = input.ReadInt32();
            // Name
            existingInstance.Name = input.ReadString();
            // Description
            existingInstance.Description = input.ReadString();
            // Abstract
            existingInstance.Abstract = input.ReadString();
            // Image Name
            existingInstance.Image_Name = input.ReadString();
            // Image Index
            existingInstance.Image_Index = input.ReadInt32();
            // Animation Id
            existingInstance.Animation_Id = input.ReadInt32();
            // Map Anim Id
            existingInstance.Map_Anim_Id = input.ReadInt32();

            return existingInstance;
        }
    }
}
