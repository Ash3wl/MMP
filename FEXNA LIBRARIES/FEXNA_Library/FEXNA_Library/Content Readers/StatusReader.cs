using Microsoft.Xna.Framework.Content;
using ListExtension;

using TRead = FEXNA_Library.Data_Status;

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
    public class StatusReader : ContentTypeReader<TRead>
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
            // Turns
            existingInstance.Turns = input.ReadInt32();
            // Negative
            existingInstance.Negative = input.ReadBoolean();
            // Damage per Turn
            existingInstance.Damage_Per_Turn = (float)input.ReadDouble();
            // Unselectable
            existingInstance.Unselectable = input.ReadBoolean();
            // AI Controlled
            existingInstance.Ai_Controlled = input.ReadBoolean();
            // Attacks Allies
            existingInstance.Attacks_Allies = input.ReadBoolean();
            // No Magic
            existingInstance.No_Magic = input.ReadBoolean();
            // Skills
            existingInstance.Skills.read(input);
            // Image Index
            existingInstance.Image_Index = input.ReadInt32();
            // Map Animation
            existingInstance.Map_Anim_Id = input.ReadInt32();
            // Battle Color
            existingInstance.Battle_Color = input.ReadColor();

            return existingInstance;
        }
    }
}
