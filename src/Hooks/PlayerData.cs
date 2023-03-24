﻿using IL.Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;

namespace TheSacrifice
{
    internal static partial class Hooks
    {
        private static ConditionalWeakTable<Player, PlayerModule> PlayerData = new ConditionalWeakTable<Player, PlayerModule>();

        private class PlayerModule
        {
            public WeakReference<Player> Player;

            // Sprites
            public int firstSprite;
            public int lastSprite;


            // Objects
            public LightSource? activeObjectGlow;


            public PlayerModule(Player player)
            {
                Player = new WeakReference<Player>(player);

                InitSounds(player);
                InitColors(player);

                LoadTailTexture("tail");

                LoadEarLTexture("ear_l", AccentColor);
                LoadEarRTexture("ear_r", AccentColor);
            }

            public bool canSwallowOrRegurgitate = true;
            public Vector2 prevHeadRotation = Vector2.zero;



            public List<AbstractPhysicalObject> abstractInventory = new List<AbstractPhysicalObject>();
            public List<PhysicalObject> realizedObjects = new List<PhysicalObject>();

            public AbstractPhysicalObject? realizedActiveObject = null;

            public int? selectedIndex = null;
            public int? predictedIndex = null;


            public AbstractPhysicalObject? transferObject = null;
            public Vector2? transferObjectInitialPos = null;
            public bool canTransferObject = true;
            public int transferStacker = 0;


            public float shortcutColorStacker = 0.0f;
            public int shortcutColorStackerDirection = 1;


            #region Ears

            public TailSegment[]? earL;
            public TailSegment[]? earR;

            public int earLSprite;
            public int earRSprite;

            public Texture2D? earLTexture;
            public Texture2D? earRTexture;

            public FAtlas? earLAtlas;
            public FAtlas? earRAtlas;

            public Vector2 earLAttachPos;
            public Vector2 earRAttachPos;

            public int earLFlipDirection;
            public int earRFlipDirection;

            public void LoadEarLTexture(string textureName, Color color)
            {
                if (color == EarLColor) return;


                if (Futile.atlasManager.DoesContainElementWithName(textureName)) Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);
                if (earLTexture != null) Object.DestroyImmediate(earLTexture);

                earLTexture = AssetLoader.GetTexture(textureName);
                if (earLTexture == null) return;

                // Apply Colors
                MapAlphaToColor(earLTexture, 1.0f, BodyColor);
                MapAlphaToColor(earLTexture, 0.0f, color);
                EarLColor = color;

                earLAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + TextureID, earLTexture, false);
            }

            public void LoadEarRTexture(string textureName, Color color)
            {
                if (color == EarRColor) return;
                

                if (Futile.atlasManager.DoesContainElementWithName(textureName)) Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);
                if (earRTexture != null) Object.DestroyImmediate(earRTexture);

                earRTexture = AssetLoader.GetTexture(textureName);
                if (earRTexture == null) return;

                // Apply Colors
                MapAlphaToColor(earRTexture, 1.0f, BodyColor);
                MapAlphaToColor(earRTexture, 0.0f, color);
                EarRColor = color;

                earRAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + TextureID, earRTexture, false);
            }

            public void RegenerateEars()
            {
                if (!Player.TryGetTarget(out var player)) return;

                if (player.graphicsModule == null) return;

                PlayerGraphics self = (PlayerGraphics)player.graphicsModule;

                TailSegment[] newEarL = new TailSegment[2];
                newEarL[0] = new TailSegment(self, 2.5f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
                newEarL[1] = new TailSegment(self, 1.5f, 7.0f, newEarL[0], 0.85f, 1.0f, 0.5f, true);

                if (earL != null)
                {
                    for (var i = 0; i < newEarL.Length && i < earL.Length; i++)
                    {
                        newEarL[i].pos = earL[i].pos;
                        newEarL[i].lastPos = earL[i].lastPos;
                        newEarL[i].vel = earL[i].vel;
                        newEarL[i].terrainContact = earL[i].terrainContact;
                        newEarL[i].stretched = earL[i].stretched;
                    }
                }


                TailSegment[] newEarR = new TailSegment[2];
                newEarR[0] = new TailSegment(self, 2.5f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
                newEarR[1] = new TailSegment(self, 1.5f, 7.0f, newEarR[0], 0.85f, 1.0f, 0.5f, true);

                if (earR != null)
                {
                    for (var i = 0; i < newEarR.Length && i < earR.Length; i++)
                    {
                        newEarR[i].pos = earR[i].pos;
                        newEarR[i].lastPos = earR[i].lastPos;
                        newEarR[i].vel = earR[i].vel;
                        newEarR[i].terrainContact = earR[i].terrainContact;
                        newEarR[i].stretched = earR[i].stretched;
                    }
                }

                earL = newEarL;
                earR = newEarR;

                List<BodyPart> newBodyParts = self.bodyParts.ToList();

                newBodyParts.AddRange(earL);
                newBodyParts.AddRange(earR);

                self.bodyParts = newBodyParts.ToArray();
            }

            #endregion

            #region Tail

            public Texture2D? tailTexture;
            public FAtlas? tailAtlas;

            public void LoadTailTexture(string textureName)
            {
                if (Futile.atlasManager.DoesContainElementWithName(textureName)) Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);
                if (tailTexture != null) Object.DestroyImmediate(tailTexture);

                tailTexture = AssetLoader.GetTexture(textureName);
                if (tailTexture == null) return;


                // Apply Colors
                MapAlphaToColor(tailTexture, 1.0f, BodyColor);
                MapAlphaToColor(tailTexture, 0.0f, AccentColor);

                tailAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + TextureID, tailTexture, false);
            }


            public void RegenerateTail()
            {
                if (!Player.TryGetTarget(out var player)) return;

                if (player.graphicsModule == null) return;

                PlayerGraphics self = (PlayerGraphics)player.graphicsModule;

                TailSegment[] newTail = new TailSegment[6];
                newTail[0] = new TailSegment(self, 8.0f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
                newTail[1] = new TailSegment(self, 7.0f, 7.0f, newTail[0], 0.85f, 1.0f, 0.5f, true);
                newTail[2] = new TailSegment(self, 6.0f, 7.0f, newTail[1], 0.85f, 1.0f, 0.5f, true);
                newTail[3] = new TailSegment(self, 5.0f, 7.0f, newTail[2], 0.85f, 1.0f, 0.5f, true);
                newTail[4] = new TailSegment(self, 2.5f, 7.0f, newTail[3], 0.85f, 1.0f, 0.5f, true);
                newTail[5] = new TailSegment(self, 1.0f, 7.0f, newTail[4], 0.85f, 1.0f, 0.5f, true);

                for (int i = 0; i < newTail.Length && i < self.tail.Length; i++)
                {
                    newTail[i].pos = self.tail[i].pos;
                    newTail[i].lastPos = self.tail[i].lastPos;
                    newTail[i].vel = self.tail[i].vel;
                    newTail[i].terrainContact = self.tail[i].terrainContact;
                    newTail[i].stretched = self.tail[i].stretched;
                }

                self.tail = newTail;

                // Generate the new body parts array, whilst attempting to preserve the existing indexes
                // The flaw with this is that if the new tail is shorter than the default, this will crash
                List<BodyPart> newBodyParts = self.bodyParts.ToList();
                List<int> oldTailSegmentIndexes = new List<int>();

                bool reachedOldTail = false;

                // Get existing indexes
                for (int i = 0; i <= newBodyParts.Count; i++)
                {
                    if (newBodyParts[i] is TailSegment)
                    {
                        reachedOldTail = true;
                        oldTailSegmentIndexes.Add(i);
                    }
                    else if (reachedOldTail)
                    {
                        break;
                    }
                }

                int tailSegmentIndex = 0;

                // Where possible, substitute the existing indexes with the new tail
                foreach (int i in oldTailSegmentIndexes)
                {
                    newBodyParts[i] = self.tail[tailSegmentIndex];
                    tailSegmentIndex++;
                }

                // For any remaining tail segments, append them to the end
                for (int i = tailSegmentIndex; i < self.tail.Length; i++)
                {
                    newBodyParts.Add(self.tail[tailSegmentIndex]);
                }

                self.bodyParts = newBodyParts.ToArray();
            }

            #endregion

            #region Sounds
            public DynamicSoundLoop storingObjectSound = null!;
            public DynamicSoundLoop retrievingObjectSound = null!;

            private void InitSounds(Player player)
            {
                storingObjectSound = new ChunkDynamicSoundLoop(player.bodyChunks[0]);
                storingObjectSound.sound = Enums.Sounds.StoringObject;
                storingObjectSound.destroyClipWhenDone = false;
                storingObjectSound.Volume = 0.0f;

                retrievingObjectSound = new ChunkDynamicSoundLoop(player.bodyChunks[0]);
                retrievingObjectSound.sound = Enums.Sounds.RetrievingObject;
                retrievingObjectSound.destroyClipWhenDone = false;
                retrievingObjectSound.Volume = 0.0f;
            }

            #endregion

            #region Colours

            public Color BodyColor;
            public Color EyesColor;

            public Color AccentColor;
            public Color CloakColor;


            // Non Customizable
            public List<Color> DynamicColors = new List<Color>();

            public Color EarLColor;
            public Color EarRColor;


            private void InitColors(Player player)
            {
                if (!SlugBaseCharacter.TryGet(Enums.Slugcat.Sacrifice, out var character)) return;

                if (!character.Features.TryGet(PlayerFeatures.CustomColors, out var customColors)) return;

                int playerNumber = !player.room.game.IsArenaSession && player.playerState.playerNumber == 0 ? -1 : player.playerState.playerNumber;

                // Default Colours
                SetColor(customColors, playerNumber, ref BodyColor, "Body");
                SetColor(customColors, playerNumber, ref EyesColor, "Eyes");

                SetColor(customColors, playerNumber, ref AccentColor, "Accent");
                SetColor(customColors, playerNumber, ref CloakColor, "Cloak");


                // Custom Colours
                if (PlayerGraphics.customColors == null || player.IsJollyPlayer) return;

                BodyColor = PlayerGraphics.CustomColorSafety(0);
                EyesColor = PlayerGraphics.CustomColorSafety(1);
                
                AccentColor = PlayerGraphics.CustomColorSafety(2);
                CloakColor = PlayerGraphics.CustomColorSafety(3);
            }

            private void SetColor(ColorSlot[] customColors, int playerNumber, ref Color color, string name)
            {
                ColorSlot customColor = customColors.Where(customColor => customColor.Name == name).FirstOrDefault();
                if (customColor == null) return;

                color = customColor.GetColor(playerNumber);
            }

            #endregion
        }

        private static int MaxStorageCount => 10;

        // Constant Features
        private const float FrameShortcutColorAddition = 0.003f;

        private const int FramesToStoreObject = 80;
        private const int FramesToRetrieveObject = 80;

        private static readonly Vector2 ActiveObjectBaseOffset = new Vector2(0.0f, 20.0f);



        // Generates a unique texture ID so that the atlases don't overlap
        private static int _textureID = 0;
        public static int TextureID => _textureID++;


        private static bool IsCustomSlugcat(Player player) => player.SlugCatClass == Enums.Slugcat.Sacrifice;

        private static List<PlayerModule> GetAllPlayerData(RainWorldGame game)
        {
            List<PlayerModule> allPlayerData = new List<PlayerModule>();
            List<AbstractCreature> players = game.Players;

            if (players == null) return allPlayerData;

            foreach (AbstractCreature creature in players)
            {
                if (creature.realizedCreature == null) continue;

                if (creature.realizedCreature is not Player player) continue;

                if (!PlayerData.TryGetValue(player, out PlayerModule playerModule)) continue;

                allPlayerData.Add(playerModule);
            }

            return allPlayerData;
        }



        // SlugBase Features
        private static Vector2 Vector2Feature(JsonAny json)
        {
            JsonList jsonList = json.AsList();
            return new Vector2(jsonList[0].AsFloat(), jsonList[1].AsFloat());
        }
    }
}
