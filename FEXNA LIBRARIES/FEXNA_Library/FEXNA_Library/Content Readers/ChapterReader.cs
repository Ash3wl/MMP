using Microsoft.Xna.Framework.Content;

using TRead = FEXNA_Library.Data_Chapter;

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
    public class ChapterReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            existingInstance = FEXNA_Library.Data_Chapter.Read(input);

            return existingInstance;
        }
    }
}
