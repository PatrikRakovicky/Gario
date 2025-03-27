using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Gario_game;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    Texture2D garioTexture;
    Texture2D monkeyManTexture;
    Texture2D brickTexture;
    Texture2D garioCoinTexture;
    private Matrix cameraTransform;

    Vector2 garioPosition = new Vector2(200, 100);
    Vector2 coinPosition;
    private Vector2 cameraPosition;
    private float worldWidth = 9600f;
    float velocityY = 0;
    float gravity = 0.5f;
    bool isJumping = false;
    bool right = true;
    int screenWidth;
    int screenHeight;
    int scale = 4;
    int tileWidth = 16;
    int tileHeight = 16;
    bool[,] map;
    int maxColumn = 0;
    int previousColumn = 0;

    Random random = new Random();

    // Pridáme premennú pre zvuk
    SoundEffect jumpSound;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        map = new bool[(Convert.ToInt32(worldWidth) / (tileWidth * scale)), 4];
        for (int i = 0; i < map.GetLength(0); i++)
        {
            map[i, 0] = true;
            int random1 = random.Next(0, 10);
            if (random1 <= 8 && map[i, 0] && maxColumn - 1 < previousColumn)
            {
                map[i, 1] = true;
                maxColumn += 1;
            }
            else
            {
                map[i, 1] = false;
            }
            int random2 = random.Next(0, 10);
            if (random2 <= 7 && map[i, 1] && maxColumn - 1 < previousColumn)
            {
                map[i, 2] = true;
                maxColumn += 1;
            }
            else
            {
                map[i, 2] = false;
            }
            int random3 = random.Next(0, 10);
            if (random3 <= 6 && map[i, 2] && maxColumn - 1 < previousColumn)
            {
                map[i, 3] = true;
                maxColumn += 1;
            }
            else
            {
                map[i, 3] = false;
            }

            previousColumn = maxColumn;
            maxColumn = 0;
        }
        screenWidth = _graphics.PreferredBackBufferWidth;
        screenHeight = _graphics.PreferredBackBufferHeight;
        cameraPosition = Vector2.Zero;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        garioTexture = Content.Load<Texture2D>("gario");
        monkeyManTexture = Content.Load<Texture2D>("monkey_man");
        brickTexture = Content.Load<Texture2D>("brick");
        garioCoinTexture = Content.Load<Texture2D>("gario_coin");

        // Načítanie zvukového súboru
        jumpSound = Content.Load<SoundEffect>("jump_sound_gario1");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Pohyb kamery
        float screenCenter = cameraPosition.X + screenWidth / 2;
        float cameraMoveThreshold = screenWidth * 0.25f;

        if (garioPosition.X > screenCenter + cameraMoveThreshold)
        {
            cameraPosition.X += (garioPosition.X - (screenCenter + cameraMoveThreshold));
        }
        else if (garioPosition.X < screenCenter - cameraMoveThreshold)
        {
            cameraPosition.X -= ((screenCenter - cameraMoveThreshold) - garioPosition.X);
        }

        cameraPosition.X = MathHelper.Clamp(cameraPosition.X, 0, worldWidth - screenWidth);
        cameraTransform = Matrix.CreateTranslation(new Vector3(-cameraPosition.X, 0, 0));

        // Aplikácia gravitácie na osi Y
        velocityY += gravity;
        garioPosition.Y += velocityY;

        int height = 1;
        int heightr = 1;
        int heightl = 1;
        for (int i = 1; i < map.GetLength(1); i++)
        {
            int index = Math.Max(0, (int)Math.Round(garioPosition.X / (tileWidth * scale)));

            if (map[index, i])
            {
                height++;
            }
            if (index + 1 < map.GetLength(0) && map[index + 1, i])
            {
                heightr++;
            }
            if (index - 1 >= 0 && map[index - 1, i])
            {
                heightl++;
            }
        }

        // Kolízia na osi Y (pád na podlahu)
        if (garioPosition.Y >= screenHeight - (tileHeight * scale) * height - garioTexture.Height * scale)
        {
            garioPosition.Y = screenHeight - (tileHeight * scale) * height - garioTexture.Height * scale;
            isJumping = false;
            velocityY = 0;
        }

        KeyboardState keyState = Keyboard.GetState();
        // Ukladanie predchádzajúcej X pozície pre X-ovú kolíziu
        float previousX = garioPosition.X;
        if (keyState.IsKeyDown(Keys.Left))
        {
            garioPosition.X -= 6;
            right = false;
        }
        if (keyState.IsKeyDown(Keys.Right))
        {
            garioPosition.X += 6;
            right = true;
        }

        // Ak sa po pohybe na osi X vyskytne kolízia s blokom, vrátime hráča na predchádzajúcu X pozíciu.
        if (IsCollidingOnX(garioPosition))
        {
            garioPosition.X = previousX;
        }

        // Pri stlačení SPACE vykonáme skok, vygenerujeme mincu a prehráme zvuk
        if (keyState.IsKeyDown(Keys.Space) && !isJumping)
        {
            velocityY = -10;  // Sila skoku
            isJumping = true;
            GenerateCoin();

            // Prehrá zvuk
            jumpSound.Play();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: cameraTransform);

        // Kreslenie postavy
        _spriteBatch.Draw(garioTexture, garioPosition, null, Color.White, 0f, Vector2.Zero, scale, right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);

        // Kreslenie blokov
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if (map[i, j])  // Ak blok existuje
                {
                    _spriteBatch.Draw(
                        brickTexture,
                        new Vector2(i * tileWidth * scale, screenHeight - (j + 1) * tileHeight * scale),
                        null,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }
        // Kreslenie mince
        _spriteBatch.Draw(garioCoinTexture, coinPosition, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    // Upravená metóda zisťujúca kolíziu na osi X s použitím vnútorného kolízneho obdĺžnika
    private bool IsCollidingOnX(Vector2 position)
    {
        // Margin, ktorý zmenší kolízny obdĺžnik (lze prispôsobiť)
        int margin = 15;
        Rectangle garioRect = new Rectangle(
            (int)position.X + margin,
            (int)position.Y + margin,
            (garioTexture.Width * scale) - 2 * margin,
            (garioTexture.Height * scale) - 2 * margin
        );

        // Vypočítanie rozsahu indexov dlaždíc, ktoré môžu kolidovať
        int startColumn = Math.Max(0, (int)(garioRect.Left / (tileWidth * scale)));
        int endColumn = Math.Min(map.GetLength(0) - 1, (int)(garioRect.Right / (tileWidth * scale)));
        int startRow = 0;
        int endRow = map.GetLength(1) - 1;

        // Prechádzame dlaždice v tomto rozsahu a zisťujeme kolíziu
        for (int i = startColumn; i <= endColumn; i++)
        {
            for (int j = startRow; j <= endRow; j++)
            {
                if (map[i, j])
                {
                    Rectangle tileRect = new Rectangle(
                        i * tileWidth * scale,
                        screenHeight - (j + 1) * tileHeight * scale,
                        tileWidth * scale,
                        tileHeight * scale
                    );
                    if (garioRect.Intersects(tileRect))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void GenerateCoin()
    {
        int x = random.Next(0, Convert.ToInt32(worldWidth));
        coinPosition = new Vector2(x, 0);
    }
}
