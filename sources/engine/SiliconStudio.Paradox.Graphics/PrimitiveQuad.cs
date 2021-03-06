﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects.Modules;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Primitive quad use to draw an effect on a quad (fullscreen by default). This is directly accessible from the <see cref="GraphicsDevice.DrawQuad"/> method.
    /// </summary>
    public class PrimitiveQuad : ComponentBase
    {
        private readonly Effect simpleEffect;
        private readonly SharedData sharedData;
        private const int QuadCount = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveQuad" /> class with a <see cref="SimpleEffect"/>.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        public PrimitiveQuad(GraphicsDevice graphicsDevice) : this(graphicsDevice, new SimpleEffect(graphicsDevice))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveQuad" /> class with a particular effect.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The effect.</param>
        public PrimitiveQuad(GraphicsDevice graphicsDevice, Effect effect)
        {
            GraphicsDevice = graphicsDevice;
            simpleEffect = effect;
            simpleEffect.Parameters.Set(SpriteBaseKeys.MatrixTransform, Matrix.Identity);
            sharedData = GraphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, "PrimitiveQuad::VertexBuffer", () => new SharedData(GraphicsDevice, simpleEffect.InputSignature));
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Draws a quad. The effect must have been applied before calling this method with pixel shader having the signature float2:TEXCOORD.
        /// </summary>
        public void Draw()
        {
            GraphicsDevice.SetVertexArrayObject(sharedData.VertexBuffer);
            GraphicsDevice.Draw(PrimitiveType.TriangleList, QuadCount);
            GraphicsDevice.SetVertexArrayObject(null);
        }

        /// <summary>
        /// Draws a quad with a texture. This Draw method is using the current effect bound to this instance.
        /// </summary>
        /// <param name="texture">The texture.</param>
        public void Draw(Texture texture)
        {
            Draw(texture, null, Color.White);
        }

        /// <summary>
        /// Draws a quad with a texture. This Draw method is using a simple pixel shader that is sampling the texture.
        /// </summary>
        /// <param name="texture">The texture to draw.</param>
        /// <param name="samplerState">State of the sampler. If null, default sampler is <see cref="SamplerStateFactory.LinearClamp" />.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="System.ArgumentException">Expecting a Texture2D;texture</exception>
        public void Draw(Texture texture, SamplerState samplerState, Color4 color)
        {
            var texture2D = texture as Texture2D;
            if (texture2D == null) throw new ArgumentException("Expecting a Texture2D", "texture");

            // Make sure that we are using our vertex shader
            simpleEffect.Parameters.Set(SpriteEffectKeys.Color, color);
            simpleEffect.Parameters.Set(TexturingKeys.Texture0, texture as Texture2D);
            simpleEffect.Parameters.Set(TexturingKeys.Sampler, samplerState ?? GraphicsDevice.SamplerStates.LinearClamp);
            simpleEffect.Apply();
            Draw();

            // TODO ADD QUICK UNBIND FOR SRV
            //GraphicsDevice.Context.PixelShader.SetShaderResource(0, null);
        }

        /// <summary>
        /// Internal structure used to store VertexBuffer and VertexInputLayout.
        /// </summary>
        private class SharedData : ComponentBase
        {
            /// <summary>
            /// The vertex buffer
            /// </summary>
            public readonly VertexArrayObject VertexBuffer;
            
            private static readonly VertexPositionTexture[] QuadsVertices = new []
            {
                new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3( 3, 1, 0), new Vector2(2, 0)),
                new VertexPositionTexture(new Vector3(-1,-3, 0), new Vector2(0, 2)),
            };

            public SharedData(GraphicsDevice device, EffectInputSignature defaultSignature)
            {
                var vertexBuffer = Buffer.Vertex.New(device, QuadsVertices).DisposeBy(this);
                
                // Register reload
                vertexBuffer.Reload = (graphicsResource) => ((Buffer)graphicsResource).Recreate(QuadsVertices);

                VertexBuffer = VertexArrayObject.New(device, defaultSignature, new VertexBufferBinding(vertexBuffer, VertexPositionTexture.Layout, QuadsVertices.Length, VertexPositionTexture.Size)).DisposeBy(this);
            }
        }
    }
}