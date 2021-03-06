﻿namespace OpenGLviaFramebuffer
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using OpenTK;
    using OpenTK.Graphics;

    using FramebufferAttachment = OpenTK.Graphics.OpenGL.FramebufferAttachment;
    using FramebufferErrorCode = OpenTK.Graphics.OpenGL.FramebufferErrorCode;
    using FramebufferTarget = OpenTK.Graphics.OpenGL.FramebufferTarget;
    using GL = OpenTK.Graphics.OpenGL.GL;
    using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
    using PixelInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat;
    using PixelType = OpenTK.Graphics.OpenGL.PixelType;
    using RenderbufferStorage = OpenTK.Graphics.OpenGL.RenderbufferStorage;
    using RenderbufferTarget = OpenTK.Graphics.OpenGL.RenderbufferTarget;
    using Size = System.Drawing.Size;
    using TextureMagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter;
    using TextureMinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter;
    using TextureParameterName = OpenTK.Graphics.OpenGL.TextureParameterName;
    using TextureTarget = OpenTK.Graphics.OpenGL.TextureTarget;
    using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

    internal class FrameBufferHandler
    {
        #region Fields

        private int depthbufferId;

        private int framebufferId;

        private GLControl glControl;

        private bool loaded;

        private Size size;

        private int textureId;

        #endregion

        #region Constructors and Destructors

        public FrameBufferHandler()
        {
            this.loaded = false;
            this.size = Size.Empty;
            this.framebufferId = -1;

            this.glControl = new GLControl(new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false));
            this.glControl.MakeCurrent();
        }

        #endregion

        #region Methods

        internal void Cleanup(ref WriteableBitmap backbuffer)
        {
            if (backbuffer == null || backbuffer.Width != this.size.Width || backbuffer.Height != this.size.Height)
            {
                backbuffer = new WriteableBitmap(
                    this.size.Width, 
                    this.size.Height, 
                    96, 
                    96, 
                    PixelFormats.Pbgra32, 
                    BitmapPalettes.WebPalette);
            }

            backbuffer.Lock();

            GL.ReadPixels(
                0, 
                0, 
                this.size.Width, 
                this.size.Height, 
                PixelFormat.Bgra, 
                PixelType.UnsignedByte, 
                backbuffer.BackBuffer);

            backbuffer.AddDirtyRect(new Int32Rect(0, 0, (int)backbuffer.Width, (int)backbuffer.Height));
            backbuffer.Unlock();
        }

        internal void Prepare(Size framebuffersize)
        {
            if (GraphicsContext.CurrentContext != this.glControl.Context)
            {
                this.glControl.MakeCurrent();
            }

            if (framebuffersize != this.size || this.loaded == false)
            {
                this.size = framebuffersize;
                this.CreateFramebuffer();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.framebufferId);
        }

        private void CreateFramebuffer()
        {
            this.glControl.MakeCurrent();

            if (this.framebufferId > 0)
            {
                GL.DeleteFramebuffer(this.framebufferId);
            }

            if (this.depthbufferId > 0)
            {
                GL.DeleteRenderbuffer(this.depthbufferId);
            }

            if (this.textureId > 0)
            {
                GL.DeleteTexture(this.textureId);
            }

            this.textureId = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, this.textureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(
                TextureTarget.Texture2D, 
                TextureParameterName.TextureMinFilter, 
                (int)TextureMinFilter.Nearest);
            GL.TexParameter(
                TextureTarget.Texture2D, 
                TextureParameterName.TextureMagFilter, 
                (int)TextureMagFilter.Nearest);
            GL.TexImage2D(
                TextureTarget.Texture2D, 
                0, 
                PixelInternalFormat.Rgb8, 
                this.size.Width, 
                this.size.Height, 
                0, 
                PixelFormat.Bgra, 
                PixelType.UnsignedByte, 
                IntPtr.Zero);

            this.framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.framebufferId);
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer, 
                FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2D, 
                this.textureId, 
                0);

            this.depthbufferId = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this.depthbufferId);
            GL.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer, 
                RenderbufferStorage.DepthComponent24, 
                this.size.Width, 
                this.size.Height);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer, 
                FramebufferAttachment.DepthAttachment, 
                RenderbufferTarget.Renderbuffer, 
                this.depthbufferId);

            FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception();
            }

            this.loaded = true;
        }

        #endregion
    }
}