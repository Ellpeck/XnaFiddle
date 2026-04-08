using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Font;
using MLEM.Formatting;
using MLEM.Extended.Font;
using FontStashSharp;

public class Game1 : Game
{
    const string Text =
        "MLEM's text formatting system allows for various <b>formatting codes</b> to be applied in the middle of a string. Here's a demonstration of some of them.\n\n" +
        "You can write in <b>bold</b>, <i>italics</i>, <u>with an underline</u>, <st>strikethrough</st>, with a <s>drop shadow</s> whose <s #ff0000 4>color</s> and <s #000000 10>offset</s> you can modify in each application of the code, with an <o>outline</o> that you can also <o #ff0000 4>modify</o> <o #ff00ff 2>dynamically</o>, or with various types of <b>combined <c Pink>formatting</c> codes</b>.\n\n" +
        "You can apply <c CornflowerBlue>custom</c> <c Yellow>colors</c> to text, including all default <c Orange>MonoGame colors</c> and <c #aabb00>inline custom colors</c>.\n\n" +
        "You can also use animations like <a wobbly>a wobbly one</a>, as well as create custom ones using the <a wobbly>Code class</a>.\n\n" +
        "You can also display icons in your text, and use super<sup>script</sup> or sub<sub>script</sub> formatting!\n\n" +
        "Additionally, the text formatter has various methods for interacting with the text, like custom behaviors when hovering over certain parts, and more.";
    const float DefaultScale = 0.5F;
    const float WidthMultiplier = 0.9F;

    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    TextFormatter formatter;
    TokenizedString tokenizedText;
    GenericFont font;

    float Scale {
        get {
            // calculate our scale based on how much larger the window is, so that the text scales with the window
            var viewport = new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);
            return DefaultScale * Math.Min(viewport.Width / 1280F, viewport.Height / 720F);
        }
    }

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        if (GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef))
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // creating a new text formatter as well as a generic font to draw with
        formatter = new TextFormatter {
            DefaultShadowOffset = new Vector2(4),
            DefaultOutlineThickness = 4
        };
        // GenericFont and its subtypes are wrappers around various font classes, including SpriteFont, MonoGame.Extended's BitmapFont and FontStashSharp
        // supplying a bold and italic version of the font here allows for the bold and italic formatting codes to be used
        font = new GenericStashFont(
            LoadFont("RobotoRegular"),
            LoadFont("RobotoBold"),
            LoadFont("RobotoItalic"));

        // tokenizing our text and splitting it to fit the screen
        // we specify our text alignment here too, so that all data is cached correctly for display
        tokenizedText = formatter.Tokenize(font, Text, TextAlignment.Center);
        tokenizedText.Split(font, GraphicsDevice.Viewport.Width * WidthMultiplier, Scale, TextAlignment.Center);

        Window.ClientSizeChanged += (sender, args) => {
            // re-split our text if the window resizes, since it depends on the window size
            // this doesn't require re-tokenization of the text, since TokenizedString also stores the un-split version
            tokenizedText.Split(font, GraphicsDevice.Viewport.Width * WidthMultiplier, Scale, TextAlignment.Center);
        };
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // update our tokenized string to animate the animation codes
        tokenizedText.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        GraphicsDevice.Clear(Color.DarkSlateGray);
        spriteBatch.Begin();

        // we draw the tokenized text in the center of the screen
        // since the text is already center-aligned, we only need to align it on the y axis here
        var size = tokenizedText.GetArea(scale: Scale).Size;
        var pos = new Vector2(GraphicsDevice.Viewport.Width / 2, (GraphicsDevice.Viewport.Height - size.Y) / 2);

        // draw the text itself (start and end indices are optional)
        tokenizedText.Draw(gameTime, spriteBatch, pos, font, Color.White, Scale, 0);

        spriteBatch.End();
    }

    private SpriteFontBase LoadFont(string name)
    {
        // we use FontStashSharp fonts for this example; see the FontStashSharp example for more information
        var files = XnaFiddle.InMemoryContentManager.Files;
        var fontSystem = new FontSystem();
        fontSystem.AddFont(files[name]);
        return fontSystem.GetFont(64);
    }
}
