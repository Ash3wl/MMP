#region File Description
//-----------------------------------------------------------------------------
// ManifestPipeline.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content.Pipeline;
// for palettes
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace ContentManifestExtensions
{
    // the importer is just a passthrough that gives the processor the filepath
    [ContentImporter(".manifest", DisplayName = "Manifest Importer", DefaultProcessor = "ManifestProcessor")]
    public class ManifestImporter : ContentImporter<string>
    {
        public override string Import(string filename, ContentImporterContext context)
        {
            // just give the processor the filename needed to do the processing
            return filename;
        }
    }

    // processor takes in a filename and returns a list of files in the content project being built or
    // copied to the output directory
    [ContentProcessor(DisplayName = "Manifest Processor")]
    public class ManifestProcessor : ContentProcessor<string, List<string>>
    {
        public override List<string> Process(string input, ContentProcessorContext context)
        {
            List<string> filenames = new List<string>();
            // whoo breaking/fixing things
            using (StreamReader reader = new StreamReader(input))
                while (!reader.EndOfStream)
                {
                    string filename = reader.ReadLine();
                    if (!string.IsNullOrEmpty(filename))
                        filenames.Add(filename);
                }
            return filenames;
            // we assume the manifest is in the root of the content project.
            // we also assume there is only one content project in this file's directory.
            // using these assumptions we can create a path to the content project.
            string contentDirectory = input.Substring(0, input.LastIndexOf('\\'));
            string[] contentProjects = Directory.GetFiles(contentDirectory, "*.contentproj");
            if (contentProjects.Length != 1)
            {
                throw new InvalidOperationException("Could not locate content project.");
            }

            // we add a dependency on the content project itself to ensure our manifest is
            // rebuilt anytime the content project is modified
            context.AddDependency(contentProjects[0]);

            // create a list which we will fill with all the files being copied or built.
            // these will all be relative to the content project's root directory. built
            // content will not have an extension whereas copied content will maintain
            // its extension for loading.
            List<string> files = new List<string>();

            // we can now open up the content project for parsing which will allow us to
            // see what files are being built or copied
            XDocument document = XDocument.Load(contentProjects[0]);

            // we need the xmlns for us to find nodes in the document
            XNamespace xmlns = document.Root.Attribute("xmlns").Value;

            // we need the content root directory from the file to know where copied files will end up
            string contentRootDirectory = document.Descendants(xmlns + "ContentRootDirectory").First().Value;

            // first find all assets that are set to compile into XNB files
            var compiledAssets = document.Descendants(xmlns + "Compile");
            foreach (var asset in compiledAssets)
            {
                // get the include path and name
                string includePath = asset.Attribute("Include").Value;
                string name = asset.Descendants(xmlns + "Name").First().Value;

                // if the include path is a manifest, skip it
                if (includePath.EndsWith(".manifest"))
                    continue;

                // combine the two into the asset path if the include path
                // has a directory. otherwise we just use the name.
                if (includePath.Contains('\\'))
                {
                    string dir = includePath.Substring(0, includePath.LastIndexOf('\\'));
                    string assetPath = Path.Combine(dir, name);
                    files.Add(assetPath);
                }
                else
                {
                    files.Add(name);
                }
            }

            // next we find all assets that are set to copy to the output directory. we are going
            // to leverage LINQ to do this for us. this is the logic employed:
            //  1) we select all nodes that are children of an ItemGroup.
            //  2) from that set we find nodes that have a CopyToOutputDirectory node and make sure it is not set to None
            //  3) we then select that node's Include attribute as that is the value we want. we must also prepend
            //     the output directory to make the file path relative to the game instead of the content.
            var copiedAssetFiles = from node in document.Descendants(xmlns + "ItemGroup").Descendants()
                                   where node.Descendants(xmlns + "CopyToOutputDirectory").Any() &&
                                         node.Descendants(xmlns + "CopyToOutputDirectory").First().Value != "None"
                                   select Path.Combine(contentRootDirectory, node.Attribute("Include").Value);

            // we can now just add all of those files to our list
            files.AddRange(copiedAssetFiles);

            // Convert path separator to forward slash for better compatibility :R
            files = files
                .Select(x => x.Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();

            // lastly we want to override the manifest file with this list. this allows us to 
            // easily see what files were included in the build for debugging.
            using (FileStream fileStream = new FileStream(input, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    // now write all the files into the manifest
                    foreach (var file in files)
                    {
                        writer.WriteLine(file);
                    }
                }
            }

            // Putting this here because lazy~
            update_palette_data(contentDirectory, context);

            // just return the list which will be automatically serialized for us without
            // needing a ContentTypeWriter like we would have needed pre- XNA GS 3.1
            return files;
        }

        protected void update_palette_data(string content_folder, ContentProcessorContext context)
        {
            update_palette_data(content_folder, context, @"Palettes\Battlers", @"\Palette_Data.xml");
            update_palette_data(content_folder, context, "Faces", @"\Face_Palette_Data.xml");
        }
        protected void update_palette_data(string content_folder, ContentProcessorContext context, string folder, string output_filename)
        {
            Dictionary<string, Microsoft.Xna.Framework.Color[]> testData = new Dictionary<string, Microsoft.Xna.Framework.Color[]>();

            string[] battler_filenames = Directory.GetFiles(content_folder + @"\Graphics\" + folder + @"\", "*.png", SearchOption.TopDirectoryOnly);

            foreach (string filename in battler_filenames)
            {
                List<Microsoft.Xna.Framework.Color> palette = new List<Microsoft.Xna.Framework.Color>();

                using (Bitmap image = new Bitmap(filename))
                {
                    BitmapData bmp_data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    int stride = bmp_data.Stride;
                    System.IntPtr scan = bmp_data.Scan0;
                    int width = image.Width;
                    int height = image.Height;
                    unsafe
                    {
                        byte* ptr;
                        int offset = stride - image.Width * 4;
                        for (int y = 0; y < height; y++)
                        {
                            ptr = (byte*)(void*)scan + (offset + image.Width * 4 * y);
                            for (int x = 0; x < width; x++)
                            {
                                if (ptr[3] > 0)
                                    if (!palette.Contains(new Microsoft.Xna.Framework.Color((int)ptr[2], (int)ptr[1], (int)ptr[0], (int)ptr[3])))
                                        palette.Add(new Microsoft.Xna.Framework.Color((int)ptr[2], (int)ptr[1], (int)ptr[0], (int)ptr[3]));
                                //ptr[0] // B
                                //ptr[1] // G
                                //ptr[2] // R
                                //ptr[3] // A
                                ptr += 4;
                            }
                        }
                    }
                    image.UnlockBits(bmp_data);
                }
                testData[Path.GetFileNameWithoutExtension(filename)] = palette.ToArray();
                //write_debug(filename + ", " + palette.Count);
                //foreach (Microsoft.Xna.Framework.Color color in palette)
                //    write_debug(color.ToString());
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(content_folder + output_filename, settings))
            {
                IntermediateSerializer.Serialize(writer, testData, null);
            }
        }

        protected void write_debug(string text)
        {
            using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\palette test.txt", true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
