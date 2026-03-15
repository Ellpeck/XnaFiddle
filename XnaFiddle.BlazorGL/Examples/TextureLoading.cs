using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MyGame : Game
{
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    Texture2D logo;
    float rotation;
    float scale = 1f;
    float scaleDir = 1f;

    public MyGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load a texture from a content file.
        // Assets can be bundled with examples or drag-and-dropped onto the canvas.
        logo = Content.Load<Texture2D>("KniIcon");
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Spin the logo
        rotation += dt * 1.5f;

        // Pulse the scale between 0.5x and 1.5x
        scale += scaleDir * dt * 0.4f;
        if (scale > 1.5f) { scale = 1.5f; scaleDir = -1f; }
        if (scale < 0.5f) { scale = 0.5f; scaleDir = 1f; }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 46));

        Vector2 center = new Vector2(
            GraphicsDevice.Viewport.Width / 2f,
            GraphicsDevice.Viewport.Height / 2f);
        Vector2 origin = new Vector2(logo.Width / 2f, logo.Height / 2f);

        spriteBatch.Begin();
        spriteBatch.Draw(logo, center, null, Color.White,
            rotation, origin, scale, SpriteEffects.None, 0f);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
