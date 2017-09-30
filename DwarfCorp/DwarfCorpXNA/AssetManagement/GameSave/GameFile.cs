// GameFile.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    public class GameFile 
    {
        public class GameData : SaveData
        {
            [JsonIgnore]
            public Texture2D Screenshot { get; set; }

            [JsonIgnore]
            public List<ChunkFile> ChunkData { get; set; }

            public MetaData Metadata { get; set; }


            public PlayData Worlddata { get; set; }

            public int GameID { get; set; }

            public GameData()
            {
                Metadata = new MetaData();
            }

            public void SaveToDirectory(string directory)
            {
                System.IO.Directory.CreateDirectory(directory);
                System.IO.Directory.CreateDirectory(directory + ProgramData.DirChar + "Chunks");

                foreach(ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(directory + ProgramData.DirChar + "Chunks" + ProgramData.DirChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + (DwarfGame.COMPRESSED_BINARY_SAVES ? ChunkFile.CompressedExtension : ChunkFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                }

                FileUtils.SaveJSon(this.Metadata, directory + ProgramData.DirChar + "Metadata." + MetaData.Extension, false);
                FileUtils.SaveJSon(this.Worlddata, directory + ProgramData.DirChar + "World." + PlayData.Extension, DwarfGame.COMPRESSED_BINARY_SAVES);
            }
        }

        public GameData Data { get; set; }
       
        public static string Extension = "json";
        public static string CompressedExtension = "zip";

        public GameFile(string overworld, int id, WorldManager world)
        {
            Data = new GameData
            {
                Metadata =
                {
                    OverworldFile = overworld,
                    WorldOrigin = world.WorldOrigin,
                    WorldScale = world.WorldScale,
                    TimeOfDay = world.Sky.TimeOfDay,
                    GameID = id,
                    Time = world.Time,
                    Slice = (int)world.ChunkManager.ChunkData.MaxViewingLevel,
                    NumChunks = world.ChunkManager.WorldSize
                },
                Worlddata =  PlayData.CreateFromWorld(world),
                ChunkData = new List<ChunkFile>(),
            };


            foreach (ChunkFile file in world.ChunkManager.ChunkData.GetChunkEnumerator().Select(c => new ChunkFile(c)))
                Data.ChunkData.Add(file);
        }

        public virtual string GetExtension()
        {
            return "game";
        }

        public virtual string GetCompressedExtension()
        {
            return "zgame";
        }

        public GameFile(string file, bool compressed, WorldManager world)
        {
            Data = new GameData();
            ReadMetadata(file, compressed, world);
        }

        public GameFile()
        {
            Data = new GameData();
        }

        public void CopyFrom(GameFile file)
        {
            Data = file.Data;
        }


        public bool ReadChunks(string filePath)
        {
            string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");


            if (chunkDirs.Length > 0)
            {
                string chunkDir = chunkDirs[0];

                string[] chunks = SaveData.GetFilesInDirectory(chunkDir, DwarfGame.COMPRESSED_BINARY_SAVES, ChunkFile.CompressedExtension, ChunkFile.Extension);
                Data.ChunkData = new List<ChunkFile>();
                foreach (string chunk in chunks)
                {
                    Data.ChunkData.Add(new ChunkFile(chunk, DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES));
                }
            }
            else
            {
                Console.Error.WriteLine("Can't load chunks {0}, no chunks found", filePath);
                return false;
            }
            return true;
        }

        public bool ReadWorld(string filePath, WorldManager world)
        {
            string[] worldFiles = System.IO.Directory.GetFiles(filePath, "*." + PlayData.Extension);

            if (worldFiles.Length > 0)
            {
                string worldFile = worldFiles[0];
                Data.Worlddata = FileUtils.LoadJson<PlayData>(worldFile, DwarfGame.COMPRESSED_BINARY_SAVES,
                    world);
            }
            else
            {
                Console.Error.WriteLine("Can't load world from {0}, no data file found.", filePath);
                return false;
            }
            return true;
        }

        public  bool ReadMetadata(string filePath, bool isCompressed, WorldManager world)
        {
            if(!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] screenshots = SaveData.GetFilesInDirectory(filePath, false, "png", "png");

                
                string[] metaFiles = System.IO.Directory.GetFiles(filePath, "*." + MetaData.Extension);

                if (metaFiles.Length > 0)
                {
                    Data.Metadata = FileUtils.LoadJson<MetaData>(metaFiles[0], false, null);
                }
                else
                {
                    Console.Error.WriteLine("Can't load file {0}, no metadata found", filePath);
                    return false;
                }

                if(screenshots.Length > 0)
                {
                    string screenshot = screenshots[0];
                    Data.Screenshot = TextureManager.LoadInstanceTexture(screenshot);
                }

                return true;
            }
        }

        public bool WriteFile(string filePath, bool compress)
        {
            Data.SaveToDirectory(filePath);
            return true;
        }

        public static string GetLatestSaveFile()
        {
            DirectoryInfo saveDirectory = Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves");
            DirectoryInfo newest = null;
            foreach (var dir in saveDirectory.EnumerateDirectories())
            {
                if (newest == null || newest.CreationTime < dir.CreationTime)
                {
                    newest = dir;
                }
            }
            return newest == null ? null : newest.FullName;
        }
    }

}
