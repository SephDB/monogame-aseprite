/* -----------------------------------------------------------------------------
Copyright 2022 Christopher Whitley

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
----------------------------------------------------------------------------- */
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.Aseprite;

public sealed class AnimatedSprite : Sprite
{
    private readonly SpriteSheet _spriteSheet;
    private SpriteSheetAnimation _currentAnimation;

    public SpriteSheetAnimation CurrentAnimation => _currentAnimation;

    public AnimatedSprite(SpriteSheet spriteSheet, string startingAnimation)
        : this(spriteSheet, startingAnimation, Vector2.Zero, Color.White, 0.0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.0f) { }

    public AnimatedSprite(SpriteSheet spriteSheet, string startingAnimation, Vector2 position)
        : this(spriteSheet, startingAnimation, position, Color.White, 0.0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.0f) { }

    public AnimatedSprite(SpriteSheet spriteSheet, string startingAnimation, Vector2 position, Color color)
    : this(spriteSheet, startingAnimation, position, color, 0.0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.0f) { }

    public AnimatedSprite(SpriteSheet spriteSheet, string startingAnimation, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        : base(spriteSheet[0], position, color, rotation, origin, scale, effects, layerDepth)
    {
        _spriteSheet = spriteSheet;
        SetCurrentAnimation(startingAnimation);
    }

    [MemberNotNull(nameof(_currentAnimation))]
    private void SetCurrentAnimation(string name)
    {
        SpriteSheetAnimationDefinition definition = _spriteSheet.GetAnimationDefinition(name);

        SpriteSheetFrame[] frames = new SpriteSheetFrame[definition.FrameIndexes.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = _spriteSheet.GetRegion(definition.FrameIndexes[i]);
        }

        _currentAnimation = new(definition.Name, frames, definition.IsLooping, definition.IsReversed, definition.IsPingPong);
        SpriteSheetRegion = _currentAnimation.CurrentFrame;

    }

    public SpriteSheetAnimation Play(string name, Action? onFrameBegin = default, Action? onFrameEnd = default, Action? onAnimationLoop = default, Action? onAnimationEnd = default)
    {
        SetCurrentAnimation(name);
        _currentAnimation.OnFrameBegin = onFrameBegin;
        _currentAnimation.OnFrameEnd = onFrameEnd;
        _currentAnimation.OnAnimationLoop = onAnimationLoop;
        _currentAnimation.OnAnimationEnd = onAnimationEnd;
        return _currentAnimation;
    }

    public void Update(float deltaTimeMilliseconds)
    {
        GameTime fakeGameTime = new();
        fakeGameTime.ElapsedGameTime = TimeSpan.FromMilliseconds(deltaTimeMilliseconds);
        Update(fakeGameTime);
    }

    public void Update(GameTime gameTime)
    {
        _currentAnimation.Update(gameTime);
        SpriteSheetRegion = _currentAnimation.CurrentFrame;
        // SpriteSheetRegion = _spriteSheet[_currentAnimation.CurrentFrame.Index];
    }
}
