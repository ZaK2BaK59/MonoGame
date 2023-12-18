using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;

public class Remake : Game
{
    private GraphicsDeviceManager graphics;

    private string saveFilePath = "save.txt";
    private SpriteBatch spriteBatch;
    private KeyboardState keyboardState;
    private Random random = new Random();

    private bool isPaused = false;


    private int score = 0;
    private float elapsedTimeSinceLastScoreIncrement = 0f;

    private struct ParallaxData
    {
        public Song gameMusic;
        public SoundEffect jumpMusic;
    }

    private struct ParallaxTextures
    {
        public Texture2D TextureA;
        public Texture2D TextureB;
        public Texture2D TextureC;
        public Texture2D TextureD;
        public Texture2D TextureP;
        public Texture2D TextureEnemy;
    }

    private struct ParallaxPositions
    {
        public Vector2 PosB;
        public Vector2 PosC;
        public Vector2 PosD;
        public Vector2 PosP;
    }

    private struct ParallaxScales
    {
        public Vector2 ScaleP;
        public Vector2 ScaleA;
        public Vector2 ScaleB;
        public Vector2 ScaleC;
        public Vector2 ScaleD;
    }

    private struct Enemy
    {
        public Vector2 Position;
        public Texture2D Texture;
    }

    private ParallaxData data;

    private bool gameOver = false;
    private Texture2D gameOverTexture;

    private ParallaxTextures textures;
    private ParallaxPositions positions;
    private ParallaxScales scales;
    private Rectangle frame;

    private int LoadBestScore()
{
    int bestScore = 0;

    if (File.Exists(saveFilePath))
    {
        using (StreamReader reader = new StreamReader(saveFilePath))
        {
           
            string bestScoreString = reader.ReadLine();
            if (int.TryParse(bestScoreString, out int savedBestScore))
            {
                bestScore = savedBestScore;
            }
        }
    }

    return bestScore;
}


private int LoadIntValue(StreamReader reader)
{
    string valueString = reader.ReadLine();
    return int.TryParse(valueString, out int value) ? value : 0;
}


    private List<Vector2> fireballs;
    private List<Enemy> enemies; 

    private List<Enemy> enemiesToRemove;
    private Texture2D fireballTexture;
    private const float FireballSpeed = 500f;
    private const float FireballCooldown = 0.5f;
    private float currentFireballCooldown = 0f;

    private int bestScore = 0;


    private const float EnemySpeed = 200f; 
    private const float EnemySpawnInterval = 2f; 
    private float enemySpawnTimer = 0f;

    private float enemySpawnInterval = 2f;

    private int frameCounter = 0;
    private int framesToSkip = 4;
    private int spriteWidth = 109;
    private int totalSprites = 8;

    private const float ParallaxCSpeed = 10f;
    private const float ParallaxBSpeed = 5f;
    private const float ParallaxDSpeed = 15f;

    private SpriteFont font;

    public Remake()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromMilliseconds(16);
        positions.PosB.Y = 0;
        graphics.PreferredBackBufferWidth = 1200;
        graphics.PreferredBackBufferHeight = 600;
        graphics.ApplyChanges();
        InitializeFireballs();
        InitializeEnemies();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        LoadTextures();
        LoadSound();
        LoadFireballTexture();
        font = Content.Load<SpriteFont>("Arial"); 

        MediaPlayer.Volume = 0.3f;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Play(data.gameMusic);

        frame = new Rectangle(0, 0, 109, 130);

        MakeSpace();

        LoadGame();
    }

    private const string SaveFileName = "save.txt";

private void SaveGame()
{
    using (StreamWriter writer = new StreamWriter(saveFilePath))
    {
       
        writer.WriteLine(score);

        
        writer.WriteLine(bestScore);
        
    }
}


private void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            using (StreamReader reader = new StreamReader(saveFilePath))
            {
               
                string scoreString = reader.ReadLine();
                if (int.TryParse(scoreString, out int savedScore))
                {
                    score = savedScore;
                }
                
                string bestScoreString = reader.ReadLine();
                if (int.TryParse(bestScoreString, out int savedBestScore))
                {
                    bestScore = savedBestScore;
                }
            }
        }
    }

    private void LoadTextures()
    {
        textures.TextureA = Content.Load<Texture2D>("texture/parallax3");
        textures.TextureB = Content.Load<Texture2D>("texture/parallax2");
        textures.TextureC = Content.Load<Texture2D>("texture/parallax1");
        textures.TextureD = Content.Load<Texture2D>("texture/parallax4");
        textures.TextureP = Content.Load<Texture2D>("texture/player");
        textures.TextureEnemy = Content.Load<Texture2D>("texture/ped");
        gameOverTexture = Content.Load<Texture2D>("texture/gameover");

    }

    private void LoadSound()
    {
        data.gameMusic = Content.Load<Song>("music/game_music");
        data.jumpMusic = Content.Load<SoundEffect>("sound/jump_music");
    }

    private void LoadFireballTexture()
    {
        fireballTexture = Content.Load<Texture2D>("texture/boule");
    }

    private void InitializeFireballs()
    {
        fireballs = new List<Vector2>();
    }

    private void InitializeEnemies()
    {
        enemies = new List<Enemy>();
        enemiesToRemove = new List<Enemy>(); 
    }

    private void AdjustSpeed()
    {
        frameCounter++;
        if (frameCounter >= framesToSkip)
        {
            frame.X += spriteWidth;
            if (frame.X >= spriteWidth * totalSprites)
            {
                frame.X = 0;
            }
            frameCounter = 0;
        }
    }

protected override void Update(GameTime gameTime)
{
    keyboardState = Keyboard.GetState();

   
    HandlePauseInput();

    if (!isPaused && !gameOver)
    {
        AdjustSpeed();
        UpdateFireballs(gameTime);
        MoveParallaxD();
        MoveParallaxC();
        MoveParallaxB();
        UpdateFireballCooldown(gameTime);

        
        elapsedTimeSinceLastScoreIncrement += (float)gameTime.ElapsedGameTime.TotalSeconds;

        
        if (elapsedTimeSinceLastScoreIncrement >= 0.1f)
        {
            score++;
            elapsedTimeSinceLastScoreIncrement = 0f; 
        }

       
        enemySpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (enemySpawnTimer > enemySpawnInterval)
        {
            CreateEnemies(gameTime);
        }

        UpdateEnemies(gameTime);

        
        CheckCollisionsWithEnemies();
    }

    
    if (keyboardState.IsKeyDown(Keys.Enter) && gameOver)
    {
        ResetGame(); 
        
        
        RestartGame();
    }

    base.Update(gameTime);
}




private void ResetGame()
{
   
    score = 0;
    elapsedTimeSinceLastScoreIncrement = 0f;
    gameOver = false;
    MediaPlayer.Resume(); 
    InitializeEnemies(); 
    

    
    bestScore = LoadBestScore();

    

    
    MakeSpace();
}
private void CheckCollisionsWithEnemies()
{
    Rectangle playerRectangle = new Rectangle(
        (int)(positions.PosP.X),
        (int)(GraphicsDevice.Viewport.Height - frame.Height * scales.ScaleP.Y - 100),
        (int)(frame.Width * scales.ScaleP.X),
        (int)(frame.Height * scales.ScaleP.Y));

    foreach (Enemy enemy in enemies)
    {
        Rectangle enemyRectangle = new Rectangle(
            (int)enemy.Position.X,
            (int)enemy.Position.Y,
            enemy.Texture.Width,
            enemy.Texture.Height);

        if (playerRectangle.Intersects(enemyRectangle))
        {
           
            gameOver = true;
            MediaPlayer.Pause(); 

            
            if (score > bestScore)
            {
                bestScore = score;
                SaveGame(); 
            }
        }
    }
}



private bool isSpaceKeyPressedLastFrame = false; 

private void HandlePauseInput()
{
    KeyboardState currentKeyboardState = Keyboard.GetState();

    if (currentKeyboardState.IsKeyDown(Keys.Space) && !isSpaceKeyPressedLastFrame)
    {
        
        isPaused = !isPaused;

        
        if (isPaused)
        {
            MediaPlayer.Pause();
            SaveGame();
{
    keyboardState = Keyboard.GetState();

    if (keyboardState.IsKeyDown(Keys.Escape))
    {
        SaveGame();
        Exit();
    }

    if (keyboardState.IsKeyDown(Keys.Enter) && gameOver)
    {
       
        
        RestartGame();
    }
}
        }
        else
        {
           
            MediaPlayer.Resume();
        }
    }

   
    isSpaceKeyPressedLastFrame = currentKeyboardState.IsKeyDown(Keys.Space);
}

    private void UpdateEnemies(GameTime gameTime)
    {
        enemySpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;


        List<Enemy> enemiesToRemove = new List<Enemy>();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy currentEnemy = enemies[i]; 


            currentEnemy.Position.X -= EnemySpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;


            foreach (Vector2 fireball in fireballs)
            {
                Rectangle enemyRectangle = new Rectangle((int)currentEnemy.Position.X, (int)currentEnemy.Position.Y, 150, 150);
                Rectangle fireballRectangle = new Rectangle((int)fireball.X, (int)fireball.Y, 30, 30);

                if (enemyRectangle.Intersects(fireballRectangle))
                {
                    
                    enemiesToRemove.Add(currentEnemy);
                }
            }

            
            if (currentEnemy.Position.X + currentEnemy.Texture.Width < 0)
            {
                enemiesToRemove.Add(currentEnemy);
            }

            
            enemies[i] = currentEnemy;
        }

        
        foreach (Enemy enemyToRemove in enemiesToRemove)
        {
            enemies.Remove(enemyToRemove);
        }
    }

    private void CheckCollisionsWithEnemies(Vector2 fireballPosition)
    {
        Rectangle fireballRectangle = new Rectangle((int)fireballPosition.X, (int)fireballPosition.Y, 30, 30);

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            Rectangle enemyRectangle = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 150, 150);

            
            if (fireballRectangle.Intersects(enemyRectangle))
            {
                
                enemiesToRemove.Add(enemy);

                
                fireballs.RemoveAt(i);
            }
        }
    }

    private void CreateEnemies(GameTime gameTime)
    {
        enemySpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (enemySpawnTimer > enemySpawnInterval)
        {
           
            Enemy newEnemy = new Enemy
            {
                Position = new Vector2(GraphicsDevice.Viewport.Width, GetRandomYForEnemy()),
                Texture = textures.TextureEnemy 
            };

            float offsetX = 350f;
            float offsetY = 375f;
            newEnemy.Position.X += offsetX;
            newEnemy.Position.Y += offsetY;

            enemies.Add(newEnemy);

           
            enemySpawnTimer = 0f;
            enemySpawnInterval = (float)random.NextDouble() * 2.0f + 0.5f; 
        }
    }

    private int GetRandomYForEnemy()
    {
        int maxY = GraphicsDevice.Viewport.Height - textures.TextureEnemy.Height;

        if (maxY < 0)
        {
            
            
            return 0;
        }

        return random.Next(0, maxY);
    }

    private void UpdateFireballCooldown(GameTime gameTime)
    {
        if (currentFireballCooldown > 0)
        {
            currentFireballCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }

    private void UpdateFireballs(GameTime gameTime)
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            fireballs[i] = new Vector2(fireballs[i].X + FireballSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds, fireballs[i].Y);

            if (fireballs[i].X > GraphicsDevice.Viewport.Width)
            {
                fireballs.RemoveAt(i);
            }
        }

        if (keyboardState.IsKeyDown(Keys.Right) && currentFireballCooldown <= 0)
        {
            FireFireball();
            currentFireballCooldown = FireballCooldown;
        }
    }

    protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);

    spriteBatch.Begin();

    if (isPaused)
    {
        
        string pauseText = "Pause";
        Vector2 pauseTextPosition = new Vector2((GraphicsDevice.Viewport.Width - font.MeasureString(pauseText).X) / 2, (GraphicsDevice.Viewport.Height - font.MeasureString(pauseText).Y) / 2);
        spriteBatch.DrawString(font, pauseText, pauseTextPosition, Color.White);
    }
    else if (gameOver)
    {
        
        float gameOverScale = 0.55f; 

        
        int newWidth = (int)(gameOverTexture.Width * gameOverScale);
        int newHeight = (int)(gameOverTexture.Height * gameOverScale);

        
        Vector2 gameOverPosition = new Vector2((GraphicsDevice.Viewport.Width - newWidth) / 2, (GraphicsDevice.Viewport.Height - newHeight) / 2);

        
        spriteBatch.Draw(gameOverTexture, new Rectangle((int)gameOverPosition.X, (int)gameOverPosition.Y, newWidth, newHeight), Color.White);
    }
    else
    {
        
        RenderParallax();
        DrawFireballs();
        DrawEnemies();

        
        string scoreText = "Score: " + score;
        Vector2 scoreSize = font.MeasureString(scoreText);
        Vector2 scorePosition = new Vector2(10, 10); 
        spriteBatch.DrawString(font, scoreText, scorePosition, Color.White); 

        
        string bestScoreText = "Best Score: " + bestScore;
        Vector2 bestScoreSize = font.MeasureString(bestScoreText);
        Vector2 bestScorePosition = new Vector2(10, 10 + scoreSize.Y + 5); 
        spriteBatch.DrawString(font, bestScoreText, bestScorePosition, Color.White);
    }

    spriteBatch.End();

    base.Draw(gameTime);
}



    private void MakeSpace()
    {
        scales.ScaleP = new Vector2(1, 1);
        scales.ScaleD = new Vector2(9, 2.5f);
        scales.ScaleC = new Vector2(12, 4);
        scales.ScaleB = new Vector2(8, 4.02684563758f);
        scales.ScaleA = new Vector2(2, 2);
    }

    private void MoveParallaxC()
    {
        positions.PosC.X -= ParallaxCSpeed;

        float scaledWidthC = GetScaledWidth(textures.TextureC, scales.ScaleC);

        if (positions.PosC.X + scaledWidthC <= 1200)
        {
            positions.PosC.X = 0;
        }
    }

    private void MoveParallaxB()
    {
        positions.PosB.X -= ParallaxBSpeed;

        float scaledWidthB = GetScaledWidth(textures.TextureB, scales.ScaleB);

        if (positions.PosB.X + scaledWidthB <= 1200)
        {
            positions.PosB.X = 0;
        }
    }

    private void MoveParallaxD()
    {
        positions.PosD.X -= ParallaxDSpeed;

        float scaledWidthD = GetScaledWidth(textures.TextureD, scales.ScaleD);

        if (positions.PosD.X + scaledWidthD <= 1200)

        {
            positions.PosD.X = 0;
        }
    }

    private float GetScaledWidth(Texture2D texture, Vector2 scale)
    {
        return texture.Width * scale.X;
    }

    private void RenderParallax()
    {
        spriteBatch.Draw(textures.TextureA, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scales.ScaleA, SpriteEffects.None, 0f);
        spriteBatch.Draw(textures.TextureB, new Vector2(positions.PosB.X, GraphicsDevice.Viewport.Height - textures.TextureB.Height * scales.ScaleB.Y), null, Color.White, 0f, Vector2.Zero, scales.ScaleB, SpriteEffects.None, 0f);
        spriteBatch.Draw(textures.TextureC, new Vector2(positions.PosC.X, GraphicsDevice.Viewport.Height - textures.TextureC.Height * scales.ScaleC.Y), null, Color.White, 0f, Vector2.Zero, scales.ScaleC, SpriteEffects.None, 0f);
        spriteBatch.Draw(textures.TextureD, new Vector2(positions.PosD.X, GraphicsDevice.Viewport.Height - textures.TextureD.Height * scales.ScaleD.Y), null, Color.White, 0f, Vector2.Zero, scales.ScaleD, SpriteEffects.None, 0f);
        spriteBatch.Draw(textures.TextureP, new Vector2((GraphicsDevice.Viewport.Width - frame.Width * scales.ScaleP.X) / 2, GraphicsDevice.Viewport.Height - frame.Height * scales.ScaleP.Y - 100), frame, Color.White, 0f, Vector2.Zero, scales.ScaleP, SpriteEffects.None, 0f);
    }

    private void DrawFireballs()
    {
        foreach (Vector2 fireball in fireballs)
        {
            spriteBatch.Draw(fireballTexture, new Rectangle((int)fireball.X, (int)fireball.Y, 30, 30), Color.White);
        }
    }

    private void DrawEnemies()
    {
        foreach (Enemy enemy in enemies)
        {
            spriteBatch.Draw(enemy.Texture, new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 150, 150), Color.White);
        }
    }

    private void FireFireball()
    {
        float offsetX = 550f;
        float offsetY = 350f;

        fireballs.Add(new Vector2(positions.PosP.X + frame.Width * scales.ScaleP.X + offsetX, positions.PosP.Y + frame.Height * scales.ScaleP.Y / 2 + offsetY));
    }

    private void HandleEvents()
{
    keyboardState = Keyboard.GetState();

    if (keyboardState.IsKeyDown(Keys.Escape))
    {
        SaveGame();
        Exit();
    }

    if (keyboardState.IsKeyDown(Keys.Enter) && gameOver)
    {
        LoadGame();
        RestartGame();
    }
}

private void RestartGame()
{
    ResetGame(); 
    
    InitializeFireballs();
    MediaPlayer.Play(data.gameMusic);
}

}