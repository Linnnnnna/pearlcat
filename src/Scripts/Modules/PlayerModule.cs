﻿using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using RWCustom;

namespace Pearlcat;

public class PlayerModule
{
    public WeakReference<Player> PlayerRef;

    public int firstSprite;
    public int lastSprite;


    public LightSource? activeObjectGlow;


    public PlayerModule(Player self)
    {
        PlayerRef = new WeakReference<Player>(self);

        InitSounds(self);
        InitColors(self);

        LoadTailTexture("tail");

        LoadEarLTexture("ear_l", AccentColor);
        LoadEarRTexture("ear_r", AccentColor);

        currentAnimation = GetObjectAnimation(self);
    }

    public bool canSwallowOrRegurgitate = true;
    public Vector2 prevHeadRotation = Vector2.zero;


    public AbstractPhysicalObject? ActiveObject => activeObjectIndex != null ? abstractInventory[(int)activeObjectIndex] : null;
    public List<AbstractPhysicalObject> abstractInventory = new();

    public int? activeObjectIndex = 0;
    public int? selectedObjectIndex = null;


    public AbstractPhysicalObject? transferObject = null;
    public bool canTransferObject = true;

    public Vector2? transferObjectInitialPos = null;
    public int transferStacker = 0;



    public float shortcutColorStacker = 0.0f;
    public int shortcutColorStackerDirection = 1;



    public ObjectAnimation currentAnimation;

    public ObjectAnimation GetObjectAnimation(Player self)
    {
        List<ObjectAnimation> animationPool = new()
        {
            new BasicObjectAnimation(self),
        };

        return animationPool[Random.Range(0, animationPool.Count)];
    }



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


        if (Futile.atlasManager.DoesContainElementWithName(textureName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);
        
        if (earLTexture != null)
            Object.DestroyImmediate(earLTexture);

        earLTexture = AssetLoader.GetTexture(textureName);
        if (earLTexture == null) return;

        // Apply Colors
        MapAlphaToColor(earLTexture, 1.0f, BodyColor);
        MapAlphaToColor(earLTexture, 0.0f, color);
        EarLColor = color;

        earLAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + Hooks.TextureID, earLTexture, false);
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

        earRAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + Hooks.TextureID, earRTexture, false);
    }

    public void RegenerateEars()
    {
        if (!PlayerRef.TryGetTarget(out var player)) return;

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
        if (Futile.atlasManager.DoesContainElementWithName(textureName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);
        
        if (tailTexture != null)
            Object.DestroyImmediate(tailTexture);

        tailTexture = AssetLoader.GetTexture(textureName);
        if (tailTexture == null) return;


        // Apply Colors
        MapAlphaToColor(tailTexture, 1.0f, BodyColor);
        MapAlphaToColor(tailTexture, 0.0f, AccentColor);

        tailAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + Hooks.TextureID, tailTexture, false);
    }


    public void RegenerateTail()
    {
        if (!PlayerRef.TryGetTarget(out var player)) return;

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
        List<int> oldTailSegmentIndexes = new();

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


    public Texture2D? cloakTexture;
    public FAtlas? cloakAtlas;

    public int cloakSprite;
    public Cloak cloak = null!;

    public void LoadCloakTexture(string textureName)
    {
        if (Futile.atlasManager.DoesContainElementWithName(textureName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(textureName);

        if (cloakTexture != null)
            Object.DestroyImmediate(cloakTexture);

        cloakTexture = AssetLoader.GetTexture(textureName);
        if (cloakTexture == null) return;

        // Apply Colors
        MapAlphaToColor(cloakTexture, 1.0f, Color.red);

        cloakAtlas = Futile.atlasManager.LoadAtlasFromTexture(Plugin.MOD_ID + textureName + Hooks.TextureID, cloakTexture, false);
    }

    public class Cloak
    {
        private readonly int divs = 11;

        private readonly PlayerGraphics owner;
        private readonly PlayerModule playerModule;

        public Vector2[,,] clothPoints;
        public bool visible;
        public bool needsReset;

        public Cloak(PlayerGraphics owner, PlayerModule playerModule)
        {
            this.owner = owner;
            this.playerModule = playerModule;
            
            clothPoints = new Vector2[divs, divs, 3];
            visible = true;
            needsReset = true;
        }

        public void Update()
        {
            if (!visible || owner.player.room == null)
            {
                needsReset = true;
                return;
            }

            if (needsReset)
            {
                for (int i = 0; i < divs; i++)
                {
                    for (int j = 0; j < divs; j++)
                    {
                        clothPoints[i, j, 1] = owner.player.bodyChunks[1].pos;
                        clothPoints[i, j, 0] = owner.player.bodyChunks[1].pos;
                        clothPoints[i, j, 2] *= 0f;
                    }
                }
                needsReset = false;
            }

            Vector2 vector = Vector2.Lerp(owner.head.pos, owner.player.bodyChunks[1].pos, 0.75f);

            if (owner.player.bodyMode == Player.BodyModeIndex.Crawl)
                vector += new Vector2(0f, 4f);

            Vector2 a = default;

            if (owner.player.bodyMode == Player.BodyModeIndex.Stand)
            {
                vector += new Vector2(0f, Mathf.Sin(owner.player.animationFrame / 6f * 2f * Mathf.PI) * 2f);
                a = new Vector2(0f, -11f + Mathf.Sin(owner.player.animationFrame / 6f * 2f * Mathf.PI) * -2.5f);
            }

            Vector2 bodyPos = vector;
            Vector2 vector2 = Custom.DirVec(owner.player.bodyChunks[1].pos, owner.player.bodyChunks[0].pos + Custom.DirVec(default, owner.player.bodyChunks[0].vel) * 5f) * 1.6f;
            Vector2 perp = Custom.PerpendicularVector(vector2);

            for (int k = 0; k < divs; k++)
            {
                for (int l = 0; l < divs; l++)
                {
                    Mathf.InverseLerp(0f, divs - 1, k);
                    float num = Mathf.InverseLerp(0f, divs - 1, l);

                    clothPoints[k, l, 1] = clothPoints[k, l, 0];
                    clothPoints[k, l, 0] += clothPoints[k, l, 2];
                    clothPoints[k, l, 2] *= 0.999f;
                    clothPoints[k, l, 2].y -= 1.1f * owner.player.EffectiveRoomGravity;

                    Vector2 idealPos = IdealPosForPoint(k, l, bodyPos, vector2, perp) + a * (-1f * num);
                    Vector3 rot = Vector3.Slerp(-vector2, Custom.DirVec(vector, idealPos), num) * (0.01f + 0.9f * num);

                    clothPoints[k, l, 2] += new Vector2(rot.x, rot.y);

                    float num2 = Vector2.Distance(clothPoints[k, l, 0], idealPos);
                    float num3 = Mathf.Lerp(0f, 9f, num);

                    Vector2 a2 = Custom.DirVec(clothPoints[k, l, 0], idealPos);

                    if (num2 > num3)
                    {
                        clothPoints[k, l, 0] -= (num3 - num2) * a2 * (1f - num / 1.4f);
                        clothPoints[k, l, 2] -= (num3 - num2) * a2 * (1f - num / 1.4f);
                    }

                    for (int m = 0; m < 4; m++)
                    {
                        IntVector2 intVector = new IntVector2(k, l) + Custom.fourDirections[m];
                        if (intVector.x >= 0 && intVector.y >= 0 && intVector.x < divs && intVector.y < divs)
                        {
                            num2 = Vector2.Distance(clothPoints[k, l, 0], clothPoints[intVector.x, intVector.y, 0]);
                            a2 = Custom.DirVec(clothPoints[k, l, 0], clothPoints[intVector.x, intVector.y, 0]);
                            float num4 = Vector2.Distance(idealPos, IdealPosForPoint(intVector.x, intVector.y, bodyPos, vector2, perp));
                            clothPoints[k, l, 2] -= (num4 - num2) * a2 * 0.05f;
                            clothPoints[intVector.x, intVector.y, 2] += (num4 - num2) * a2 * 0.05f;
                        }
                    }
                }
            }
        }

        private Vector2 IdealPosForPoint(int x, int y, Vector2 bodyPos, Vector2 dir, Vector2 perp)
        {
            float num = Mathf.InverseLerp(0f, divs - 1, x);
            float t = Mathf.InverseLerp(0f, divs - 1, y);

            return bodyPos + Mathf.Lerp(-1f, 1f, num) * perp * Mathf.Lerp(9f, 11f, t) + dir * Mathf.Lerp(8f, -9f, t) * (1f + Mathf.Sin(3.1415927f * num) * 0.35f * Mathf.Lerp(-1f, 1f, t));
        }

        public Color CloakColorAtPos(float f) => Color.white;


        public void InitiateSprite(int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            playerModule.LoadCloakTexture("cloak");

            if (playerModule.cloakAtlas == null) return;

            sLeaser.sprites[sprite] = TriangleMesh.MakeGridMesh(playerModule.cloakAtlas.name, divs - 1);
            sLeaser.sprites[sprite].color = Color.white;

            for (int i = 0; i < divs; i++)
            {
                for (int j = 0; j < divs; j++)
                {
                    clothPoints[i, j, 0] = owner.player.firstChunk.pos;
                    clothPoints[i, j, 1] = owner.player.firstChunk.pos;
                    clothPoints[i, j, 2] = new Vector2(0f, 0f);
                }
            }
        }

        public void ApplyPalette(int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < divs; i++)
                for (int j = 0; j < divs; j++)
                    ((TriangleMesh)sLeaser.sprites[sprite]).verticeColors[j * divs + i] = CloakColorAtPos(i / (float)(divs - 1));
        }

        public void DrawSprite(int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[sprite].isVisible = (visible && owner.player.room != null);
            if (!sLeaser.sprites[sprite].isVisible) return;

            for (int i = 0; i < divs; i++)
                for (int j = 0; j < divs; j++)
                    ((TriangleMesh)sLeaser.sprites[sprite]).MoveVertice(i * divs + j, Vector2.Lerp(clothPoints[i, j, 1], clothPoints[i, j, 0], timeStacker) - camPos);
        }
    }



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
    public List<Color> DynamicColors = new();

    public Color EarLColor;
    public Color EarRColor;


    private void InitColors(Player player)
    {
        if (!SlugBaseCharacter.TryGet(Enums.Slugcat.Pearlcat, out var character)) return;

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



    public static void MapAlphaToColor(Texture2D texture, float alphaFrom, Color colorTo)
    {
        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                if (texture.GetPixel(x, y).a != alphaFrom) continue;

                texture.SetPixel(x, y, colorTo);
            }
        }

        texture.Apply(false);
    }

    #endregion
}
